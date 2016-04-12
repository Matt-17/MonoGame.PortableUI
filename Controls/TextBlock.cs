using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public enum TextAlignment
    {
        Left,
        Center,
        Right
    }

    public class TextBlock : UIControl
    {
        public TextAlignment TextAlignment { get; set; }

        private SpriteFont _font;
        public Color TextColor { get; set; }

        public string Text { get; set; }

        public float TextHeight
        {
            get { return _font.MeasureString(Text).Y; }
        }

        public float TextWidth
        {
            get { return _font.MeasureString(Text).X; }
        }

        public TextBlock(Game game):base(game)
        {
            _font = game.Content.Load<SpriteFont>(@"Fonts/Segoe-bold-14");
            TextColor = Color.Black;
            TextAlignment = TextAlignment.Center;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rectangle rect)
        {
            var position = new Vector2(rect.Left + Left, rect.Top + Top);
            position.Y += ((VerticalAlignment == VerticalAlignment.Stretch ? rect.Height : Height) - TextHeight) / 2;

            var f = (HorizontalAlignment == HorizontalAlignment.Stretch ? rect.Width : Width) - TextWidth;
            if (TextAlignment == TextAlignment.Center)
                position.X += f/2;
            else if (TextAlignment == TextAlignment.Right)
                position.X += f;

            if (SnapToPixel)
                position = position.ToInts();

            spriteBatch.DrawString(_font, Text, position, TextColor);
        }
    }
}