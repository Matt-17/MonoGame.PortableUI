using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class Border : ContentControl
    {
        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            spriteBatch.Draw(ScreenEngine.Pixel, rect, BackgroundColor);
        }
    }
}