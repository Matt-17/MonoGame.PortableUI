using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public abstract class Brush
    {
        public abstract void Draw(SpriteBatch spriteBatch, Rect rect);
        public static implicit operator Brush(Color color)
        {
            return new SolidColorBrush(color);
        }
    }
}