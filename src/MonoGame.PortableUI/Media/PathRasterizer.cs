using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    ///     CPU rasterizer for <see cref="PathGeometry"/>: fills (non-zero or even-odd) and strokes the
    ///     flattened figures into premultiplied pixels with 4×4 supersampled anti-aliasing. Meant for
    ///     load time or occasional size changes — the result is cached as a texture and drawn as a
    ///     sprite (see docs/path-drawing.md for why this beats a per-frame triangulator here).
    /// </summary>
    public static class PathRasterizer
    {
        private const int Samples = 4;

        /// <summary>Rasterizes into a <paramref name="width"/>×<paramref name="height"/> premultiplied buffer;
        /// the view box is scaled to fill it.</summary>
        public static Color[] Rasterize(PathGeometry geometry, int width, int height, Color? fill, Color? stroke = null, float strokeWidth = 1)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            width = Math.Max(1, width);
            height = Math.Max(1, height);

            var view = geometry.ViewBox;
            var scaleX = width / Math.Max(0.0001f, view.Width);
            var scaleY = height / Math.Max(0.0001f, view.Height);
            var figures = new List<(Vector2[] Points, bool Closed)>();
            for (var f = 0; f < geometry.Figures.Count; f++)
            {
                var source = geometry.Figures[f];
                var points = new Vector2[source.Count];
                for (var i = 0; i < source.Count; i++)
                    points[i] = new Vector2((source[i].X - view.X) * scaleX, (source[i].Y - view.Y) * scaleY);
                figures.Add((points, geometry.IsClosed(f)));
            }

            // Stroke width is given in view units; scale it with the shape.
            var halfStroke = strokeWidth * (scaleX + scaleY) / 4f;
            var fillColor = fill.HasValue ? Premultiply(fill.Value) : (Color?)null;
            var strokeColor = stroke.HasValue ? Premultiply(stroke.Value) : (Color?)null;
            var data = new Color[width * height];
            const float step = 1f / Samples;

            var coverage = fillColor.HasValue ? FillCoverage(figures, width, height, geometry.EvenOddFill) : null;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var fillHits = coverage?[y * width + x] ?? 0;
                    var strokeHits = 0;
                    if (strokeColor.HasValue)
                    {
                        for (var sy = 0; sy < Samples; sy++)
                        {
                            for (var sx = 0; sx < Samples; sx++)
                            {
                                if (IsOnStroke(figures, new Vector2(x + (sx + 0.5f) * step, y + (sy + 0.5f) * step), halfStroke))
                                    strokeHits++;
                            }
                        }
                    }

                    var total = Samples * Samples;
                    var pixel = Color.Transparent;
                    if (fillColor.HasValue && fillHits > 0)
                        pixel = fillColor.Value * (fillHits / (float)total);
                    if (strokeColor.HasValue && strokeHits > 0)
                        pixel = Over(strokeColor.Value * (strokeHits / (float)total), pixel);
                    data[y * width + x] = pixel;
                }
            }

            return data;
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<GraphicsDevice, StrongBox<int>> ResetGenerations = new();

        /// <summary>Increments whenever <paramref name="device"/> is reset (Android context loss
        /// recreates textures empty), so cached path textures know to rasterize again.</summary>
        internal static int GetResetGeneration(GraphicsDevice device)
        {
            return ResetGenerations.GetValue(device, static d =>
            {
                var box = new StrongBox<int>(0);
                d.DeviceReset += (_, _) => box.Value++;
                return box;
            }).Value;
        }

        /// <summary>Rasterizes into a new texture (caller owns it).</summary>
        public static Texture2D CreateTexture(GraphicsDevice device, PathGeometry geometry, int width, int height, Color? fill, Color? stroke = null, float strokeWidth = 1)
        {
            var data = Rasterize(geometry, width, height, fill, stroke, strokeWidth);
            var texture = new Texture2D(device, Math.Max(1, width), Math.Max(1, height));
            texture.SetData(data);
            return texture;
        }

        /// <summary>
        ///     Samples inside the fill per pixel (0..Samples²), by scanlines: each sample row crosses the edges once and
        ///     the spans between crossings are filled by their winding. Testing every sample against every edge
        ///     instead took seconds for curve-rich icons (thousands of edges) at phone sizes.
        /// </summary>
        private static int[] FillCoverage(List<(Vector2[] Points, bool Closed)> figures, int width, int height, bool evenOdd)
        {
            var coverage = new int[width * height];
            var crossings = new List<(float X, int Direction)>();
            var columns = width * Samples;
            for (var row = 0; row < height * Samples; row++)
            {
                var y = (row + 0.5f) / Samples;
                crossings.Clear();
                foreach (var (points, _) in figures)
                {
                    // Every figure counts as closed for filling, like SVG.
                    for (var i = 0; i < points.Length; i++)
                    {
                        var a = points[i];
                        var b = points[(i + 1) % points.Length];
                        int direction;
                        if (a.Y <= y && b.Y > y)
                            direction = 1;
                        else if (b.Y <= y && a.Y > y)
                            direction = -1;
                        else
                            continue;
                        crossings.Add((a.X + (y - a.Y) * (b.X - a.X) / (b.Y - a.Y), direction));
                    }
                }
                if (crossings.Count < 2)
                    continue;
                crossings.Sort((l, r) => l.X.CompareTo(r.X));

                var winding = 0;
                var count = 0;
                var pixelRow = row / Samples * width;
                for (var i = 0; i < crossings.Count - 1; i++)
                {
                    winding += crossings[i].Direction;
                    count++;
                    if (evenOdd ? (count & 1) == 0 : winding == 0)
                        continue;
                    // Sample columns whose centre lies in [this crossing, next crossing).
                    var first = Math.Max(0, (int)Math.Ceiling(crossings[i].X * Samples - 0.5f));
                    var last = Math.Min(columns - 1, (int)Math.Ceiling(crossings[i + 1].X * Samples - 0.5f) - 1);
                    for (var column = first; column <= last; column++)
                        coverage[pixelRow + column / Samples]++;
                }
            }
            return coverage;
        }

        private static bool IsOnStroke(List<(Vector2[] Points, bool Closed)> figures, Vector2 p, float halfWidth)
        {
            var limit = halfWidth * halfWidth;
            foreach (var (points, closed) in figures)
            {
                var count = closed ? points.Length : points.Length - 1;
                for (var i = 0; i < count; i++)
                {
                    if (DistanceSquaredToSegment(p, points[i], points[(i + 1) % points.Length]) <= limit)
                        return true;
                }
            }
            return false;
        }

        private static float DistanceSquaredToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var lengthSquared = ab.LengthSquared();
            var t = lengthSquared <= 0 ? 0 : MathHelper.Clamp(Vector2.Dot(p - a, ab) / lengthSquared, 0, 1);
            return Vector2.DistanceSquared(p, a + ab * t);
        }

        private static Color Premultiply(Color color) => Color.FromNonPremultiplied(color.R, color.G, color.B, color.A);

        // Premultiplied "source over destination".
        private static Color Over(Color source, Color destination)
        {
            var inverse = 1 - source.A / 255f;
            return new Color(
                (byte)Math.Min(255, source.R + destination.R * inverse),
                (byte)Math.Min(255, source.G + destination.G * inverse),
                (byte)Math.Min(255, source.B + destination.B * inverse),
                (byte)Math.Min(255, source.A + destination.A * inverse));
        }
    }
}
