using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    internal static class RoundedRectRenderer
    {
        private static readonly Dictionary<int, Texture2D> CornerMasks = new Dictionary<int, Texture2D>();
        private static readonly Dictionary<(int Radius, int Thickness), Texture2D> CornerRingMasks = new Dictionary<(int, int), Texture2D>();

        /// <summary>Draws a border that follows the corner radius: 4 edge strips + quarter-ring corners. Expects a premultiplied color.</summary>
        public static void DrawBorder(SpriteBatch spriteBatch, Rect rect, CornerRadius radius, Thickness thickness, Color color)
        {
            if (rect.Width <= 0 || rect.Height <= 0 || color.A == 0)
                return;

            radius = Clamp(radius, rect);
            if (radius.IsEmpty)
                return;

            // Edge strips between the rounded corners.
            var top = new Rect(rect.Left + radius.TopLeft, rect.Top, Math.Max(0, rect.Width - radius.TopLeft - radius.TopRight), thickness.Top);
            var bottom = new Rect(rect.Left + radius.BottomLeft, rect.Bottom - thickness.Bottom, Math.Max(0, rect.Width - radius.BottomLeft - radius.BottomRight), thickness.Bottom);
            var left = new Rect(rect.Left, rect.Top + radius.TopLeft, thickness.Left, Math.Max(0, rect.Height - radius.TopLeft - radius.BottomLeft));
            var right = new Rect(rect.Right - thickness.Right, rect.Top + radius.TopRight, thickness.Right, Math.Max(0, rect.Height - radius.TopRight - radius.BottomRight));
            if (top.Height > 0 && top.Width > 0)
                spriteBatch.Draw(SolidColorBrush.Pixel, top, color);
            if (bottom.Height > 0 && bottom.Width > 0)
                spriteBatch.Draw(SolidColorBrush.Pixel, bottom, color);
            if (left.Width > 0 && left.Height > 0)
                spriteBatch.Draw(SolidColorBrush.Pixel, left, color);
            if (right.Width > 0 && right.Height > 0)
                spriteBatch.Draw(SolidColorBrush.Pixel, right, color);

            DrawCornerRing(spriteBatch, rect.Left, rect.Top, radius.TopLeft, Math.Max(thickness.Top, thickness.Left), color, SpriteEffects.None);
            DrawCornerRing(spriteBatch, rect.Right - radius.TopRight, rect.Top, radius.TopRight, Math.Max(thickness.Top, thickness.Right), color, SpriteEffects.FlipHorizontally);
            DrawCornerRing(spriteBatch, rect.Right - radius.BottomRight, rect.Bottom - radius.BottomRight, radius.BottomRight, Math.Max(thickness.Bottom, thickness.Right), color, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically);
            DrawCornerRing(spriteBatch, rect.Left, rect.Bottom - radius.BottomLeft, radius.BottomLeft, Math.Max(thickness.Bottom, thickness.Left), color, SpriteEffects.FlipVertically);
        }

        private static void DrawCornerRing(SpriteBatch spriteBatch, float x, float y, float radius, float thickness, Color color, SpriteEffects effects)
        {
            var size = (int)Math.Ceiling(radius);
            if (size <= 0 || thickness <= 0)
                return;

            var mask = GetCornerRingMask(size, Math.Max(1, (int)Math.Round(thickness)));
            spriteBatch.Draw(mask, new Rect(x, y, radius, radius), null, color, 0, Vector2.Zero, effects, 0);
        }

        private static Texture2D GetCornerRingMask(int radius, int thickness)
        {
            if (CornerRingMasks.TryGetValue((radius, thickness), out var texture))
                return texture;

            var data = new Color[radius * radius];
            var center = new Vector2(radius, radius);
            for (var y = 0; y < radius; y++)
            {
                for (var x = 0; x < radius; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    var distance = Vector2.Distance(point, center);
                    var outer = MathHelper.Clamp(radius + 0.5f - distance, 0, 1);
                    var inner = MathHelper.Clamp(radius - thickness + 0.5f - distance, 0, 1);
                    var coverage = (byte)(MathHelper.Clamp(outer - inner, 0, 1) * 255);
                    data[y * radius + x] = new Color(coverage, coverage, coverage, coverage);
                }
            }

            texture = new Texture2D(ScreenEngine.Instance!.Game.GraphicsDevice, radius, radius);
            texture.SetData(data);
            CornerRingMasks[(radius, thickness)] = texture;
            return texture;
        }

        /// <summary>Multiplies premultiplied texel data with rounded-rect coverage so textures get rounded corners.</summary>
        public static void ApplyCornerMask(Color[] data, int width, int height, CornerRadius radius)
        {
            radius = Clamp(radius, new Rect(0, 0, width, height));
            if (radius.IsEmpty)
                return;

            MaskCorner(data, width, height, radius.TopLeft, new Vector2(radius.TopLeft, radius.TopLeft), 0, 0);
            MaskCorner(data, width, height, radius.TopRight, new Vector2(width - radius.TopRight, radius.TopRight), width - (int)Math.Ceiling(radius.TopRight), 0);
            MaskCorner(data, width, height, radius.BottomRight, new Vector2(width - radius.BottomRight, height - radius.BottomRight), width - (int)Math.Ceiling(radius.BottomRight), height - (int)Math.Ceiling(radius.BottomRight));
            MaskCorner(data, width, height, radius.BottomLeft, new Vector2(radius.BottomLeft, height - radius.BottomLeft), 0, height - (int)Math.Ceiling(radius.BottomLeft));
        }

        private static void MaskCorner(Color[] data, int width, int height, float radius, Vector2 center, int startX, int startY)
        {
            var size = (int)Math.Ceiling(radius);
            if (size <= 0)
                return;

            var endX = Math.Min(width, startX + size);
            var endY = Math.Min(height, startY + size);
            for (var y = Math.Max(0, startY); y < endY; y++)
            {
                for (var x = Math.Max(0, startX); x < endX; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    var coverage = MathHelper.Clamp(radius + 0.5f - distance, 0, 1);
                    if (coverage >= 1)
                        continue;

                    var color = data[y * width + x];
                    data[y * width + x] = new Color(
                        (byte)(color.R * coverage),
                        (byte)(color.G * coverage),
                        (byte)(color.B * coverage),
                        (byte)(color.A * coverage));
                }
            }
        }

        public static void DrawSolid(SpriteBatch spriteBatch, Rect rect, CornerRadius radius, Color color)
        {
            if (rect.Width <= 0 || rect.Height <= 0 || color.A == 0)
                return;

            radius = Clamp(radius, rect);
            if (radius.IsEmpty)
            {
                spriteBatch.Draw(SolidColorBrush.Pixel, rect, color);
                return;
            }

            // Overlap the straight fills 1px into the corner regions. On a pill/circle the middle
            // bands would otherwise collapse to zero width/height and only the four corner masks
            // draw, leaving a hairline "+" seam where they meet; the overlap bridges it.
            foreach (var fillRect in GetFillRects(rect, radius, overlap: 1f))
            {
                if (fillRect.Width > 0 && fillRect.Height > 0)
                    spriteBatch.Draw(SolidColorBrush.Pixel, fillRect, color);
            }

            DrawCorner(spriteBatch, rect.Left, rect.Top, radius.TopLeft, color, SpriteEffects.None);
            DrawCorner(spriteBatch, rect.Right - radius.TopRight, rect.Top, radius.TopRight, color, SpriteEffects.FlipHorizontally);
            DrawCorner(spriteBatch, rect.Right - radius.BottomRight, rect.Bottom - radius.BottomRight, radius.BottomRight, color, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically);
            DrawCorner(spriteBatch, rect.Left, rect.Bottom - radius.BottomLeft, radius.BottomLeft, color, SpriteEffects.FlipVertically);
        }

        internal static IEnumerable<Rect> GetFillRects(Rect rect, CornerRadius radius, float overlap = 0f)
        {
            radius = Clamp(radius, rect);
            var left = Math.Max(radius.TopLeft, radius.BottomLeft);
            var right = Math.Max(radius.TopRight, radius.BottomRight);
            var top = Math.Max(radius.TopLeft, radius.TopRight);
            var bottom = Math.Max(radius.BottomLeft, radius.BottomRight);

            // Inset the band boundaries by `overlap` so each straight fill reaches `overlap` px into
            // the corner squares, sharing an edge with the corner masks instead of leaving a seam.
            var l = Math.Max(0, left - overlap);
            var r = Math.Max(0, right - overlap);
            var t = Math.Max(0, top - overlap);
            var b = Math.Max(0, bottom - overlap);

            yield return new Rect(rect.Left + l, rect.Top, Math.Max(0, rect.Width - l - r), rect.Height);
            yield return new Rect(rect.Left, rect.Top + t, left, Math.Max(0, rect.Height - t - b));
            yield return new Rect(rect.Right - right, rect.Top + t, right, Math.Max(0, rect.Height - t - b));
        }

        private static CornerRadius Clamp(CornerRadius radius, Rect rect)
        {
            var max = Math.Max(0, Math.Min(rect.Width, rect.Height) / 2);
            return new CornerRadius(
                Math.Min(radius.TopLeft, max),
                Math.Min(radius.TopRight, max),
                Math.Min(radius.BottomRight, max),
                Math.Min(radius.BottomLeft, max));
        }

        private static void DrawCorner(SpriteBatch spriteBatch, float x, float y, float radius, Color color, SpriteEffects effects)
        {
            var size = (int)Math.Ceiling(radius);
            if (size <= 0)
                return;

            var mask = GetCornerMask(size);
            spriteBatch.Draw(mask, new Rect(x, y, radius, radius), null, color, 0, Vector2.Zero, effects, 0);
        }

        private static Texture2D GetCornerMask(int radius)
        {
            if (CornerMasks.TryGetValue(radius, out var texture))
                return texture;

            var data = new Color[radius * radius];
            var center = new Vector2(radius, radius);
            for (var y = 0; y < radius; y++)
            {
                for (var x = 0; x < radius; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    var distance = Vector2.Distance(point, center);
                    var coverage = (byte)(MathHelper.Clamp(radius + 0.5f - distance, 0, 1) * 255);
                    // Premultiplied: white * coverage so AA edges composite correctly under AlphaBlend.
                    data[y * radius + x] = new Color(coverage, coverage, coverage, coverage);
                }
            }

            texture = new Texture2D(ScreenEngine.Instance!.Game.GraphicsDevice, radius, radius);
            texture.SetData(data);
            CornerMasks[radius] = texture;
            return texture;
        }
    }
}
