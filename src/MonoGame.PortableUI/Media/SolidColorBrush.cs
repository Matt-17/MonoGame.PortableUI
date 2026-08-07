using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public class SolidColorBrush : Brush
    {
        private static Texture2D? _pixel;

        /// <summary>Shared 1×1 white texture. Recreated when the cached instance (or its device)
        /// has been disposed, e.g. after a graphics device reset.</summary>
        public static Texture2D Pixel
        {
            get
            {
                var pixel = _pixel;
                if (pixel != null && !pixel.IsDisposed && !pixel.GraphicsDevice.IsDisposed)
                    return pixel;

                var device = ScreenEngine.Instance?.Game.GraphicsDevice
                    ?? throw new InvalidOperationException(
                        "SolidColorBrush.Pixel needs an initialized ScreenEngine (call ScreenEngine.Initialize first).");
                pixel = new Texture2D(device, 1, 1);
                pixel.SetData(new[] { Color.White });
                _pixel = pixel;
                return pixel;
            }
        }



        public Color Color { get; set; }

        public SolidColorBrush()
        {
            Color = Color.White;
        }

        public SolidColorBrush(Color color)
        {
            Color = color;
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect)
        {
            spriteBatch.Draw(Pixel, rect, Premultiply(Color));
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            spriteBatch.Draw(Pixel, rect, ApplyOpacity(Color, opacity));
        }

        public override void Draw(SpriteBatch spriteBatch, in BrushContext context)
        {
            RoundedRectRenderer.DrawSolid(spriteBatch, context.Rect, context.Radius, ApplyOpacity(Color, context.Opacity));
        }
    }
}
