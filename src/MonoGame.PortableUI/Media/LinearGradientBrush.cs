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

        public float AngleDegrees { get; set; } = 90;

        public override void Draw(SpriteBatch spriteBatch, Rect rect)
        {
            Draw(spriteBatch, rect, 1);
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            Draw(spriteBatch, new BrushContext(rect, 0, opacity, spriteBatch.GraphicsDevice));
        }

        public override void Draw(SpriteBatch spriteBatch, in BrushContext context)
        {
            if (context.Rect.Width <= 0 || context.Rect.Height <= 0)
                return;

            Texture2D texture;
            if (context.Radius.IsEmpty)
            {
                texture = BrushTextureCache.GetOrCreate(
                    spriteBatch.GraphicsDevice,
                    CreateFastPathCacheKey(),
                    graphicsDevice => CreateTexture(graphicsDevice, FastPathTextureSize, FastPathTextureSize, new CornerRadius(0)));
            }
            else
            {
                // Corner-mask pixels are chosen from the actual pixel dimensions, so a rounded fill
                // still needs an element-sized texture to look correct at every aspect ratio.
                var width = Math.Max(1, (int)Math.Ceiling(context.Rect.Width));
                var height = Math.Max(1, (int)Math.Ceiling(context.Rect.Height));
                var radius = context.Radius;
                texture = BrushTextureCache.GetOrCreate(
                    spriteBatch.GraphicsDevice,
                    CreateTextureCacheKey(width, height, radius),
                    graphicsDevice => CreateTexture(graphicsDevice, width, height, radius));
            }
            spriteBatch.Draw(texture, context.Rect, ApplyOpacity(Color.White, context.Opacity));
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
                "linear-gradient-v2",
                width,
                height,
                BitConverter.SingleToInt32Bits(AngleDegrees),
                HashCode.Combine(GetStopsHash(), radius));
        }

        private Texture2D CreateTexture(GraphicsDevice graphicsDevice, int width, int height, CornerRadius radius)
        {
            var stops = GetOrderedStops();
            var data = new Color[width * height];
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
            for (var y = 0; y < height; y++)
            {
                var v = height == 1 ? 0 : y / (float)(height - 1);
                for (var x = 0; x < width; x++)
                {
                    var u = width == 1 ? 0 : x / (float)(width - 1);
                    var t = (GetProjection(new Vector2(u, v), direction) - min) / range;
                    data[y * width + x] = Premultiply(GradientStops.Evaluate(stops, t));
                }
            }

            RoundedRectRenderer.ApplyCornerMask(data, width, height, radius);
            var texture = new Texture2D(graphicsDevice, width, height);
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

        private static float GetProjection(Vector2 point, Vector2 direction)
        {
            return Vector2.Dot(point, direction);
        }
    }
}
