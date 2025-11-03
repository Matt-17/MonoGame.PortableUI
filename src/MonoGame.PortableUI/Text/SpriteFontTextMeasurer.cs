using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Text
{
    public sealed class SpriteFontTextMeasurer : ITextMeasurer
    {
        private readonly SpriteFont _font;

        public SpriteFontTextMeasurer(SpriteFont font)
        {
            _font = font;
        }

        public Vector2 MeasureString(string text)
        {
            return _font.MeasureString(text ?? string.Empty);
        }
    }
}
