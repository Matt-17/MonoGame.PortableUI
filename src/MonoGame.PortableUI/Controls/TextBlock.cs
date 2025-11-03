using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Controls
{
    public class TextBlock : Control
    {
        private TextAlignment _textAlignment;
        protected SpriteFont Font;
        private ITextMeasurer _textMeasurer;
        private string _text = "";
        private int _textSize;
        private Color _textColor;

        public TextAlignment TextAlignment
        {
            get { return _textAlignment; }
            set
            {
                _textAlignment = value;
                InvalidateLayout(false);
            }
        }

        public Color TextColor
        {
            get { return _textColor; }
            set
            {
                _textColor = value;
                InvalidateLayout(false);
            }
        }
        public Vector2 MeasuredText { get; private set; }

        public string Text
        {
            get { return _text; }
            set
            {
                _text = value ?? "";
                MeasuredText = MeasureText(_text);
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

        public override Size MeasureLayout()
        {
            var size = base.MeasureLayout();

            var vector2 = MeasureText(Text);
            size.Width = Width.IsFixed() ? Width : vector2.X;
            if (vector2.Y > size.Height)
                size.Height = vector2.Y;
            //size.Height = Height;

            return size;
        }

        public TextBlock()
        {
            Font = FontManager.DefaultFont;
            _textMeasurer = Font != null ? new SpriteFontTextMeasurer(Font) : ApproximateTextMeasurer.Default;
            TextColor = Color.Black;
            TextAlignment = TextAlignment.Left;
        }

        public ITextMeasurer TextMeasurer
        {
            get { return _textMeasurer; }
            set
            {
                _textMeasurer = value ?? ApproximateTextMeasurer.Default;
                MeasuredText = MeasureText(Text);
                InvalidateLayout(true);
            }
        }

        protected Vector2 MeasureText(string text)
        {
            if (Font != null)
                return Font.MeasureString(text ?? "");
            return TextMeasurer.MeasureString(text ?? "");
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {                    
            base.OnDraw(spriteBatch, rect);
            var offset = rect.Offset;
            offset.Y += (rect.Height - MeasuredText.Y) / 2;

            switch (TextAlignment)
            {
                case TextAlignment.Left:
                    break;
                case TextAlignment.Center:
                    offset.X += (rect.Width - MeasuredText.X) / 2;
                    break;
                case TextAlignment.Right:
                    offset.X += rect.Width - MeasuredText.X;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (SnapToPixel)
                offset = offset.ToInts();
            if (Font != null)
                spriteBatch.DrawString(Font, Text, offset, TextColor);
        }
    }
}
