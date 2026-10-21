using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public class LinearGradientBrush : Brush
    {
        // The gradient is evaluated in rect-normalized UV space, so for a plain (non-rounded) fill a
        // small fixed-size texture stretched to the target rect looks identical to an element-sized
        // one (same trick RadialGradientBrush uses) but avoids minting -- and permanently caching,
        // since BrushTextureCache only evicts on device reset -- a new texture per distinct pixel size
        // whenever a gradient-filled control resizes or animates.
        private const int FastPathTextureSize = 64;

        public LinearGradientBrush(Color startColor, Color endColor)
            : this(new GradientStop(0, startColor), new GradientStop(1, endColor))
        {
        }

        public LinearGradientBrush(params GradientStop[] stops)
        {
            Stops = stops?.ToList() ?? new List<GradientStop>();
        }

        public List<GradientStop> Stops { get; }

        private float _angleDegrees = 90;
        public float AngleDegrees
        {
            get => _angleDegrees;
            set => SetProperty(ref _angleDegrees, value);
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect)
        {
            MarkDrawn();
            Draw(spriteBatch, rect, 1);
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            MarkDrawn();
            Draw(spriteBatch, new BrushContext(rect, 0, opacity, spriteBatch.GraphicsDevice));
        }

        public override void Draw(SpriteBatch spriteBatch, in BrushContext context)
        {
            MarkDrawn();
            if (context.Rect.Width <= 0 || context.Rect.Height <= 0)
                return;

            Texture2D texture;
            if (context.Radius.IsEmpty)
            {
                var fastKey = CreateFastPathCacheKey();
                if (!BrushTextureCache.TryGet(spriteBatch.GraphicsDevice, fastKey, out texture))
                    texture = CreateCachedTexture(spriteBatch.GraphicsDevice, fastKey, FastPathTextureSize, FastPathTextureSize, new CornerRadius(0));
            }
            else
            {
                // Corner-mask pixels are chosen from the actual pixel dimensions, so the corners are
                // computed for the element's size; the bands between them stretch (StretchedCorners).
                // In motion at the rest size, stretched: a scale animation builds no texture per frame.
                var width = context.RestPixelWidth;
                var height = context.RestPixelHeight;
                var radius = context.RestRadius;
                var key = CreateTextureCacheKey(width, height, radius);
                var layout = StretchedCorners.For(width, height, radius);
                if (!BrushTextureCache.TryGet(spriteBatch.GraphicsDevice, key, out texture))
                    texture = CreateCachedTexture(spriteBatch.GraphicsDevice, key, layout);
                layout.Draw(spriteBatch, texture, context.Rect, ApplyOpacity(Color.White, context.Opacity));
                return;
            }
            spriteBatch.Draw(texture, context.Rect, ApplyOpacity(Color.White, context.Opacity));
        }

        // Miss path only: the factory closure captures the size, so keeping it out of Draw means
        // a cache hit allocates nothing.
        private Texture2D CreateCachedTexture(GraphicsDevice device, BrushTextureCacheKey key, int width, int height, CornerRadius radius)
        {
            return CreateCachedTexture(device, key, StretchedCorners.For(width, height, radius));
        }

        private Texture2D CreateCachedTexture(GraphicsDevice device, BrushTextureCacheKey key, StretchedCorners layout)
        {
            return BrushTextureCache.GetOrCreate(device, key, graphicsDevice => CreateTexture(graphicsDevice, layout));
        }

        private BrushTextureCacheKey CreateFastPathCacheKey()
        {
            return new BrushTextureCacheKey(
                "linear-gradient-v2-fixed",
                BitConverter.SingleToInt32Bits(AngleDegrees),
                GetStopsHash());
        }

        internal BrushTextureCacheKey CreateTextureCacheKey(int width, int height)
        {
            return CreateTextureCacheKey(width, height, new CornerRadius(0));
        }

        internal BrushTextureCacheKey CreateTextureCacheKey(int width, int height, CornerRadius radius)
        {
            return new BrushTextureCacheKey(
                "linear-gradient-v3",
                width,
                height,
                BitConverter.SingleToInt32Bits(AngleDegrees),
                HashCode.Combine(GetStopsHash(), radius));
        }

        private Texture2D CreateTexture(GraphicsDevice graphicsDevice, StretchedCorners layout)
        {
            var width = layout.Width;
            var height = layout.Height;
            var stops = GetOrderedStops();
            var data = new Color[layout.TextureWidth * layout.TextureHeight];
            var radians = MathHelper.ToRadians(AngleDegrees);
            var direction = new Vector2((float)Math.Cos(radians), (float)Math.Sin(radians));
            if (direction.LengthSquared() <= float.Epsilon)
                direction = Vector2.UnitY;

            var min = GetProjection(new Vector2(0, 0), direction);
            var max = min;
            var corners = new[]
            {
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            foreach (var corner in corners)
            {
                var projection = GetProjection(corner, direction);
                min = Math.Min(min, projection);
                max = Math.Max(max, projection);
            }

            var range = Math.Max(0.0001f, max - min);
            // The colours along the gradient once, finer than any pixel step; each pixel then only looks one up.
            var steps = Math.Clamp(2 * Math.Max(width, height), 2, 4096);
            var table = new Color[steps];
            for (var i = 0; i < steps; i++)
                table[i] = Premultiply(GradientStops.Evaluate(stops, i / (float)(steps - 1)));
            // t is linear in the pixel position: t = tx·x + ty·y + t0.
            var tx = width == 1 ? 0 : GetProjection(new Vector2(1f / (width - 1), 0), direction) / range;
            var ty = height == 1 ? 0 : GetProjection(new Vector2(0, 1f / (height - 1)), direction) / range;
            var t0 = -min / range;
            for (var y = 0; y < layout.TextureHeight; y++)
            {
                var rowT = t0 + ty * layout.ElementY(y);
                for (var x = 0; x < layout.TextureWidth; x++)
                {
                    var t = MathHelper.Clamp(rowT + tx * layout.ElementX(x), 0, 1);
                    data[y * layout.TextureWidth + x] = table[(int)(t * (steps - 1) + 0.5f)];
                }
            }

            layout.ApplyCornerMask(data);
            var texture = new Texture2D(graphicsDevice, layout.TextureWidth, layout.TextureHeight);
            texture.SetData(data);
            return texture;
        }

        private IReadOnlyList<GradientStop> GetOrderedStops()
        {
            return GradientStops.GetOrdered(Stops);
        }

        private int GetStopsHash()
        {
            return GradientStops.GetHash(Stops);
        }

        /// <summary>
        ///     The gradient's straight-alpha colour at <paramref name="offset"/> (0..1) without allocating
        ///     (stops need not be sorted). Two stops at one offset make a hard step.
        /// </summary>
        public Color ColorAt(float offset)
        {
            if (Stops.Count == 0)
                return Color.Transparent;
            if (Stops.Count == 1)
                return Stops[0].Color;
            offset = MathHelper.Clamp(offset, 0, 1);
            GradientStop below = default, above = default;
            bool hasBelow = false, hasAbove = false;
            foreach (var stop in Stops)
            {
                // Strictly above for a step: at the step offset itself the upper colour wins.
                if (stop.Offset <= offset && (!hasBelow || stop.Offset >= below.Offset))
                {
                    below = stop;
                    hasBelow = true;
                }
                if (stop.Offset > offset && (!hasAbove || stop.Offset < above.Offset))
                {
                    above = stop;
                    hasAbove = true;
                }
            }
            if (!hasBelow)
                return above.Color;
            if (!hasAbove)
                return below.Color;
            var span = Math.Max(0.0001f, above.Offset - below.Offset);
            return Color.Lerp(below.Color, above.Color, (offset - below.Offset) / span);
        }

        private static float GetProjection(Vector2 point, Vector2 direction)
        {
            return Vector2.Dot(point, direction);
        }
    }
}
