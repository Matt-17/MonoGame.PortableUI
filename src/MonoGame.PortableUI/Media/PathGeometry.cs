using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    ///     A free-form shape built from lines, arcs and Bézier curves in its own coordinate space
    ///     (<see cref="ViewBox"/>, like an SVG viewBox). It is flattened to polygons once and
    ///     rasterized at whatever pixel size it is drawn, so it stays crisp when scaled — see
    ///     <see cref="PathRasterizer"/> and the <c>Controls.PathShape</c> control.
    /// </summary>
    public sealed class PathGeometry
    {
        private readonly List<List<Vector2>> _figures = new List<List<Vector2>>();
        private readonly List<bool> _closed = new List<bool>();
        private List<Vector2>? _current;
        private Vector2 _cursor;
        private int _version;

        /// <summary>Coordinate space the path is authored in; drawing maps it onto the target size.</summary>
        public RectangleF ViewBox { get; set; }

        /// <summary>Even-odd (true) or non-zero (false, default) fill rule.</summary>
        public bool EvenOddFill { get; set; }

        /// <summary>Segments used per curve or arc when flattening.</summary>
        public int CurveSegments { get; set; } = 24;

        internal int Version => _version;

        internal IReadOnlyList<List<Vector2>> Figures => _figures;

        internal bool IsClosed(int figure) => _closed[figure];

        public PathGeometry(float viewWidth, float viewHeight)
        {
            ViewBox = new RectangleF(0, 0, viewWidth, viewHeight);
        }

        public PathGeometry MoveTo(float x, float y)
        {
            _current = new List<Vector2> { new Vector2(x, y) };
            _figures.Add(_current);
            _closed.Add(false);
            _cursor = new Vector2(x, y);
            _version++;
            return this;
        }

        public PathGeometry LineTo(float x, float y)
        {
            EnsureFigure();
            _cursor = new Vector2(x, y);
            _current!.Add(_cursor);
            _version++;
            return this;
        }

        public PathGeometry QuadraticTo(float cx, float cy, float x, float y)
        {
            EnsureFigure();
            var p0 = _cursor;
            var p1 = new Vector2(cx, cy);
            var p2 = new Vector2(x, y);
            for (var i = 1; i <= CurveSegments; i++)
            {
                var t = i / (float)CurveSegments;
                var u = 1 - t;
                _current!.Add(u * u * p0 + 2 * u * t * p1 + t * t * p2);
            }
            _cursor = p2;
            _version++;
            return this;
        }

        public PathGeometry CubicTo(float c1x, float c1y, float c2x, float c2y, float x, float y)
        {
            EnsureFigure();
            var p0 = _cursor;
            var p1 = new Vector2(c1x, c1y);
            var p2 = new Vector2(c2x, c2y);
            var p3 = new Vector2(x, y);
            for (var i = 1; i <= CurveSegments; i++)
            {
                var t = i / (float)CurveSegments;
                var u = 1 - t;
                _current!.Add(u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3);
            }
            _cursor = p3;
            _version++;
            return this;
        }

        /// <summary>Circular arc around (<paramref name="cx"/>, <paramref name="cy"/>) from
        /// <paramref name="startAngle"/> sweeping <paramref name="sweepAngle"/> (radians, clockwise in screen space).
        /// Draws a line from the current point to the arc start first.</summary>
        public PathGeometry ArcTo(float cx, float cy, float radius, float startAngle, float sweepAngle)
        {
            var start = new Vector2(cx + radius * MathF.Cos(startAngle), cy + radius * MathF.Sin(startAngle));
            if (_current == null)
                MoveTo(start.X, start.Y);
            else
                LineTo(start.X, start.Y);

            var segments = Math.Max(2, (int)Math.Ceiling(CurveSegments * Math.Abs(sweepAngle) / MathF.PI));
            for (var i = 1; i <= segments; i++)
            {
                var angle = startAngle + sweepAngle * i / segments;
                _current!.Add(new Vector2(cx + radius * MathF.Cos(angle), cy + radius * MathF.Sin(angle)));
            }
            _cursor = _current![_current.Count - 1];
            _version++;
            return this;
        }

        public PathGeometry Close()
        {
            if (_current != null)
                _closed[_closed.Count - 1] = true;
            _current = null;
            _version++;
            return this;
        }

        /// <summary>Closed rounded rectangle figure, e.g. as a clip shape.</summary>
        public PathGeometry AddRoundedRectangle(float x, float y, float width, float height, float radius)
        {
            radius = Math.Max(0, Math.Min(radius, Math.Min(width, height) / 2));
            MoveTo(x + radius, y);
            LineTo(x + width - radius, y);
            if (radius > 0) ArcTo(x + width - radius, y + radius, radius, -MathF.PI / 2, MathF.PI / 2);
            LineTo(x + width, y + height - radius);
            if (radius > 0) ArcTo(x + width - radius, y + height - radius, radius, 0, MathF.PI / 2);
            LineTo(x + radius, y + height);
            if (radius > 0) ArcTo(x + radius, y + height - radius, radius, MathF.PI / 2, MathF.PI / 2);
            LineTo(x, y + radius);
            if (radius > 0) ArcTo(x + radius, y + radius, radius, MathF.PI, MathF.PI / 2);
            return Close();
        }

        public PathGeometry AddEllipse(float cx, float cy, float rx, float ry)
        {
            var segments = Math.Max(8, CurveSegments * 2);
            MoveTo(cx + rx, cy);
            for (var i = 1; i < segments; i++)
            {
                var angle = MathF.Tau * i / segments;
                LineTo(cx + rx * MathF.Cos(angle), cy + ry * MathF.Sin(angle));
            }
            return Close();
        }

        private void EnsureFigure()
        {
            if (_current == null)
                MoveTo(_cursor.X, _cursor.Y);
        }
    }

    /// <summary>Float rectangle for path view boxes.</summary>
    public readonly struct RectangleF
    {
        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
    }
}
