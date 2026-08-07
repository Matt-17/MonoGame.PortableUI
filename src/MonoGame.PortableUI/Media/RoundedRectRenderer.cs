using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    internal static class RoundedRectRenderer
    {
        // Per-device caches with DeviceReset/Disposing cleanup (same pattern as BrushTextureCache):
        // a process-wide static Dictionary would leak textures across device resets and mix
        // textures between devices when surface engines exist.
        private static readonly object SyncRoot = new object();
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<GraphicsDevice, DeviceCache> Caches =
            new System.Runtime.CompilerServices.ConditionalWeakTable<GraphicsDevice, DeviceCache>();

        // Full W×H masks are keyed by pixel size, so animated/resizing controls would otherwise
        // grow the cache without bound. On overflow the current generation is retired and only
        // disposed on the *next* overflow, so textures referenced by an unflushed SpriteBatch
        // survive the frame they were drawn in.
        private const int MaxRoundedRectMasks = 128;

        private sealed class DeviceCache
        {
            public readonly Dictionary<int, Texture2D> CornerMasks = new Dictionary<int, Texture2D>();
            public readonly Dictionary<(int Radius, int Thickness), Texture2D> CornerRingMasks = new Dictionary<(int, int), Texture2D>();
            public readonly Dictionary<RoundedRectMaskKey, Texture2D> RoundedRectMasks = new Dictionary<RoundedRectMaskKey, Texture2D>();
            public readonly List<Texture2D> RetiredMasks = new List<Texture2D>();
        }

        private static DeviceCache GetCache(GraphicsDevice device)
        {
            lock (SyncRoot)
            {
                return Caches.GetValue(device, CreateCache);
            }
        }

        private static DeviceCache CreateCache(GraphicsDevice device)
        {
            var cache = new DeviceCache();
            device.DeviceReset += (_, _) => Clear(cache);
            device.Disposing += (_, _) => Clear(cache);
            return cache;
        }

        private static void Clear(DeviceCache cache)
        {
            lock (SyncRoot)
            {
                foreach (var texture in cache.CornerMasks.Values)
                    texture.Dispose();
                foreach (var texture in cache.CornerRingMasks.Values)
                    texture.Dispose();
                foreach (var texture in cache.RoundedRectMasks.Values)
                    texture.Dispose();
                foreach (var texture in cache.RetiredMasks)
                    texture.Dispose();
                cache.CornerMasks.Clear();
                cache.CornerRingMasks.Clear();
                cache.RoundedRectMasks.Clear();
                cache.RetiredMasks.Clear();
            }
        }

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

        /// <summary>
        /// Draws a rounded border whose colour blends diagonally from <paramref name="light"/> at the
        /// top-left to <paramref name="dark"/> at the bottom-right (a soft glass bevel). Each edge and
        /// corner is tinted by the gradient value at its position so the transition wraps the corners.
        /// </summary>
        public static void DrawBevelBorder(SpriteBatch spriteBatch, Rect rect, CornerRadius radius, Thickness thickness, Color light, Color dark, float opacity)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            radius = Clamp(radius, rect);
            if (radius.IsEmpty)
                return;

            Color At(float cx, float cy)
            {
                var g = MathHelper.Clamp(((cx - rect.Left) / rect.Width + (cy - rect.Top) / rect.Height) / 2f, 0, 1);
                return Brush.ApplyOpacity(Color.Lerp(light, dark, g), opacity);
            }

            var top = new Rect(rect.Left + radius.TopLeft, rect.Top, Math.Max(0, rect.Width - radius.TopLeft - radius.TopRight), thickness.Top);
            var bottom = new Rect(rect.Left + radius.BottomLeft, rect.Bottom - thickness.Bottom, Math.Max(0, rect.Width - radius.BottomLeft - radius.BottomRight), thickness.Bottom);
            var left = new Rect(rect.Left, rect.Top + radius.TopLeft, thickness.Left, Math.Max(0, rect.Height - radius.TopLeft - radius.BottomLeft));
            var right = new Rect(rect.Right - thickness.Right, rect.Top + radius.TopRight, thickness.Right, Math.Max(0, rect.Height - radius.TopRight - radius.BottomRight));
            if (top.Height > 0 && top.Width > 0)
                spriteBatch.Draw(SolidColorBrush.Pixel, top, At((top.Left + top.Right) / 2, rect.Top));
            if (bottom.Height > 0 && bottom.Width > 0)
                spriteBatch.Draw(SolidColorBrush.Pixel, bottom, At((bottom.Left + bottom.Right) / 2, rect.Bottom));
            if (left.Width > 0 && left.Height > 0)
                spriteBatch.Draw(SolidColorBrush.Pixel, left, At(rect.Left, (left.Top + left.Bottom) / 2));
            if (right.Width > 0 && right.Height > 0)
                spriteBatch.Draw(SolidColorBrush.Pixel, right, At(rect.Right, (right.Top + right.Bottom) / 2));

            DrawCornerRing(spriteBatch, rect.Left, rect.Top, radius.TopLeft, Math.Max(thickness.Top, thickness.Left), At(rect.Left, rect.Top), SpriteEffects.None);
            DrawCornerRing(spriteBatch, rect.Right - radius.TopRight, rect.Top, radius.TopRight, Math.Max(thickness.Top, thickness.Right), At(rect.Right, rect.Top), SpriteEffects.FlipHorizontally);
            DrawCornerRing(spriteBatch, rect.Right - radius.BottomRight, rect.Bottom - radius.BottomRight, radius.BottomRight, Math.Max(thickness.Bottom, thickness.Right), At(rect.Right, rect.Bottom), SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically);
            DrawCornerRing(spriteBatch, rect.Left, rect.Bottom - radius.BottomLeft, radius.BottomLeft, Math.Max(thickness.Bottom, thickness.Left), At(rect.Left, rect.Bottom), SpriteEffects.FlipVertically);
        }

        private static void DrawCornerRing(SpriteBatch spriteBatch, float x, float y, float radius, float thickness, Color color, SpriteEffects effects)
        {
            var size = (int)Math.Ceiling(radius);
            if (size <= 0 || thickness <= 0)
                return;

            var mask = GetCornerRingMask(spriteBatch.GraphicsDevice, size, Math.Max(1, (int)Math.Round(thickness)));
            spriteBatch.Draw(mask, new Rect(x, y, radius, radius), null, color, 0, Vector2.Zero, effects, 0);
        }

        private static Texture2D GetCornerRingMask(GraphicsDevice device, int radius, int thickness)
        {
            var cache = GetCache(device);
            if (cache.CornerRingMasks.TryGetValue((radius, thickness), out var texture))
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

            texture = new Texture2D(device, radius, radius);
            texture.SetData(data);
            cache.CornerRingMasks[(radius, thickness)] = texture;
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

            if (color.A < 255)
            {
                var width = Math.Max(1, (int)Math.Ceiling(rect.Width));
                var height = Math.Max(1, (int)Math.Ceiling(rect.Height));
                var mask = GetRoundedRectMask(spriteBatch.GraphicsDevice, width, height, radius);
                spriteBatch.Draw(mask, rect, color);
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

        private static Texture2D GetRoundedRectMask(GraphicsDevice device, int width, int height, CornerRadius radius)
        {
            var key = new RoundedRectMaskKey(
                width,
                height,
                (int)Math.Ceiling(radius.TopLeft),
                (int)Math.Ceiling(radius.TopRight),
                (int)Math.Ceiling(radius.BottomRight),
                (int)Math.Ceiling(radius.BottomLeft));
            var cache = GetCache(device);
            if (cache.RoundedRectMasks.TryGetValue(key, out var texture))
                return texture;

            if (cache.RoundedRectMasks.Count >= MaxRoundedRectMasks)
            {
                foreach (var retired in cache.RetiredMasks)
                    retired.Dispose();
                cache.RetiredMasks.Clear();
                cache.RetiredMasks.AddRange(cache.RoundedRectMasks.Values);
                cache.RoundedRectMasks.Clear();
            }

            var data = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var coverage = RoundedRectCoverage(x + 0.5f, y + 0.5f, width, height, radius);
                    var value = (byte)(coverage * 255);
                    data[y * width + x] = new Color(value, value, value, value);
                }
            }

            texture = new Texture2D(device, width, height);
            texture.SetData(data);
            cache.RoundedRectMasks[key] = texture;
            return texture;
        }

        private static float RoundedRectCoverage(float x, float y, int width, int height, CornerRadius radius)
        {
            float CoverageForCorner(float centerX, float centerY, float cornerRadius)
            {
                if (cornerRadius <= 0)
                    return 1f;

                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                return MathHelper.Clamp(cornerRadius + 0.5f - distance, 0, 1);
            }

            if (x < radius.TopLeft && y < radius.TopLeft)
                return CoverageForCorner(radius.TopLeft, radius.TopLeft, radius.TopLeft);
            if (x >= width - radius.TopRight && y < radius.TopRight)
                return CoverageForCorner(width - radius.TopRight, radius.TopRight, radius.TopRight);
            if (x >= width - radius.BottomRight && y >= height - radius.BottomRight)
                return CoverageForCorner(width - radius.BottomRight, height - radius.BottomRight, radius.BottomRight);
            if (x < radius.BottomLeft && y >= height - radius.BottomLeft)
                return CoverageForCorner(radius.BottomLeft, height - radius.BottomLeft, radius.BottomLeft);

            return 1f;
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

            var mask = GetCornerMask(spriteBatch.GraphicsDevice, size);
            spriteBatch.Draw(mask, new Rect(x, y, radius, radius), null, color, 0, Vector2.Zero, effects, 0);
        }

        private static Texture2D GetCornerMask(GraphicsDevice device, int radius)
        {
            var cache = GetCache(device);
            if (cache.CornerMasks.TryGetValue(radius, out var texture))
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

            texture = new Texture2D(device, radius, radius);
            texture.SetData(data);
            cache.CornerMasks[radius] = texture;
            return texture;
        }

        private readonly record struct RoundedRectMaskKey(
            int Width,
            int Height,
            int TopLeft,
            int TopRight,
            int BottomRight,
            int BottomLeft);
    }
}
