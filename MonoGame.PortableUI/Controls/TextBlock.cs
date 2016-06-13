using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class TextBlock : TextControl
    {
        private TextAlignment _textAlignment;

        public TextAlignment TextAlignment
        {
            get { return _textAlignment; }
            set
            {
                _textAlignment = value;
                InvalidateLayout(false);
            }
        }

        public override Size MeasureLayout()
        {
            var size = base.MeasureLayout();

            var vector2 = Font.MeasureString(Text);
            size.Height += vector2.Y;
            size.Width += vector2.X;

            return size;
        }

        public TextBlock()
        {
            Font = FontManager.DefaultFont;
            TextColor = Color.Black;
            TextAlignment = TextAlignment.Left;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            rect -= Margin;
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
            spriteBatch.DrawString(Font, Text, offset, TextColor);
        }
    }
}