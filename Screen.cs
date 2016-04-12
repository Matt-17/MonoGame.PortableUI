using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI
{
    public class Screen : Control, IScreen
    {
        public Screen(Game game) : base(game)
        {
            BackgroundColor = Color.Transparent;
        }

        internal ScreenEngine ScreenEngine { get; set; }

        public Control Content { get; set; }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rectangle rect)
        {
            base.OnDraw(spriteBatch, rect);

            if (BackgroundColor != Color.Transparent)
                spriteBatch.GraphicsDevice.Clear(BackgroundColor);
        }
    }

    public interface IScreen
    {
    }
}