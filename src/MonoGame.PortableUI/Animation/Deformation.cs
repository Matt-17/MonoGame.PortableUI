using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Animation
{
    /// <summary>
    ///     Soft-body deformation of a control (<c>control.Deformation</c>): the control is drawn as a texture on a
    ///     grid whose points move on their own. Motion-driven: while <c>Translation</c> moves, the tail lags behind
    ///     (<see cref="Trail"/>) and the front swells while the tail narrows (<see cref="Taper"/>); when it stops,
    ///     every point springs back on <see cref="Spring"/> and the shape wobbles out. <see cref="Wobble"/> kicks it
    ///     on demand; <see cref="Bulge"/>, <see cref="Bend"/> and <see cref="Wave"/> deform it statically.
    ///     Render-only (layout and hit testing keep the rectangle); skipped at <see cref="RenderQuality.Low"/>.
    /// </summary>
    public sealed class Deformation
    {
        // Speed (layout units per second) that gives the full trail and taper.
        private const float ReferenceSpeed = 1000;
        private const float RestOffset = 0.05f;
        private const float RestVelocity = 0.5f;

        private readonly int _columns;
        private readonly int _rows;
        private readonly Vector2[] _offsets;
        private readonly Vector2[] _velocities;
        private Vector2 _direction = Vector2.UnitX;
        private bool _dynamicAtRest = true;
        private SpringSpec _spring = SpringSpec.Wobbly;
        private Vector2 _size;

        /// <param name="columns">Grid cells across (2..32); more cells bend more smoothly.</param>
        /// <param name="rows">Grid cells down (2..32).</param>
        public Deformation(int columns = 8, int rows = 8)
        {
            _columns = Math.Clamp(columns, 2, 32);
            _rows = Math.Clamp(rows, 2, 32);
            _offsets = new Vector2[(_columns + 1) * (_rows + 1)];
            _velocities = new Vector2[_offsets.Length];
        }

        public int Columns => _columns;
        public int Rows => _rows;

        /// <summary>How far the tail lags behind a moving control (0 = off, 1 = a quarter of its length at full speed).</summary>
        public float Trail { get; set; } = 1;

        /// <summary>How much a moving control swells at the front and narrows at the tail (0 = off, 1 = about a third).</summary>
        public float Taper { get; set; } = 1;

        /// <summary>The spring every grid point follows (default <see cref="SpringSpec.Wobbly"/>): softer wobbles longer.</summary>
        public SpringSpec Spring
        {
            get => _spring;
            set => _spring = value.Stiffness > 0 ? value : throw new ArgumentException("Use a SpringSpec preset or constructor.", nameof(value));
        }

        /// <summary>Swells the middle outwards (&gt; 0) or pinches it (&lt; 0), as a fraction of the size.</summary>
        public float Bulge { get; set; }

        /// <summary>Bends the control into an arc: the ends move down (&gt; 0) or up (&lt; 0) by this fraction of its height.</summary>
        public float Bend { get; set; }

        /// <summary>A travelling wave across the control: amplitude as a fraction of its height (needs frames: High quality only).</summary>
        public float Wave { get; set; }

        /// <summary>Waves across the width (default 1).</summary>
        public float WaveLength { get; set; } = 1;

        /// <summary>Waves per second (default 1).</summary>
        public float WaveSpeed { get; set; } = 1;

        /// <summary>True while the shape differs from the rectangle: the control is then drawn as a mesh.</summary>
        public bool IsActive => !_dynamicAtRest || Bulge != 0 || Bend != 0 || Wave != 0;

        internal bool IsAnimating => !_dynamicAtRest || Wave != 0;

        /// <summary>Kicks the jelly: every point gets a push away from the middle (&lt; 0 towards it), and the
        /// shape wobbles out on <see cref="Spring"/> - a tap, a landing, a bump.</summary>
        public void Wobble(float amount = 1)
        {
            for (var row = 0; row <= _rows; row++)
            {
                for (var column = 0; column <= _columns; column++)
                {
                    var r = new Vector2(column / (float)_columns - 0.5f, row / (float)_rows - 0.5f);
                    // Corners and edges swing most; alternate the axes so it reads as jelly, not as a scale.
                    _velocities[row * (_columns + 1) + column] += new Vector2(r.X, -r.Y) * amount * 3.5f * Math.Max(1, (_size.X + _size.Y) / 2);
                }
            }

            _dynamicAtRest = false;
        }

        /// <summary>A new deformation with the same grid and settings, at rest (a template handed to each control).</summary>
        public Deformation Clone() => new Deformation(_columns, _rows)
        {
            Trail = Trail,
            Taper = Taper,
            Spring = Spring,
            Bulge = Bulge,
            Bend = Bend,
            Wave = Wave,
            WaveLength = WaveLength,
            WaveSpeed = WaveSpeed
        };

        /// <summary>Drops all motion; the shape is the rectangle again (static deformers stay).</summary>
        public void Reset()
        {
            Array.Clear(_offsets);
            Array.Clear(_velocities);
            _dynamicAtRest = true;
        }

        /// <summary>One step: <paramref name="velocity"/> is the control's motion in layout units per second.</summary>
        internal void Update(Vector2 size, Vector2 velocity, double seconds)
        {
            _size = size;
            if (seconds <= 0)
                return;
            var speed = velocity.Length();
            var moving = speed > 1 && (Trail != 0 || Taper != 0);
            if (!moving && _dynamicAtRest)
                return;

            if (speed > 1)
                _direction = velocity / speed;
            var motion = moving ? Math.Min(speed / ReferenceSpeed, 1.5f) : 0;
            var half = size / 2;
            var halfExtent = Math.Max(1, Math.Abs(_direction.X) * half.X + Math.Abs(_direction.Y) * half.Y);
            var settled = true;
            for (var row = 0; row <= _rows; row++)
            {
                for (var column = 0; column <= _columns; column++)
                {
                    var index = row * (_columns + 1) + column;
                    var r = new Vector2((column / (float)_columns - 0.5f) * size.X, (row / (float)_rows - 0.5f) * size.Y);
                    var target = Vector2.Zero;
                    if (motion > 0)
                    {
                        // -1 at the tail, 1 at the front.
                        var along = Math.Clamp(Vector2.Dot(r, _direction) / halfExtent, -1, 1);
                        target -= _direction * (Trail * motion * halfExtent * 0.5f * (1 - along) / 2);
                        var across = r - Vector2.Dot(r, _direction) * _direction;
                        target += across * (Taper * motion * 0.35f * along);
                    }

                    var offset = _offsets[index];
                    var v = _velocities[index];
                    _spring.Evaluate(offset.X - target.X, v.X, seconds, out var dx, out var vx);
                    _spring.Evaluate(offset.Y - target.Y, v.Y, seconds, out var dy, out var vy);
                    _offsets[index] = target + new Vector2((float)dx, (float)dy);
                    _velocities[index] = new Vector2((float)vx, (float)vy);
                    if (motion > 0 || Math.Abs(dx) > RestOffset || Math.Abs(dy) > RestOffset || Math.Abs(vx) > RestVelocity || Math.Abs(vy) > RestVelocity)
                        settled = false;
                }
            }

            _dynamicAtRest = settled;
            if (settled)
                Reset();
        }

        /// <summary>
        ///     The displacement in layout units at a point given relative to the control's box (0..1 each way;
        ///     outside it - shadows, focus rings - the edge's displacement continues).
        /// </summary>
        internal Vector2 OffsetAt(float u, float v, Vector2 size, double timeSeconds)
        {
            var result = Vector2.Zero;
            if (!_dynamicAtRest)
            {
                // Bilinear between the grid points.
                var x = Math.Clamp(u, 0, 1) * _columns;
                var y = Math.Clamp(v, 0, 1) * _rows;
                var c0 = Math.Min((int)x, _columns - 1);
                var r0 = Math.Min((int)y, _rows - 1);
                var fx = x - c0;
                var fy = y - r0;
                var stride = _columns + 1;
                var top = Vector2.Lerp(_offsets[r0 * stride + c0], _offsets[r0 * stride + c0 + 1], fx);
                var bottom = Vector2.Lerp(_offsets[(r0 + 1) * stride + c0], _offsets[(r0 + 1) * stride + c0 + 1], fx);
                result = Vector2.Lerp(top, bottom, fy);
            }

            var cu = 2 * u - 1;
            var cv = 2 * v - 1;
            if (Bulge != 0)
            {
                var weight = Math.Max(0, 1 - cu * cu) * Math.Max(0, 1 - cv * cv);
                result += new Vector2(cu * size.X, cv * size.Y) * (Bulge * 0.5f * weight);
            }

            if (Bend != 0)
                result.Y += Bend * size.Y * cu * cu;

            if (Wave != 0)
                result.Y += Wave * size.Y * 0.5f * (float)Math.Sin(MathHelper.TwoPi * (u * WaveLength - timeSeconds * WaveSpeed));

            return result;
        }
    }
}
