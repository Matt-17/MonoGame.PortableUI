using System;
using System.Globalization;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Media
{
    public sealed partial class PathGeometry
    {
        /// <summary>
        ///     A path from SVG path data (the <c>d</c> attribute: M L H V C S Q T A Z, absolute and relative), so icon
        ///     sets such as Material Symbols can be pasted in as they are. <paramref name="viewBox"/> is the SVG's
        ///     viewBox (Material Symbols: 0 -960 960 960).
        /// </summary>
        public static PathGeometry FromSvg(string pathData, RectangleF viewBox)
        {
            var geometry = new PathGeometry(viewBox.Width, viewBox.Height) { ViewBox = viewBox };
            new SvgPathReader(pathData, geometry).Read();
            return geometry;
        }

        /// <summary>SVG path data in a viewBox from (0, 0) to (<paramref name="viewWidth"/>, <paramref name="viewHeight"/>).</summary>
        public static PathGeometry FromSvg(string pathData, float viewWidth, float viewHeight)
            => FromSvg(pathData, new RectangleF(0, 0, viewWidth, viewHeight));

        /// <summary>Elliptical arc as SVG draws it: from the current point to (x, y), radii, rotation and flags.</summary>
        private void SvgArcTo(float rx, float ry, float rotationDegrees, bool largeArc, bool sweep, float x, float y)
        {
            EnsureFigure();
            var p0 = _cursor;
            var p1 = new Vector2(x, y);
            rx = Math.Abs(rx);
            ry = Math.Abs(ry);
            if (p0 == p1)
                return;
            if (rx < 1e-6f || ry < 1e-6f)
            {
                LineTo(x, y);
                return;
            }

            // SVG implementation notes F.6.5: endpoint to centre parameterisation.
            var phi = MathHelper.ToRadians(rotationDegrees);
            var cos = MathF.Cos(phi);
            var sin = MathF.Sin(phi);
            var dx = (p0.X - p1.X) / 2;
            var dy = (p0.Y - p1.Y) / 2;
            var x1 = cos * dx + sin * dy;
            var y1 = -sin * dx + cos * dy;
            var lambda = x1 * x1 / (rx * rx) + y1 * y1 / (ry * ry);
            if (lambda > 1)
            {
                var grow = MathF.Sqrt(lambda);
                rx *= grow;
                ry *= grow;
            }

            var numerator = rx * rx * ry * ry - rx * rx * y1 * y1 - ry * ry * x1 * x1;
            var denominator = rx * rx * y1 * y1 + ry * ry * x1 * x1;
            var factor = MathF.Sqrt(Math.Max(0, numerator / denominator)) * (largeArc == sweep ? -1 : 1);
            var cx1 = factor * rx * y1 / ry;
            var cy1 = -factor * ry * x1 / rx;
            var cx = cos * cx1 - sin * cy1 + (p0.X + p1.X) / 2;
            var cy = sin * cx1 + cos * cy1 + (p0.Y + p1.Y) / 2;

            var start = MathF.Atan2((y1 - cy1) / ry, (x1 - cx1) / rx);
            var end = MathF.Atan2((-y1 - cy1) / ry, (-x1 - cx1) / rx);
            var delta = end - start;
            if (sweep && delta < 0)
                delta += MathF.Tau;
            else if (!sweep && delta > 0)
                delta -= MathF.Tau;

            var segments = Math.Max(2, (int)Math.Ceiling(CurveSegments * Math.Abs(delta) / MathF.PI));
            for (var i = 1; i < segments; i++)
            {
                var angle = start + delta * i / segments;
                var ex = rx * MathF.Cos(angle);
                var ey = ry * MathF.Sin(angle);
                _current!.Add(new Vector2(cos * ex - sin * ey + cx, sin * ex + cos * ey + cy));
            }
            _current!.Add(p1);
            _cursor = p1;
            _version++;
        }

        private struct SvgPathReader
        {
            private readonly string _data;
            private readonly PathGeometry _geometry;
            private int _index;

            public SvgPathReader(string data, PathGeometry geometry)
            {
                _data = data ?? throw new ArgumentNullException(nameof(data));
                _geometry = geometry;
                _index = 0;
            }

            public void Read()
            {
                var current = Vector2.Zero;
                var start = Vector2.Zero;
                var control = Vector2.Zero;
                var command = ' ';
                var previous = ' ';
                while (true)
                {
                    SkipSeparators();
                    if (_index >= _data.Length)
                        return;
                    var c = _data[_index];
                    if (char.IsLetter(c) && c != 'e' && c != 'E')
                    {
                        command = c;
                        _index++;
                    }
                    else if (command == ' ')
                        throw new FormatException($"SVG path data must start with a command: '{_data}'.");

                    var relative = char.IsLower(command);
                    var origin = relative ? current : Vector2.Zero;
                    switch (char.ToUpperInvariant(command))
                    {
                        case 'M':
                            current = origin + ReadPoint();
                            start = current;
                            _geometry.MoveTo(current.X, current.Y);
                            // Further pairs after a move are lines.
                            command = relative ? 'l' : 'L';
                            break;
                        case 'L':
                            current = origin + ReadPoint();
                            _geometry.LineTo(current.X, current.Y);
                            break;
                        case 'H':
                            current = new Vector2((relative ? current.X : 0) + ReadNumber(), current.Y);
                            _geometry.LineTo(current.X, current.Y);
                            break;
                        case 'V':
                            current = new Vector2(current.X, (relative ? current.Y : 0) + ReadNumber());
                            _geometry.LineTo(current.X, current.Y);
                            break;
                        case 'C':
                        {
                            var c1 = origin + ReadPoint();
                            control = origin + ReadPoint();
                            current = origin + ReadPoint();
                            _geometry.CubicTo(c1.X, c1.Y, control.X, control.Y, current.X, current.Y);
                            break;
                        }
                        case 'S':
                        {
                            var c1 = "CcSs".IndexOf(previous) >= 0 ? 2 * current - control : current;
                            control = origin + ReadPoint();
                            current = origin + ReadPoint();
                            _geometry.CubicTo(c1.X, c1.Y, control.X, control.Y, current.X, current.Y);
                            break;
                        }
                        case 'Q':
                            control = origin + ReadPoint();
                            current = origin + ReadPoint();
                            _geometry.QuadraticTo(control.X, control.Y, current.X, current.Y);
                            break;
                        case 'T':
                            control = "QqTt".IndexOf(previous) >= 0 ? 2 * current - control : current;
                            current = origin + ReadPoint();
                            _geometry.QuadraticTo(control.X, control.Y, current.X, current.Y);
                            break;
                        case 'A':
                        {
                            var rx = ReadNumber();
                            var ry = ReadNumber();
                            var rotation = ReadNumber();
                            var large = ReadFlag();
                            var sweep = ReadFlag();
                            current = origin + ReadPoint();
                            _geometry.SvgArcTo(rx, ry, rotation, large, sweep, current.X, current.Y);
                            break;
                        }
                        case 'Z':
                            _geometry.Close();
                            current = start;
                            // The next command draws on from the figure's start.
                            _geometry._cursor = start;
                            previous = command;
                            command = ' ';
                            continue;
                        default:
                            throw new FormatException($"Unknown SVG path command '{command}'.");
                    }
                    previous = command;
                }
            }

            private Vector2 ReadPoint()
            {
                var x = ReadNumber();
                var y = ReadNumber();
                return new Vector2(x, y);
            }

            private bool ReadFlag()
            {
                SkipSeparators();
                if (_index >= _data.Length || (_data[_index] != '0' && _data[_index] != '1'))
                    throw new FormatException($"Expected an arc flag at {_index} in '{_data}'.");
                return _data[_index++] == '1';
            }

            private float ReadNumber()
            {
                SkipSeparators();
                var begin = _index;
                if (_index < _data.Length && (_data[_index] == '-' || _data[_index] == '+'))
                    _index++;
                var dot = false;
                while (_index < _data.Length)
                {
                    var c = _data[_index];
                    if (char.IsDigit(c))
                        _index++;
                    else if (c == '.' && !dot)
                    {
                        dot = true;
                        _index++;
                    }
                    else if ((c == 'e' || c == 'E') && _index > begin)
                    {
                        _index++;
                        if (_index < _data.Length && (_data[_index] == '-' || _data[_index] == '+'))
                            _index++;
                    }
                    else
                        break;
                }
                if (_index == begin)
                    throw new FormatException($"Expected a number at {_index} in '{_data}'.");
                return float.Parse(_data.Substring(begin, _index - begin), NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            private void SkipSeparators()
            {
                while (_index < _data.Length && (char.IsWhiteSpace(_data[_index]) || _data[_index] == ','))
                    _index++;
            }
        }
    }
}
