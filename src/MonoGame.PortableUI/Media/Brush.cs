using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public abstract class Brush
    {
        public abstract void Draw(SpriteBatch spriteBatch, Rect rect);

        public virtual void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            Draw(spriteBatch, rect);
        }

        public static Color ApplyOpacity(Color color, float opacity)
        {
            opacity = MathHelper.Clamp(opacity, 0, 1);
            var alpha = (byte)MathHelper.Clamp(color.A * opacity, 0, 255);
            return new Color(color.R, color.G, color.B, alpha);
        }

        public static implicit operator Brush(Color color)
        {
            return new SolidColorBrush(color);
        }
    }
}
