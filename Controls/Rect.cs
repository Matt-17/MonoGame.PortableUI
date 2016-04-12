using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Controls
{
    public class Rect : Control
    {

        public Rect(Game game) : base (game)
        {
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rectangle rect)
        {
            spriteBatch.Draw(BackgroundTexture, rect, BackgroundColor);
        }
    }
}