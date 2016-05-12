using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public class GradientBrush : Brush
    {
        private Texture2D _texture;
        private Rect _rect;
        private Color _startColor;
        private Color _endColor;

        public GradientBrush(Color startColor, Color endColor)
        {
            _startColor = startColor;
            _endColor = endColor;
        }

        public Color StartColor
        {
            get { return _startColor; }
            set
            {
                _startColor = value;
                _texture = null;
            }
        }

        public Color EndColor
        {
            get { return _endColor; }
            set
            {
                _endColor = value;
                _texture = null;
            }
        }

        private void RecreateTexture(SpriteBatch spriteBatch, Size rect)
        {
            if (_rect == rect && _texture != null)
                return;

            _rect = rect;
            var width = (int)rect.Width;
            var height = (int)rect.Height;
            _texture = new Texture2D(spriteBatch.GraphicsDevice, width, height);
            Color[] colArr = new Color[width * height];
            for (int y = 0; y < height; ++y)
            {
                float yRel = y / (float)height;
                float u = MathHelper.Clamp(yRel, 0.0f, 1.0f);

                for (int x = 0; x < width; x++)
                {
                    colArr[x + y * width] = Color.Lerp(StartColor, EndColor, u);
                }
            }
            _texture.SetData(colArr);
        }


        public override void Draw(SpriteBatch spriteBatch, Rect rect)
        {
            RecreateTexture(spriteBatch, (Size)rect);
            spriteBatch.Draw(_texture, rect, Color.White);
        }
    }
}