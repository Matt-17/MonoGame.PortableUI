using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Controls
{
    public class TextControl : Control
    {
        protected SpriteFont Font;

        public Color TextColor { get; set; }

        public string Text { get; set; }

        public int TextSize { get; set; }
    }
}