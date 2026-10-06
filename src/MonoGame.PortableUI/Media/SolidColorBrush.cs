using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public class SolidColorBrush : Brush
    {
        /// <summary>Shared 1×1 white texture on the primary engine's device. Rendering code should
        /// use <see cref="Primitives.Pixel(SpriteBatch)"/> instead, which follows the device of the
        /// batch at hand (surface engines, no initialized primary engine) and survives device resets.</summary>
        public static Texture2D Pixel
        {
            get
            {
                var device = ScreenEngine.Instance?.Game.GraphicsDevice
                    ?? throw new InvalidOperationException(
                        "SolidColorBrush.Pixel needs an initialized ScreenEngine; use Primitives.Pixel(spriteBatch) in rendering code.");
                return Primitives.Pixel(device);
            }
        }

        /// <summary>Fill colour, straight alpha: <c>Color.White.WithAlpha(0.07f)</c> is white at 7 %.
        /// MonoGame's <c>Color.White * 0.07f</c> is premultiplied and would draw at 0.5 % (see <see cref="ColorAlpha"/>).</summary>
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
            spriteBatch.Draw(Primitives.Pixel(spriteBatch), rect, Premultiply(Color));
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            spriteBatch.Draw(Primitives.Pixel(spriteBatch), rect, ApplyOpacity(Color, opacity));
        }

        public override void Draw(SpriteBatch spriteBatch, in BrushContext context)
        {
            RoundedRectRenderer.DrawSolid(spriteBatch, context.Rect, context.Radius, ApplyOpacity(Color, context.Opacity));
        }
    }
}
