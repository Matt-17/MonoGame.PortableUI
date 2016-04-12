using Microsoft.Xna.Framework;
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



    }

    public interface IScreen
    {
    }
}