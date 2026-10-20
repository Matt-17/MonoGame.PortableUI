using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    ///     Draws a filled circle with an optionally soft edge, clipped to a rounded rectangle, one pixel row
    ///     at a time: a solid core span plus stretched alpha ramps for the edge. No texture per size and no
    ///     scissor, so an animated ripple batches with everything else.
    /// </summary>
    internal static class RippleRenderer
    {
        private const int RampSize = 64;
        private static Texture2D? _ramp;

        // softEdge: width of the soft edge as a fraction of the radius (0 = hard, anti-aliased edge).
        public static void Draw(SpriteBatch spriteBatch, Rect clip, CornerRadius radius, Vector2 center, float circleRadius, float softEdge, Color color)
        {
            if (circleRadius <= 0 || clip.Width <= 0 || clip.Height <= 0 || color.A == 0)
                return;
            var pixel = Primitives.Pixel(spriteBatch);
            var ramp = Ramp(spriteBatch.GraphicsDevice);
            // A hard edge still gets one pixel of ramp: its anti-aliasing.
            var feather = Math.Max(1f, circleRadius * Math.Clamp(softEdge, 0, 1));
            var inner = Math.Max(0, circleRadius - feather);
            var top = (int)MathF.Floor(Math.Max(clip.Top, center.Y - circleRadius));
            var bottom = (int)MathF.Ceiling(Math.Min(clip.Bottom, center.Y + circleRadius));
            for (var y = top; y < bottom; y++)
            {
                var rowCenter = y + 0.5f;
                var dy = Math.Abs(rowCenter - center.Y);
                if (dy >= circleRadius || rowCenter < clip.Top || rowCenter > clip.Bottom)
                    continue;
                var minX = clip.Left + CornerInset(rowCenter, clip, radius.TopLeft, radius.BottomLeft);
                var maxX = clip.Right - CornerInset(rowCenter, clip, radius.TopRight, radius.BottomRight);
                var outer = MathF.Sqrt(circleRadius * circleRadius - dy * dy);
                if (dy < inner)
                {
                    var core = MathF.Sqrt(inner * inner - dy * dy);
                    Ramp(spriteBatch, ramp, y, center.X - outer, center.X - core, 0, 1, minX, maxX, color);
                    Span(spriteBatch, pixel, y, Math.Max(minX, center.X - core), Math.Min(maxX, center.X + core), color);
                    Ramp(spriteBatch, ramp, y, center.X + core, center.X + outer, 1, 0, minX, maxX, color);
                }
                else
                {
                    // The row only crosses the soft edge: it peaks in the middle at its depth into the edge.
                    var peak = Math.Clamp((circleRadius - dy) / feather, 0, 1);
                    Ramp(spriteBatch, ramp, y, center.X - outer, center.X, 0, peak, minX, maxX, color);
                    Ramp(spriteBatch, ramp, y, center.X, center.X + outer, peak, 0, minX, maxX, color);
                }
            }
        }

        // How far a rounded corner cuts into the row at y (0 outside the corner's rows).
        private static float CornerInset(float y, Rect rect, float topRadius, float bottomRadius)
        {
            if (topRadius > 0 && y < rect.Top + topRadius)
            {
                var d = rect.Top + topRadius - y;
                return topRadius - MathF.Sqrt(Math.Max(0, topRadius * topRadius - d * d));
            }
            if (bottomRadius > 0 && y > rect.Bottom - bottomRadius)
            {
                var d = y - (rect.Bottom - bottomRadius);
                return bottomRadius - MathF.Sqrt(Math.Max(0, bottomRadius * bottomRadius - d * d));
            }
            return 0;
        }

        private static void Span(SpriteBatch spriteBatch, Texture2D pixel, int y, float left, float right, Color color)
        {
            if (right - left <= 0)
                return;
            spriteBatch.Draw(pixel, new Vector2(left, y), null, color, 0, Vector2.Zero, new Vector2(right - left, 1), SpriteEffects.None, 0);
        }

        // A horizontal alpha ramp from alpha `from` at `start` to `to` at `end`, clipped to [minX, maxX].
        private static void Ramp(SpriteBatch spriteBatch, Texture2D ramp, int y, float start, float end, float from, float to, float minX, float maxX, Color color)
        {
            var width = end - start;
            if (width <= 0)
                return;
            var left = Math.Max(start, minX);
            var right = Math.Min(end, maxX);
            if (right <= left)
                return;
            var a = from + (to - from) * (left - start) / width;
            var b = from + (to - from) * (right - start) / width;
            // Sample the ramp texture between the two alphas (texel centers), mirrored when falling.
            var u0 = a * (RampSize - 1) + 0.5f;
            var u1 = b * (RampSize - 1) + 0.5f;
            var effects = u1 < u0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            var lo = Math.Min(u0, u1);
            var hi = Math.Max(u0, u1);
            var source = new Rectangle((int)MathF.Floor(lo), 0, Math.Max(1, (int)MathF.Ceiling(hi) - (int)MathF.Floor(lo)), 1);
            spriteBatch.Draw(ramp, new Vector2(left, y), source, color, 0, Vector2.Zero, new Vector2((right - left) / source.Width, 1), effects, 0);
        }

        private static Texture2D Ramp(GraphicsDevice device)
        {
            if (_ramp is { IsDisposed: false } existing && ReferenceEquals(existing.GraphicsDevice, device))
                return existing;
            var data = new Color[RampSize];
            for (var i = 0; i < RampSize; i++)
            {
                var alpha = i / (float)(RampSize - 1);
                data[i] = Color.White * alpha; // premultiplied
            }
            _ramp = new Texture2D(device, RampSize, 1);
            _ramp.SetData(data);
            return _ramp;
        }
    }
}
