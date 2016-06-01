using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Controls
{
    public abstract class TextControl : Control
    {
        protected SpriteFont Font;
        private string _text;
        private int _textSize;
        private Color _textColor;

        public Color TextColor
        {
            get { return _textColor; }
            set
            {
                _textColor = value;
                InvalidateLayout(false);
            }

        }

        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                InvalidateLayout(true);
            }
        }

        public int TextSize
        {
            get { return _textSize; }
            set
            {
                _textSize = value;
                InvalidateLayout(true);
            }
        }
    }
}