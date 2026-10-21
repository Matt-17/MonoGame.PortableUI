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

        private sealed class DeviceCache
        {
            public readonly Dictionary<int, Texture2D> CornerMasks = new Dictionary<int, Texture2D>();
            public readonly Dictionary<(int Radius, int Thickness), Texture2D> CornerRingMasks = new Dictionary<(int, int), Texture2D>();
            public readonly Dictionary<(int TopLeft, int TopRight, int BottomRight, int BottomLeft), Texture2D> NineSliceMasks = new Dictionary<(int, int, int, int), Texture2D>();
            public readonly Dictionary<SubpixelMaskKey, Texture2D> SubpixelMasks = new Dictionary<SubpixelMaskKey, Texture2D>();
        }

        /// <summary>
        ///     Set by the screen around a control's draw: true while the control moves (an animation,
        ///     transition, squash or breathing) and its theme does not ask for <see cref="PortableTheme.PixelMotion"/>.
        ///     Fills and borders are then drawn at their exact fractional position and size with
        ///     anti-aliased edges, so motion glides instead of stepping a whole pixel at a time.
        /// </summary>
        [ThreadStatic]
        internal static bool Subpixel;

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
                foreach (var texture in cache.NineSliceMasks.Values)
                    texture.Dispose();
                foreach (var texture in cache.SubpixelMasks.Values)
                    texture.Dispose();
                cache.NineSliceMasks.Clear();
                cache.SubpixelMasks.Clear();
                cache.CornerMasks.Clear();
                cache.CornerRingMasks.Clear();
            }
        }

        /// <summary>Draws a border that follows the corner radius: 4 edge strips + quarter-ring corners. Expects a premultiplied color.</summary>
        public static void DrawBorder(SpriteBatch spriteBatch, Rect rect, CornerRadius radius, Thickness thickness, Color color)
        {
            if (rect.Width <= 0 || rect.Height <= 0 || color.A == 0)
                return;

            if (Subpixel && !IsWholePixels(rect, radius) && TryDrawSubpixelBorder(spriteBatch, rect, radius, thickness, color))
                return;

            SnapToPixels(ref rect, ref radius);
            radius = Clamp(radius, rect);
            if (radius.IsEmpty)
                return;

            // Edge strips between the rounded corners.
            var top = new Rect(rect.Left + radius.TopLeft, rect.Top, Math.Max(0, rect.Width - radius.TopLeft - radius.TopRight), thickness.Top);
            var bottom = new Rect(rect.Left + radius.BottomLeft, rect.Bottom - thickness.Bottom, Math.Max(0, rect.Width - radius.BottomLeft - radius.BottomRight), thickness.Bottom);
            var left = new Rect(rect.Left, rect.Top + radius.TopLeft, thickness.Left, Math.Max(0, rect.Height - radius.TopLeft - radius.BottomLeft));
            var right = new Rect(rect.Right - thickness.Right, rect.Top + radius.TopRight, thickness.Right, Math.Max(0, rect.Height - radius.TopRight - radius.BottomRight));
            if (top.Height > 0 && top.Width > 0)
                spriteBatch.Draw(Primitives.Pixel(spriteBatch), top, color);
            if (bottom.Height > 0 && bottom.Width > 0)
                spriteBatch.Draw(Primitives.Pixel(spriteBatch), bottom, color);
            if (left.Width > 0 && left.Height > 0)
                spriteBatch.Draw(Primitives.Pixel(spriteBatch), left, color);
            if (right.Width > 0 && right.Height > 0)
                spriteBatch.Draw(Primitives.Pixel(spriteBatch), right, color);

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

            SnapToPixels(ref rect, ref radius);
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
                spriteBatch.Draw(Primitives.Pixel(spriteBatch), top, At((top.Left + top.Right) / 2, rect.Top));
            if (bottom.Height > 0 && bottom.Width > 0)
                spriteBatch.Draw(Primitives.Pixel(spriteBatch), bottom, At((bottom.Left + bottom.Right) / 2, rect.Bottom));
            if (left.Width > 0 && left.Height > 0)
                spriteBatch.Draw(Primitives.Pixel(spriteBatch), left, At(rect.Left, (left.Top + left.Bottom) / 2));
            if (right.Width > 0 && right.Height > 0)
                spriteBatch.Draw(Primitives.Pixel(spriteBatch), right, At(rect.Right, (right.Top + right.Bottom) / 2));

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

            if (Subpixel && !IsWholePixels(rect, radius))
            {
                DrawSubpixel(spriteBatch, rect, Clamp(radius, rect), 0, color);
                return;
            }

            SnapToPixels(ref rect, ref radius);
            radius = Clamp(radius, rect);
            if (radius.IsEmpty)
            {
                spriteBatch.Draw(Primitives.Pixel(spriteBatch), rect, color);
                return;
            }

            if (color.A < 255)
            {
                // Translucent fills must not overlap (overlap doubles alpha into visible seams).
                // A 9-slice of one small mask keyed by the radii covers any size without
                // overlap, so fading/animated/shadowed rects no longer build a W×H mask per size.
                DrawNineSlice(spriteBatch, rect, radius, color);
                return;
            }

            // Overlap the straight fills 1px into the corner regions. On a pill/circle the middle
            // bands would otherwise collapse to zero width/height and only the four corner masks
            // draw, leaving a hairline "+" seam where they meet; the overlap bridges it.
            Span<Rect> fills = stackalloc Rect[3];
            FillRects(rect, radius, 1f, fills);
            foreach (var fillRect in fills)
            {
                if (fillRect.Width > 0 && fillRect.Height > 0)
                    spriteBatch.Draw(Primitives.Pixel(spriteBatch), fillRect, color);
            }

            DrawCorner(spriteBatch, rect.Left, rect.Top, radius.TopLeft, color, SpriteEffects.None);
            DrawCorner(spriteBatch, rect.Right - radius.TopRight, rect.Top, radius.TopRight, color, SpriteEffects.FlipHorizontally);
            DrawCorner(spriteBatch, rect.Right - radius.BottomRight, rect.Bottom - radius.BottomRight, radius.BottomRight, color, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically);
            DrawCorner(spriteBatch, rect.Left, rect.Bottom - radius.BottomLeft, radius.BottomLeft, color, SpriteEffects.FlipVertically);

            // A corner smaller than its neighbours (a teardrop's square corner) leaves part of its band
            // square to the straight fills, which stop at the larger radius: fill around its mask.
            Span<Rect> rest = stackalloc Rect[8];
            CornerRemainders(rect, radius, rest);
            foreach (var fillRect in rest)
            {
                if (fillRect.Width > 0 && fillRect.Height > 0)
                    spriteBatch.Draw(Primitives.Pixel(spriteBatch), fillRect, color);
            }
        }

        /// <summary>
        ///     Per corner, the part of its band square (side bands × top/bottom bands, sized by the larger
        ///     radius on each side) that neither the straight fills nor the corner's own mask cover.
        /// </summary>
        internal static void CornerRemainders(Rect rect, CornerRadius radius, Span<Rect> rects)
        {
            var left = Math.Max(radius.TopLeft, radius.BottomLeft);
            var right = Math.Max(radius.TopRight, radius.BottomRight);
            var top = Math.Max(radius.TopLeft, radius.TopRight);
            var bottom = Math.Max(radius.BottomLeft, radius.BottomRight);
            // Each corner: a strip beside its mask and the rest of the square below/above it.
            var tl = radius.TopLeft;
            rects[0] = new Rect(rect.Left + tl, rect.Top, left - tl, tl);
            rects[1] = new Rect(rect.Left, rect.Top + tl, left, top - tl);
            var tr = radius.TopRight;
            rects[2] = new Rect(rect.Right - right, rect.Top, right - tr, tr);
            rects[3] = new Rect(rect.Right - right, rect.Top + tr, right, top - tr);
            var br = radius.BottomRight;
            rects[4] = new Rect(rect.Right - right, rect.Bottom - br, right - br, br);
            rects[5] = new Rect(rect.Right - right, rect.Bottom - bottom, right, bottom - br);
            var bl = radius.BottomLeft;
            rects[6] = new Rect(rect.Left + bl, rect.Bottom - bl, left - bl, bl);
            rects[7] = new Rect(rect.Left, rect.Bottom - bottom, left, bottom - bl);
        }

        private static void DrawNineSlice(SpriteBatch spriteBatch, Rect rect, CornerRadius radius, Color color)
        {
            var tl = (int)Math.Ceiling(radius.TopLeft);
            var tr = (int)Math.Ceiling(radius.TopRight);
            var br = (int)Math.Ceiling(radius.BottomRight);
            var bl = (int)Math.Ceiling(radius.BottomLeft);
            var mask = GetNineSliceMask(spriteBatch.GraphicsDevice, tl, tr, br, bl);

            // Source layout: [left corner band][1 pad][1 stretch][1 pad][right corner band], same
            // for rows. Stretching a single texel whose neighbours are also fully covered keeps
            // linear filtering from bleeding corner coverage into the stretched edges.
            var srcLeft = Math.Max(tl, bl);
            var srcRight = Math.Max(tr, br);
            var srcTop = Math.Max(tl, tr);
            var srcBottom = Math.Max(bl, br);
            Span<int> srcX = stackalloc int[] { 0, srcLeft + 1, srcLeft + 3 };
            Span<int> srcW = stackalloc int[] { srcLeft, 1, srcRight };
            Span<int> srcY = stackalloc int[] { 0, srcTop + 1, srcTop + 3 };
            Span<int> srcH = stackalloc int[] { srcTop, 1, srcBottom };

            // Integer destination like every other SpriteBatch draw here (Rect -> Rectangle), so
            // edges land on the same pixels as the opaque path instead of half-covering a row.
            Rectangle target = rect;
            var left = srcLeft;
            var right = srcRight;
            if (left + right > target.Width)
            {
                left = target.Width * left / Math.Max(1, left + right);
                right = target.Width - left;
            }
            var top = srcTop;
            var bottom = srcBottom;
            if (top + bottom > target.Height)
            {
                top = target.Height * top / Math.Max(1, top + bottom);
                bottom = target.Height - top;
            }

            Span<int> dstX = stackalloc int[] { target.Left, target.Left + left, target.Right - right };
            Span<int> dstW = stackalloc int[] { left, target.Width - left - right, right };
            Span<int> dstY = stackalloc int[] { target.Top, target.Top + top, target.Bottom - bottom };
            Span<int> dstH = stackalloc int[] { top, target.Height - top - bottom, bottom };

            for (var row = 0; row < 3; row++)
            {
                if (dstH[row] <= 0 || srcH[row] <= 0)
                    continue;
                for (var column = 0; column < 3; column++)
                {
                    if (dstW[column] <= 0 || srcW[column] <= 0)
                        continue;
                    spriteBatch.Draw(
                        mask,
                        new Rectangle(dstX[column], dstY[row], dstW[column], dstH[row]),
                        new Rectangle(srcX[column], srcY[row], srcW[column], srcH[row]),
                        color);
                }
            }
        }

        private static Texture2D GetNineSliceMask(GraphicsDevice device, int tl, int tr, int br, int bl)
        {
            var cache = GetCache(device);
            if (cache.NineSliceMasks.TryGetValue((tl, tr, br, bl), out var texture))
                return texture;

            var data = NineSliceMaskPixels(tl, tr, br, bl, out var width, out var height);
            texture = new Texture2D(device, width, height);
            texture.SetData(data);
            cache.NineSliceMasks[(tl, tr, br, bl)] = texture;
            return texture;
        }

        /// <summary>The 9-slice mask's pixels: the four corners with a three-pixel cross between them.</summary>
        internal static uint[] NineSliceMaskPixels(int tl, int tr, int br, int bl, out int width, out int height)
        {
            width = Math.Max(tl, bl) + 3 + Math.Max(tr, br);
            height = Math.Max(tl, tr) + 3 + Math.Max(bl, br);
            // Fully covered, then each corner square written from its quarter circle (mirrored into place).
            // Shadow layers ask for many large radii, so this runs often on the first frames of a page.
            var data = new uint[width * height];
            Array.Fill(data, 0xFFFFFFFFu);
            var quarter = tl == tr && tr == br && br == bl ? QuarterCircle(tl) : null;
            WriteCorner(data, width, quarter ?? QuarterCircle(tl), tl, 0, 0, false, false);
            WriteCorner(data, width, quarter ?? QuarterCircle(tr), tr, width - tr, 0, true, false);
            WriteCorner(data, width, quarter ?? QuarterCircle(br), br, width - br, height - br, true, true);
            WriteCorner(data, width, quarter ?? QuarterCircle(bl), bl, 0, height - bl, false, true);
            return data;
        }

        // Coverage of a top-left corner square of radius r (row-major, r×r): 1 inside the arc, 0 outside, the
        // one-pixel band between from the distance to the arc's centre - squared distances skip the root
        // everywhere but on that band.
        private static byte[] QuarterCircle(int r)
        {
            var coverage = new byte[r * r];
            var inner = Math.Max(0, r - 0.5f);
            var inner2 = inner * inner;
            var outer2 = (r + 0.5f) * (r + 0.5f);
            for (var y = 0; y < r; y++)
            {
                var dy = r - (y + 0.5f);
                for (var x = 0; x < r; x++)
                {
                    var dx = r - (x + 0.5f);
                    var d2 = dx * dx + dy * dy;
                    coverage[y * r + x] = d2 <= inner2 ? (byte)255 : d2 >= outer2 ? (byte)0 : (byte)(MathHelper.Clamp(r + 0.5f - MathF.Sqrt(d2), 0, 1) * 255);
                }
            }
            return coverage;
        }

        private static void WriteCorner(uint[] data, int width, byte[] coverage, int r, int left, int top, bool flipX, bool flipY)
        {
            for (var y = 0; y < r; y++)
            {
                var row = (top + y) * width + left;
                var sourceRow = (flipY ? r - 1 - y : y) * r;
                for (var x = 0; x < r; x++)
                {
                    uint value = coverage[sourceRow + (flipX ? r - 1 - x : x)];
                    data[row + x] = value | value << 8 | value << 16 | value << 24;
                }
            }
        }

        internal static IEnumerable<Rect> GetFillRects(Rect rect, CornerRadius radius, float overlap = 0f)
        {
            var rects = new Rect[3];
            FillRects(rect, radius, overlap, rects);
            return rects;
        }

        /// <summary>The three straight fills between the corners, written into <paramref name="rects"/> (length 3).</summary>
        internal static void FillRects(Rect rect, CornerRadius radius, float overlap, Span<Rect> rects)
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

            rects[0] = new Rect(rect.Left + l, rect.Top, Math.Max(0, rect.Width - l - r), rect.Height);
            rects[1] = new Rect(rect.Left, rect.Top + t, left, Math.Max(0, rect.Height - t - b));
            rects[2] = new Rect(rect.Right - right, rect.Top + t, right, Math.Max(0, rect.Height - t - b));
        }

        // The pieces become integer Rectangles one by one (Rect -> Rectangle truncates). On a
        // fractional rect (any scale animation, HiDPI) corners and strips truncated separately end up
        // to 2 px apart. Snapping the whole rect first, the way a plain fill of it would land
        // (Rectangle conversion), and using whole-pixel radii keeps every piece on the same pixels.
        private static void SnapToPixels(ref Rect rect, ref CornerRadius radius)
        {
            Rectangle pixels = rect;
            rect = new Rect(pixels.X, pixels.Y, Math.Max(0, pixels.Width), Math.Max(0, pixels.Height));
            radius = new CornerRadius(
                MathF.Round(radius.TopLeft),
                MathF.Round(radius.TopRight),
                MathF.Round(radius.BottomRight),
                MathF.Round(radius.BottomLeft));
        }

        private static bool IsWhole(float value) => MathF.Abs(value - MathF.Round(value)) < 0.002f;

        // At whole pixels the snapped path draws the same pixels; it stays the reference.
        private static bool IsWholePixels(Rect rect, CornerRadius radius)
            => IsWhole(rect.Left) && IsWhole(rect.Top) && IsWhole(rect.Width) && IsWhole(rect.Height)
               && IsWhole(radius.TopLeft) && IsWhole(radius.TopRight) && IsWhole(radius.BottomRight) && IsWhole(radius.BottomLeft);

        private static bool TryDrawSubpixelBorder(SpriteBatch spriteBatch, Rect rect, CornerRadius radius, Thickness thickness, Color color)
        {
            // One ring mask per (radii, width): uneven borders keep the snapped path.
            if (!(thickness.Left == thickness.Top && thickness.Top == thickness.Right && thickness.Right == thickness.Bottom) || thickness.Left <= 0)
                return false;
            DrawSubpixel(spriteBatch, rect, Clamp(radius, rect), thickness.Left, color);
            return true;
        }

        internal readonly record struct SubpixelMaskKey(int Left, int Right, int Top, int Bottom, int TopLeft, int TopRight, int BottomRight, int BottomLeft, int Border);

        /// <summary>
        ///     A rounded rect (or a ring <paramref name="border"/> wide) at a fractional position and size:
        ///     a 9-slice of a mask with a transparent texel around the shape, drawn as float quads with
        ///     linear filtering, so each edge covers a pixel by how far it reaches into it.
        /// </summary>
        private static void DrawSubpixel(SpriteBatch spriteBatch, Rect rect, CornerRadius radius, float border, Color color)
        {
            // Band sizes in pixels: the corners, at least the border width.
            var left = Math.Max(Math.Max(radius.TopLeft, radius.BottomLeft), border);
            var right = Math.Max(Math.Max(radius.TopRight, radius.BottomRight), border);
            var top = Math.Max(Math.Max(radius.TopLeft, radius.TopRight), border);
            var bottom = Math.Max(Math.Max(radius.BottomLeft, radius.BottomRight), border);
            if (left + right > rect.Width)
            {
                var k = rect.Width / Math.Max(0.001f, left + right);
                left *= k;
                right *= k;
            }
            if (top + bottom > rect.Height)
            {
                var k = rect.Height / Math.Max(0.001f, top + bottom);
                top *= k;
                bottom *= k;
            }

            var bandLeft = Math.Max(0, (int)MathF.Ceiling(left - 0.002f));
            var bandRight = Math.Max(0, (int)MathF.Ceiling(right - 0.002f));
            var bandTop = Math.Max(0, (int)MathF.Ceiling(top - 0.002f));
            var bandBottom = Math.Max(0, (int)MathF.Ceiling(bottom - 0.002f));
            // The mask is drawn shrunk from whole texels to the fractional band: its radii are scaled
            // up by the same factor, so a corner that fills its band (a circle) lands exactly on its
            // radius instead of jumping by a pixel whenever the radius crosses a half.
            var scaleLeft = left > 0 ? bandLeft / left : 1;
            var scaleRight = right > 0 ? bandRight / right : 1;
            var key = new SubpixelMaskKey(
                bandLeft, bandRight, bandTop, bandBottom,
                Math.Min(bandLeft, (int)MathF.Round(radius.TopLeft * scaleLeft)),
                Math.Min(bandRight, (int)MathF.Round(radius.TopRight * scaleRight)),
                Math.Min(bandRight, (int)MathF.Round(radius.BottomRight * scaleRight)),
                Math.Min(bandLeft, (int)MathF.Round(radius.BottomLeft * scaleLeft)),
                border > 0 ? Math.Max(1, (int)MathF.Round(border * Math.Min(scaleLeft, scaleRight))) : 0);
            var mask = GetSubpixelMask(spriteBatch.GraphicsDevice, key);

            // Mask columns: [pad][left band][fill][stretch][fill][right band][pad], rows alike. The
            // outer pad is transparent: filtering against it anti-aliases the edges.
            Span<int> srcX = stackalloc int[] { 0, key.Left + 2, key.Left + 4 };
            Span<int> srcW = stackalloc int[] { key.Left + 1, 1, key.Right + 1 };
            Span<int> srcY = stackalloc int[] { 0, key.Top + 2, key.Top + 4 };
            Span<int> srcH = stackalloc int[] { key.Top + 1, 1, key.Bottom + 1 };
            Span<float> dstX = stackalloc float[] { rect.Left - 1, rect.Left + left, rect.Right - right };
            Span<float> dstW = stackalloc float[] { left + 1, rect.Width - left - right, right + 1 };
            Span<float> dstY = stackalloc float[] { rect.Top - 1, rect.Top + top, rect.Bottom - bottom };
            Span<float> dstH = stackalloc float[] { top + 1, rect.Height - top - bottom, bottom + 1 };

            for (var row = 0; row < 3; row++)
            {
                if (dstH[row] <= 0.001f)
                    continue;
                for (var column = 0; column < 3; column++)
                {
                    if (dstW[column] <= 0.001f)
                        continue;
                    // A ring has nothing in the middle.
                    if (row == 1 && column == 1 && key.Border > 0)
                        continue;
                    spriteBatch.Draw(
                        mask,
                        new Vector2(dstX[column], dstY[row]),
                        new Rectangle(srcX[column], srcY[row], srcW[column], srcH[row]),
                        color,
                        0,
                        Vector2.Zero,
                        new Vector2(dstW[column] / srcW[column], dstH[row] / srcH[row]),
                        SpriteEffects.None,
                        0);
                }
            }
        }

        private static Texture2D GetSubpixelMask(GraphicsDevice device, SubpixelMaskKey key)
        {
            var cache = GetCache(device);
            if (cache.SubpixelMasks.TryGetValue(key, out var existing))
                return existing;

            // The shape spans (1,1)..(width-1,height-1); its middle is 3 texels long per axis.
            var width = key.Left + key.Right + 5;
            var height = key.Top + key.Bottom + 5;
            var data = new Color[width * height];
            var outer = new CornerRadius(key.TopLeft, key.TopRight, key.BottomRight, key.BottomLeft);
            var b = key.Border;
            var inner = new CornerRadius(
                Math.Max(0, key.TopLeft - b), Math.Max(0, key.TopRight - b),
                Math.Max(0, key.BottomRight - b), Math.Max(0, key.BottomLeft - b));
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var px = x + 0.5f;
                    var py = y + 0.5f;
                    var coverage = Coverage(px - 1, py - 1, width - 2, height - 2, outer);
                    if (b > 0)
                        coverage -= Coverage(px - 1 - b, py - 1 - b, width - 2 - 2 * b, height - 2 - 2 * b, inner);
                    var value = (byte)(MathHelper.Clamp(coverage, 0, 1) * 255 + 0.5f);
                    data[y * width + x] = new Color(value, value, value, value);
                }
            }

            var texture = new Texture2D(device, width, height);
            texture.SetData(data);
            cache.SubpixelMasks[key] = texture;
            return texture;
        }

        // Coverage of the texel centred on (x, y) by a rounded rect spanning [0,w]x[0,h], from the
        // signed distance.
        private static float Coverage(float x, float y, float w, float h, CornerRadius radius)
        {
            if (w <= 0 || h <= 0)
                return 0;
            var r = x < w / 2
                ? (y < h / 2 ? radius.TopLeft : radius.BottomLeft)
                : (y < h / 2 ? radius.TopRight : radius.BottomRight);
            r = Math.Min(r, Math.Min(w, h) / 2);
            var qx = MathF.Abs(x - w / 2) - (w / 2 - r);
            var qy = MathF.Abs(y - h / 2) - (h / 2 - r);
            var outside = new Vector2(MathF.Max(qx, 0), MathF.Max(qy, 0)).Length();
            var distance = outside + MathF.Min(MathF.Max(qx, qy), 0) - r;
            // A half-pixel ramp: linear filtering at a fractional offset adds up to one more pixel of
            // blur, so together the edge stays about as sharp as a 1 px anti-aliased edge.
            return MathHelper.Clamp(0.5f - distance * 2, 0, 1);
        }

        internal static CornerRadius Clamp(CornerRadius radius, Rect rect)
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

    }
}
