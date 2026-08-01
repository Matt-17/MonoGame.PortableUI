using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public class GradientBrush : Brush
    {
        private Texture2D? _texture;
        private Color _startColor;
        private Color _endColor;
        private GradientDirection _direction;

        public GradientBrush(Color startColor, Color endColor)
            : this(startColor, endColor, GradientDirection.Vertical)
        {
        }

        public GradientBrush(Color startColor, Color endColor, GradientDirection direction)
        {
            _startColor = startColor;
            _endColor = endColor;
            _direction = direction;
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

        public GradientDirection Direction
        {
            get { return _direction; }
            set
            {
                _direction = value;
                _texture = null;
            }
        }

        private void RecreateTexture(SpriteBatch spriteBatch)
        {
            if (_texture != null)
                return;

            switch (Direction)
            {
                case GradientDirection.Horizontal:
                    _texture = new Texture2D(spriteBatch.GraphicsDevice, 2, 1);
                    _texture.SetData(new[] { StartColor, EndColor });
                    break;
                case GradientDirection.DiagonalDown:
                    _texture = new Texture2D(spriteBatch.GraphicsDevice, 2, 2);
                    _texture.SetData(new[] { StartColor, MidColor, MidColor, EndColor });
                    break;
                case GradientDirection.DiagonalUp:
                    _texture = new Texture2D(spriteBatch.GraphicsDevice, 2, 2);
                    _texture.SetData(new[] { MidColor, EndColor, StartColor, MidColor });
                    break;
                default:
                    _texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 2);
                    _texture.SetData(new[] { StartColor, EndColor });
                    break;
            }
        }

        private Color MidColor => Color.Lerp(StartColor, EndColor, 0.5f);

        public override void Draw(SpriteBatch spriteBatch, Rect rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            RecreateTexture(spriteBatch);
            if (_texture != null)
                spriteBatch.Draw(_texture, rect, Color.White);
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            RecreateTexture(spriteBatch);
            if (_texture != null)
                spriteBatch.Draw(_texture, rect, ApplyOpacity(Color.White, opacity));
        }
    }
}
