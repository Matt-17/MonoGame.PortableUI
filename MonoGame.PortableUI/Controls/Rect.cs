using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Controls
{
    public class Rect : Control
    {
        protected internal override void OnDraw(SpriteBatch spriteBatch, Rectangle rect)
        {
            spriteBatch.Draw(ScreenEngine.Pixel, rect, BackgroundColor);
        }
    }
}