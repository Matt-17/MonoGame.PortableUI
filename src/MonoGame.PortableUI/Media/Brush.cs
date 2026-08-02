using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public abstract class Brush
    {
        public virtual bool RequiresBackdrop => false;

        public abstract void Draw(SpriteBatch spriteBatch, Rect rect);

        public virtual void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            Draw(spriteBatch, rect);
        }

        public virtual void Draw(SpriteBatch spriteBatch, in BrushContext context)
        {
            Draw(spriteBatch, context.Rect, context.Opacity);
        }

        /// <summary>
        ///     Applies opacity and converts to premultiplied alpha. SpriteBatch's default AlphaBlend
        ///     state expects premultiplied colors; passing straight-alpha colors over-brightens every
        ///     translucent draw (they blend additively instead of compositing).
        /// </summary>
        public static Color ApplyOpacity(Color color, float opacity)
        {
            opacity = MathHelper.Clamp(opacity, 0, 1);
            var alpha = (int)MathHelper.Clamp(color.A * opacity, 0, 255);
            return Color.FromNonPremultiplied(color.R, color.G, color.B, alpha);
        }

        /// <summary>Converts a straight-alpha color to premultiplied alpha for SpriteBatch drawing.</summary>
        public static Color Premultiply(Color color)
        {
            return color.A == 255 ? color : Color.FromNonPremultiplied(color.R, color.G, color.B, color.A);
        }

        public static implicit operator Brush(Color color)
        {
            return new SolidColorBrush(color);
        }
    }
}
