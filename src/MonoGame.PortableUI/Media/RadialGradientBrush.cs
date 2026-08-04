using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public class RadialGradientBrush : Brush
    {
        private const int TextureSize = 64;

        public RadialGradientBrush(Color centerColor, Color edgeColor)
            : this(new GradientStop(0, centerColor), new GradientStop(1, edgeColor))
        {
        }

        public RadialGradientBrush(params GradientStop[] stops)
        {
            Stops = stops?.ToList() ?? new List<GradientStop>();
        }

        public List<GradientStop> Stops { get; }

        public PointF Center { get; set; } = new PointF(0.5f, 0.5f);

        public float RadiusX { get; set; } = 0.5f;

        public float RadiusY { get; set; } = 0.5f;

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

            var texture = BrushTextureCache.GetOrCreate(
                spriteBatch.GraphicsDevice,
                CreateTextureCacheKey(),
                CreateTexture);
            spriteBatch.Draw(texture, context.Rect, ApplyOpacity(Color.White, context.Opacity));
        }

        internal BrushTextureCacheKey CreateTextureCacheKey()
        {
            return new BrushTextureCacheKey(
                "radial-gradient-v1",
                BitConverter.SingleToInt32Bits(Center.X),
                BitConverter.SingleToInt32Bits(Center.Y),
                HashCode.Combine(BitConverter.SingleToInt32Bits(RadiusX), BitConverter.SingleToInt32Bits(RadiusY)),
                GetStopsHash());
        }

        private Texture2D CreateTexture(GraphicsDevice graphicsDevice)
        {
            var stops = GetOrderedStops();
            var data = new Color[TextureSize * TextureSize];
            var radiusX = Math.Max(0.0001f, RadiusX);
            var radiusY = Math.Max(0.0001f, RadiusY);
            for (var y = 0; y < TextureSize; y++)
            {
                var v = y / (float)(TextureSize - 1);
                for (var x = 0; x < TextureSize; x++)
                {
                    var u = x / (float)(TextureSize - 1);
                    var dx = (u - Center.X) / radiusX;
                    var dy = (v - Center.Y) / radiusY;
                    var t = (float)Math.Sqrt(dx * dx + dy * dy);
                    data[y * TextureSize + x] = Premultiply(GradientStops.Evaluate(stops, t));
                }
            }

            var texture = new Texture2D(graphicsDevice, TextureSize, TextureSize);
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
    }
}
