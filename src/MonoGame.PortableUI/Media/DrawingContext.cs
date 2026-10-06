using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Media
{
    /// <summary>How the open ends of a stroked line end.</summary>
    public enum LineCap
    {
        /// <summary>The line ends exactly at its end points.</summary>
        Flat,

        /// <summary>The line extends half its thickness past its end points (closes wall corners).</summary>
        Square
    }

    /// <summary>
    ///     Immediate drawing API of a <see cref="Controls.DrawingCanvas"/>: lines, polylines, arcs, rings,
    ///     (rotated) rectangles, circles, ellipses, convex polygons, text at points and images, in the
    ///     canvas's design pixels with the origin at its top-left corner.
    ///     <para>
    ///         Shapes are triangles built every frame into reused buffers and drawn in one call per run -
    ///         no textures, no allocations when the drawing changes (pan, zoom, a moving gauge). Edges are
    ///         anti-aliased with a one-pixel fringe (<see cref="AntiAlias"/>). Colours are straight alpha
    ///         and multiplied with the canvas's render opacity; the layout scale and HiDPI are applied.
    ///     </para>
    ///     <para>
    ///         <see cref="Transform"/> maps drawing coordinates (e.g. world units of a floor plan) into the
    ///         canvas: positions, sizes and radii follow it, stroke thicknesses and text sizes stay in
    ///         design pixels (multiply by your zoom if they should scale).
    ///     </para>
    /// </summary>
    public sealed class DrawingContext
    {
        private const float MiterLimit = 4f;

        private static readonly RasterizerState ScissorNoCull = new RasterizerState { ScissorTestEnable = true, CullMode = CullMode.None };
        private static readonly Dictionary<GraphicsDevice, BasicEffect> Effects = new Dictionary<GraphicsDevice, BasicEffect>();

        private SpriteBatch? _spriteBatch;
        private bool _batchOpen;
        private Vector2 _origin;
        private Vector2 _scale = Vector2.One;
        private float _opacity = 1f;
        private Matrix _transform = Matrix.Identity;
        private bool _hasTransform;

        private VertexPositionColor[] _vertices = new VertexPositionColor[256];
        private int _vertexCount;
        private Vector2[] _points = new Vector2[64];
        private Vector2[] _normals = new Vector2[64];
        private float[] _miters = new float[64];

        internal DrawingContext()
        {
        }

        /// <summary>Size of the canvas in design pixels.</summary>
        public Size Size { get; private set; }

        /// <summary>Opacity applied to everything drawn (the canvas's accumulated render opacity).</summary>
        public float Opacity => _opacity;

        /// <summary>Render pixels per design pixel (layout scale, HiDPI, control scale).</summary>
        public Vector2 RenderScale => _scale;

        /// <summary>Smooth one-pixel edges (default). Off draws hard pixel edges, e.g. for pixel-art themes.</summary>
        public bool AntiAlias { get; set; } = true;

        /// <summary>
        ///     Maps drawing coordinates into canvas design pixels (2D affine: scale, rotation, translation),
        ///     e.g. <c>Matrix.CreateScale(zoom) * Matrix.CreateTranslation(panX, panY, 0)</c>. Identity by
        ///     default; reset for every frame.
        /// </summary>
        public Matrix Transform
        {
            get => _transform;
            set
            {
                _transform = value;
                _hasTransform = value != Matrix.Identity;
            }
        }

        /// <summary>Font for <see cref="DrawText"/>: a runtime font, or null for the canvas's resolved font.</summary>
        public UIFont? Font { get; set; }

        internal SpriteFont? SpriteFont { get; set; }

        internal UIFont? DefaultDynamicFont { get; set; }

        /// <summary>Vertices waiting to be drawn (tests read them in record mode).</summary>
        internal ReadOnlySpan<VertexPositionColor> PendingVertices => new ReadOnlySpan<VertexPositionColor>(_vertices, 0, _vertexCount);

        /// <summary>Sprite draws (text, images) issued, for tests in record mode.</summary>
        internal int SpriteDraws { get; private set; }

        /// <summary>Starts a frame. A null batch is record mode: geometry stays in <see cref="PendingVertices"/>.</summary>
        internal void Begin(SpriteBatch? spriteBatch, Rect renderRect, Vector2 scale, float opacity)
        {
            _spriteBatch = spriteBatch;
            _batchOpen = spriteBatch != null;
            _origin = new Vector2(renderRect.Left, renderRect.Top);
            _scale = scale;
            _opacity = MathHelper.Clamp(opacity, 0f, 1f);
            Size = new Size(scale.X > 0 ? renderRect.Width / scale.X : 0, scale.Y > 0 ? renderRect.Height / scale.Y : 0);
            Transform = Matrix.Identity;
            AntiAlias = true;
            Font = null;
            _vertexCount = 0;
            SpriteDraws = 0;
        }

        /// <summary>Draws what is pending and leaves the control batch open, as the screen expects.</summary>
        internal void End()
        {
            FlushGeometry();
            if (_spriteBatch != null && !_batchOpen)
            {
                Screen.ResumeControlBatch(_spriteBatch);
                _batchOpen = true;
            }
            _spriteBatch = null;
        }

        /// <summary>The canvas position (design pixels) of a drawing coordinate.</summary>
        public Vector2 ToCanvas(Vector2 point) => _hasTransform ? Vector2.Transform(point, _transform) : point;

        private Vector2 ToRender(Vector2 point)
        {
            var p = ToCanvas(point);
            return new Vector2(_origin.X + p.X * _scale.X, _origin.Y + p.Y * _scale.Y);
        }

        private float RenderLength(float designPixels) => designPixels * Math.Max(_scale.X, _scale.Y);

        private Color Premultiplied(Color color) => Brush.ApplyOpacity(color, _opacity);

        // ---------------------------------------------------------------- lines

        /// <summary>A straight line <paramref name="thickness"/> design pixels wide (hairlines below 1 px fade).</summary>
        public void DrawLine(Vector2 from, Vector2 to, Color color, float thickness = 1f, LineCap cap = LineCap.Flat)
        {
            EnsurePoints(2);
            _points[0] = ToRender(from);
            _points[1] = ToRender(to);
            StrokePoints(2, false, color, thickness, cap);
        }

        /// <summary>Connected line segments with mitred joins; <paramref name="closed"/> joins the last point to the first.</summary>
        public void DrawPolyline(ReadOnlySpan<Vector2> points, Color color, float thickness = 1f, bool closed = false, LineCap cap = LineCap.Flat)
        {
            if (points.Length < 2)
                return;
            EnsurePoints(points.Length);
            for (var i = 0; i < points.Length; i++)
                _points[i] = ToRender(points[i]);
            StrokePoints(points.Length, closed, color, thickness, cap);
        }

        // ---------------------------------------------------------------- arcs, circles, ellipses

        /// <summary>
        ///     An arc of a circle: <paramref name="startAngle"/> in radians (0 = right, clockwise on screen) and
        ///     <paramref name="sweepAngle"/> (negative = counter-clockwise). A ring gauge at 70 % is
        ///     <c>DrawArc(c, r, -MathHelper.PiOver2, MathHelper.TwoPi * 0.7f, color, 6)</c>.
        /// </summary>
        public void DrawArc(Vector2 center, float radius, float startAngle, float sweepAngle, Color color, float thickness = 1f, LineCap cap = LineCap.Flat)
            => DrawEllipticalArc(center, new Vector2(radius, radius), startAngle, sweepAngle, color, thickness, cap);

        /// <summary>An arc of an axis-aligned ellipse with radii <paramref name="radii"/>.</summary>
        public void DrawEllipticalArc(Vector2 center, Vector2 radii, float startAngle, float sweepAngle, Color color, float thickness = 1f, LineCap cap = LineCap.Flat)
        {
            if (sweepAngle == 0 || radii.X <= 0 || radii.Y <= 0)
                return;
            var full = Math.Abs(sweepAngle) >= MathHelper.TwoPi - 0.0001f;
            if (full)
                sweepAngle = Math.Sign(sweepAngle) * MathHelper.TwoPi;
            var segments = SegmentsFor(radii, Math.Abs(sweepAngle));
            var count = full ? segments : segments + 1;
            EnsurePoints(count);
            for (var i = 0; i < count; i++)
            {
                var angle = startAngle + sweepAngle * i / segments;
                _points[i] = ToRender(center + new Vector2(MathF.Cos(angle) * radii.X, MathF.Sin(angle) * radii.Y));
            }
            StrokePoints(count, full, color, thickness, cap);
        }

        /// <summary>The outline of a circle (a full ring).</summary>
        public void DrawCircle(Vector2 center, float radius, Color color, float thickness = 1f)
            => DrawEllipticalArc(center, new Vector2(radius, radius), 0, MathHelper.TwoPi, color, thickness);

        /// <summary>The outline of an axis-aligned ellipse.</summary>
        public void DrawEllipse(Vector2 center, Vector2 radii, Color color, float thickness = 1f)
            => DrawEllipticalArc(center, radii, 0, MathHelper.TwoPi, color, thickness);

        public void FillCircle(Vector2 center, float radius, Color color) => FillEllipse(center, new Vector2(radius, radius), color);

        public void FillEllipse(Vector2 center, Vector2 radii, Color color)
        {
            if (radii.X <= 0 || radii.Y <= 0)
                return;
            var segments = SegmentsFor(radii, MathHelper.TwoPi);
            EnsurePoints(segments);
            for (var i = 0; i < segments; i++)
            {
                var angle = MathHelper.TwoPi * i / segments;
                _points[i] = ToRender(center + new Vector2(MathF.Cos(angle) * radii.X, MathF.Sin(angle) * radii.Y));
            }
            FillPoints(segments, color);
        }

        /// <summary>A filled pie slice (sector) from the centre; angles as in <see cref="DrawArc"/>.</summary>
        public void FillPie(Vector2 center, float radius, float startAngle, float sweepAngle, Color color)
        {
            if (sweepAngle == 0 || radius <= 0)
                return;
            if (Math.Abs(sweepAngle) >= MathHelper.TwoPi - 0.0001f)
            {
                FillCircle(center, radius, color);
                return;
            }
            var segments = SegmentsFor(new Vector2(radius, radius), Math.Abs(sweepAngle));
            // A sector wider than a half circle is not convex: split it.
            if (Math.Abs(sweepAngle) > MathHelper.Pi)
            {
                var half = sweepAngle / 2;
                FillPie(center, radius, startAngle, half, color);
                FillPie(center, radius, startAngle + half, sweepAngle - half, color);
                return;
            }
            EnsurePoints(segments + 2);
            _points[0] = ToRender(center);
            for (var i = 0; i <= segments; i++)
            {
                var angle = startAngle + sweepAngle * i / segments;
                _points[i + 1] = ToRender(center + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius));
            }
            FillPoints(segments + 2, color);
        }

        // ---------------------------------------------------------------- rectangles, polygons

        public void FillRectangle(Rect rect, Color color)
            => FillRectangle(new Vector2(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2), new Vector2(rect.Width, rect.Height), 0, color);

        /// <summary>A rectangle of <paramref name="size"/> centred on <paramref name="center"/>, rotated by
        /// <paramref name="rotation"/> radians (clockwise on screen).</summary>
        public void FillRectangle(Vector2 center, Vector2 size, float rotation, Color color)
        {
            if (size.X <= 0 || size.Y <= 0)
                return;
            RectanglePoints(center, size, rotation);
            FillPoints(4, color);
        }

        public void DrawRectangle(Rect rect, Color color, float thickness = 1f)
            => DrawRectangle(new Vector2(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2), new Vector2(rect.Width, rect.Height), 0, color, thickness);

        /// <summary>The outline of a (rotated) rectangle; the stroke is centred on its edges.</summary>
        public void DrawRectangle(Vector2 center, Vector2 size, float rotation, Color color, float thickness = 1f)
        {
            if (size.X <= 0 || size.Y <= 0)
                return;
            RectanglePoints(center, size, rotation);
            StrokePoints(4, true, color, thickness, LineCap.Flat);
        }

        /// <summary>A filled convex polygon (a fan from the first point; concave shapes need splitting).</summary>
        public void FillPolygon(ReadOnlySpan<Vector2> points, Color color)
        {
            if (points.Length < 3)
                return;
            EnsurePoints(points.Length);
            for (var i = 0; i < points.Length; i++)
                _points[i] = ToRender(points[i]);
            FillPoints(points.Length, color);
        }

        private void RectanglePoints(Vector2 center, Vector2 size, float rotation)
        {
            EnsurePoints(4);
            var cos = MathF.Cos(rotation);
            var sin = MathF.Sin(rotation);
            var hx = size.X / 2;
            var hy = size.Y / 2;
            _points[0] = ToRender(center + Rotate(-hx, -hy, cos, sin));
            _points[1] = ToRender(center + Rotate(hx, -hy, cos, sin));
            _points[2] = ToRender(center + Rotate(hx, hy, cos, sin));
            _points[3] = ToRender(center + Rotate(-hx, hy, cos, sin));
        }

        private static Vector2 Rotate(float x, float y, float cos, float sin) => new Vector2(x * cos - y * sin, x * sin + y * cos);

        // ---------------------------------------------------------------- text and images

        /// <summary>
        ///     Text with its anchor at <paramref name="position"/>: <paramref name="anchor"/> (0..1 per axis)
        ///     picks the point of the text box placed there - (0, 0) top-left, (0.5, 0.5) centred. Size 0 uses
        ///     the font's default size.
        /// </summary>
        public void DrawText(string text, Vector2 position, Color color, float size = 0, Vector2 anchor = default)
        {
            if (string.IsNullOrEmpty(text))
                return;
            var at = ToRender(position);
            var dynamicFont = Font ?? DefaultDynamicFont;
            if (dynamicFont != null)
            {
                var pixelSize = size > 0 ? size : dynamicFont.DefaultSize;
                var measured = dynamicFont.MeasureString(text, pixelSize) * _scale;
                var topLeft = at - measured * anchor;
                if (BeginSprites())
                    dynamicFont.DrawString(_spriteBatch!, text, topLeft, Premultiplied(color), pixelSize, _scale);
            }
            else if (SpriteFont != null)
            {
                var baked = FontManager.GetBakedSize(SpriteFont);
                var fontScale = _scale * (size > 0 && baked > 0 ? size / baked : 1f);
                var measured = SpriteFont.MeasureString(text) * fontScale;
                var topLeft = at - measured * anchor;
                if (BeginSprites())
                    _spriteBatch!.DrawString(SpriteFont, text, topLeft, Premultiplied(color), 0, Vector2.Zero, fontScale, SpriteEffects.None, 0);
            }
        }

        /// <summary>Size of <paramref name="text"/> in design pixels, as <see cref="DrawText"/> draws it.</summary>
        public Vector2 MeasureText(string text, float size = 0)
        {
            if (string.IsNullOrEmpty(text))
                return Vector2.Zero;
            var dynamicFont = Font ?? DefaultDynamicFont;
            if (dynamicFont != null)
                return dynamicFont.MeasureString(text, size > 0 ? size : dynamicFont.DefaultSize);
            if (SpriteFont != null)
            {
                var baked = FontManager.GetBakedSize(SpriteFont);
                return SpriteFont.MeasureString(text) * (size > 0 && baked > 0 ? size / baked : 1f);
            }
            return Vector2.Zero;
        }

        /// <summary>
        ///     A texture centred on <paramref name="center"/> at <paramref name="size"/> design pixels, rotated by
        ///     <paramref name="rotation"/> radians; <paramref name="source"/> picks a sprite-sheet cell. The tint
        ///     is straight alpha (white = unchanged).
        /// </summary>
        public void DrawImage(Texture2D texture, Vector2 center, Vector2 size, Color tint, float rotation = 0, Rectangle? source = null)
        {
            if (texture == null || size.X <= 0 || size.Y <= 0)
                return;
            var src = source ?? texture.Bounds;
            if (src.Width <= 0 || src.Height <= 0)
                return;
            var at = ToRender(center);
            var scale = new Vector2(RenderLength(1) * size.X / src.Width, RenderLength(1) * size.Y / src.Height);
            if (_hasTransform)
            {
                var axis = Vector2.TransformNormal(Vector2.UnitX, _transform);
                scale *= axis.Length();
                rotation += MathF.Atan2(axis.Y, axis.X);
            }
            if (BeginSprites())
                _spriteBatch!.Draw(texture, at, src, Premultiplied(tint), rotation, new Vector2(src.Width / 2f, src.Height / 2f), scale, SpriteEffects.None, 0);
        }

        public void DrawImage(Texture2D texture, Rect destination, Color tint, Rectangle? source = null)
            => DrawImage(texture, new Vector2(destination.Left + destination.Width / 2, destination.Top + destination.Height / 2),
                new Vector2(destination.Width, destination.Height), tint, 0, source);

        // Text and images go through the control's sprite batch: pending triangles are drawn first so
        // the order of calls is the order on screen.
        private bool BeginSprites()
        {
            SpriteDraws++;
            if (_spriteBatch == null)
                return false;
            FlushGeometry();
            if (!_batchOpen)
            {
                Screen.ResumeControlBatch(_spriteBatch);
                _batchOpen = true;
            }
            return true;
        }

        // ---------------------------------------------------------------- geometry

        private int SegmentsFor(Vector2 radii, float sweep)
        {
            // Points about every 3 render pixels along the curve (finer on small circles), 6..512.
            var radius = Math.Max(radii.X, radii.Y);
            if (_hasTransform)
                radius *= Math.Max(Vector2.TransformNormal(Vector2.UnitX, _transform).Length(), Vector2.TransformNormal(Vector2.UnitY, _transform).Length());
            var length = RenderLength(radius) * sweep;
            var segments = (int)MathF.Ceiling(length / 3f);
            var minimum = Math.Max(2, (int)MathF.Ceiling(6 * sweep / MathHelper.TwoPi));
            return Math.Clamp(segments, minimum, 512);
        }

        private void EnsurePoints(int count)
        {
            if (_points.Length >= count)
                return;
            var size = Math.Max(count, _points.Length * 2);
            Array.Resize(ref _points, size);
            Array.Resize(ref _normals, size);
            Array.Resize(ref _miters, size);
        }

        private void EnsureVertices(int extra)
        {
            if (_vertexCount + extra <= _vertices.Length)
                return;
            Array.Resize(ref _vertices, Math.Max(_vertexCount + extra, _vertices.Length * 2));
        }

        private void AddTriangle(Vector2 a, Color ca, Vector2 b, Color cb, Vector2 c, Color cc)
        {
            _vertices[_vertexCount++] = new VertexPositionColor(new Vector3(a, 0), ca);
            _vertices[_vertexCount++] = new VertexPositionColor(new Vector3(b, 0), cb);
            _vertices[_vertexCount++] = new VertexPositionColor(new Vector3(c, 0), cc);
        }

        private void AddQuad(Vector2 a, Color ca, Vector2 b, Color cb, Vector2 c, Color cc, Vector2 d, Color cd)
        {
            // a-b along one edge, d-c along the opposite one.
            AddTriangle(a, ca, b, cb, c, cc);
            AddTriangle(a, ca, c, cc, d, cd);
        }

        /// <summary>Strokes _points[0..count) (render pixels) as a band <paramref name="thickness"/> design pixels wide.</summary>
        private void StrokePoints(int count, bool closed, Color color, float thickness, LineCap cap)
        {
            if (count < 2 || thickness <= 0 || color.A == 0)
                return;

            // Drop repeated points: they have no direction.
            var n = 0;
            for (var i = 0; i < count; i++)
            {
                if (n == 0 || Vector2.DistanceSquared(_points[i], _points[n - 1]) > 1e-6f)
                    _points[n++] = _points[i];
            }
            if (closed && n > 2 && Vector2.DistanceSquared(_points[0], _points[n - 1]) <= 1e-6f)
                n--;
            if (n < 2)
                return;
            if (n == 2)
                closed = false;

            var width = RenderLength(thickness);
            var core = Premultiplied(color);
            if (width < 1f && AntiAlias)
            {
                // Hairline: one pixel wide at proportionally less alpha.
                core *= width;
                width = 1f;
            }
            var fringe = AntiAlias ? 0.5f : 0f;
            var inner = Math.Max(0f, width / 2 - fringe);
            var outer = width / 2 + fringe;

            // Per-point join normals (mitred, limited).
            for (var i = 0; i < n; i++)
            {
                var hasPrev = closed || i > 0;
                var hasNext = closed || i < n - 1;
                var prev = hasPrev ? _points[i] - _points[(i - 1 + n) % n] : Vector2.Zero;
                var next = hasNext ? _points[(i + 1) % n] - _points[i] : Vector2.Zero;
                var dirPrev = hasPrev ? Vector2.Normalize(prev) : Vector2.Normalize(next);
                var dirNext = hasNext ? Vector2.Normalize(next) : dirPrev;
                var normalPrev = new Vector2(-dirPrev.Y, dirPrev.X);
                var normalNext = new Vector2(-dirNext.Y, dirNext.X);
                var miter = normalPrev + normalNext;
                if (miter.LengthSquared() < 1e-6f)
                    miter = normalNext;
                miter.Normalize();
                var dot = Vector2.Dot(miter, normalNext);
                _normals[i] = miter;
                _miters[i] = Math.Min(MiterLimit, 1f / Math.Max(dot, 1f / MiterLimit));
            }

            var transparent = Color.Transparent;
            var segments = closed ? n : n - 1;
            EnsureVertices(segments * 18 + 64);
            for (var s = 0; s < segments; s++)
            {
                var a = s;
                var b = (s + 1) % n;
                Vector2 pa = _points[a], pb = _points[b];
                Vector2 na = _normals[a] * _miters[a], nb = _normals[b] * _miters[b];
                if (inner > 0)
                    AddQuad(pa + na * inner, core, pb + nb * inner, core, pb - nb * inner, core, pa - na * inner, core);
                if (fringe > 0)
                {
                    AddQuad(pa + na * outer, transparent, pb + nb * outer, transparent, pb + nb * inner, core, pa + na * inner, core);
                    AddQuad(pa - na * inner, core, pb - nb * inner, core, pb - nb * outer, transparent, pa - na * outer, transparent);
                }
            }

            if (!closed)
            {
                AddCap(0, Vector2.Normalize(_points[0] - _points[1]), width, inner, outer, fringe, cap, core);
                AddCap(n - 1, Vector2.Normalize(_points[n - 1] - _points[n - 2]), width, inner, outer, fringe, cap, core);
            }
        }

        // Extends an open end by the cap (square: half the width, solid) and closes it with a fringe.
        private void AddCap(int index, Vector2 outward, float width, float inner, float outer, float fringe, LineCap cap, Color core)
        {
            var p = _points[index];
            var normal = new Vector2(-outward.Y, outward.X);
            var extend = cap == LineCap.Square ? width / 2 : 0f;
            var end = p + outward * extend;
            if (extend > 0)
            {
                // The solid extension, full width minus the fringe on its sides.
                AddQuad(p + normal * inner, core, end + normal * inner, core, end - normal * inner, core, p - normal * inner, core);
                if (fringe > 0)
                {
                    AddQuad(p + normal * outer, Color.Transparent, end + normal * outer, Color.Transparent, end + normal * inner, core, p + normal * inner, core);
                    AddQuad(p - normal * inner, core, end - normal * inner, core, end - normal * outer, Color.Transparent, p - normal * outer, Color.Transparent);
                }
            }
            if (fringe <= 0)
                return;
            var tip = end + outward * fringe;
            var transparent = Color.Transparent;
            AddQuad(end + normal * inner, core, tip + normal * inner, transparent, tip - normal * inner, transparent, end - normal * inner, core);
            AddTriangle(end + normal * inner, core, end + normal * outer, transparent, tip + normal * inner, transparent);
            AddTriangle(end - normal * inner, core, tip - normal * inner, transparent, end - normal * outer, transparent);
        }

        /// <summary>Fills the convex polygon _points[0..count) (render pixels) with an anti-aliased fringe.</summary>
        private void FillPoints(int count, Color color)
        {
            if (count < 3 || color.A == 0)
                return;
            var core = Premultiplied(color);
            // Outward normals need the winding: positive area = clockwise on screen (y down).
            var area = 0f;
            for (var i = 0; i < count; i++)
            {
                var a = _points[i];
                var b = _points[(i + 1) % count];
                area += a.X * b.Y - b.X * a.Y;
            }
            if (Math.Abs(area) < 1e-6f)
                return;
            var outwardSign = area > 0 ? -1f : 1f;

            var fringe = AntiAlias ? 0.5f : 0f;
            for (var i = 0; i < count; i++)
            {
                var prev = _points[i] - _points[(i - 1 + count) % count];
                var next = _points[(i + 1) % count] - _points[i];
                if (prev.LengthSquared() < 1e-12f || next.LengthSquared() < 1e-12f)
                {
                    _normals[i] = Vector2.Zero;
                    _miters[i] = 0;
                    continue;
                }
                prev.Normalize();
                next.Normalize();
                var normalPrev = new Vector2(-prev.Y, prev.X) * outwardSign;
                var normalNext = new Vector2(-next.Y, next.X) * outwardSign;
                var miter = normalPrev + normalNext;
                if (miter.LengthSquared() < 1e-6f)
                    miter = normalNext;
                miter.Normalize();
                _normals[i] = miter;
                _miters[i] = Math.Min(MiterLimit, 1f / Math.Max(Vector2.Dot(miter, normalNext), 1f / MiterLimit));
            }

            EnsureVertices((count - 2) * 3 + count * 6);
            var transparent = Color.Transparent;
            // Inner polygon shrunk by half the fringe, fanned from the first point.
            var first = _points[0] - _normals[0] * (_miters[0] * fringe);
            for (var i = 1; i < count - 1; i++)
            {
                var b = _points[i] - _normals[i] * (_miters[i] * fringe);
                var c = _points[i + 1] - _normals[i + 1] * (_miters[i + 1] * fringe);
                AddTriangle(first, core, b, core, c, core);
            }
            if (fringe <= 0)
                return;
            for (var i = 0; i < count; i++)
            {
                var j = (i + 1) % count;
                var oi = _normals[i] * (_miters[i] * fringe);
                var oj = _normals[j] * (_miters[j] * fringe);
                AddQuad(_points[i] - oi, core, _points[j] - oj, core, _points[j] + oj, transparent, _points[i] + oi, transparent);
            }
        }

        private void FlushGeometry()
        {
            if (_vertexCount == 0 || _spriteBatch == null)
                return;
            if (_batchOpen)
            {
                // Draws what the batch holds so far (the canvas's chrome, earlier text) first.
                _spriteBatch.End();
                _batchOpen = false;
            }

            var device = _spriteBatch.GraphicsDevice;
            var effect = EffectFor(device);
            var viewport = device.Viewport;
            effect.Projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1);
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = ScissorNoCull;
            effect.CurrentTechnique.Passes[0].Apply();
            // Chunks stay well below the primitive limit of every backend.
            const int chunk = 3 * 20000;
            for (var offset = 0; offset < _vertexCount; offset += chunk)
            {
                var vertices = Math.Min(chunk, _vertexCount - offset);
                device.DrawUserPrimitives(PrimitiveType.TriangleList, _vertices, offset, vertices / 3);
            }
            _vertexCount = 0;
        }

        private static BasicEffect EffectFor(GraphicsDevice device)
        {
            lock (Effects)
            {
                if (Effects.TryGetValue(device, out var effect) && !effect.IsDisposed)
                    return effect;
                effect = new BasicEffect(device)
                {
                    VertexColorEnabled = true,
                    TextureEnabled = false,
                    LightingEnabled = false,
                    World = Matrix.Identity,
                    View = Matrix.Identity
                };
                Effects[device] = effect;
                device.Disposing += (_, _) =>
                {
                    lock (Effects)
                    {
                        if (Effects.Remove(device, out var disposed))
                            disposed.Dispose();
                    }
                };
                return effect;
            }
        }
    }
}
