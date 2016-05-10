using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class TextBlock : TextControl
    {
        public TextAlignment TextAlignment { get; set; }

        public float TextHeight
        {
            get { return Font.MeasureString(Text).Y; }
        }

        public float TextWidth
        {
            get { return Font.MeasureString(Text).X; }
        }

        public TextBlock()
        {
            Font = FontManager.DefaultFont;
            TextColor = Color.Black;
            TextAlignment = TextAlignment.Center;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            var position = new Vector2(rect.Left, rect.Top);
            position.Y += ((VerticalAlignment == VerticalAlignment.Stretch ? rect.Height : Height) - TextHeight) / 2;

            var f = (HorizontalAlignment == HorizontalAlignment.Stretch ? rect.Width : Width) - TextWidth;
            if (TextAlignment == TextAlignment.Center)
                position.X += f / 2;
            else if (TextAlignment == TextAlignment.Right)
                position.X += f;

            if (SnapToPixel)
                position = position.ToInts();

            spriteBatch.DrawString(Font, Text, position, TextColor);
        }
    }
}