using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public class SolidColorBrush : Brush
    {
        private static Texture2D _pixel;

        public static Texture2D Pixel
        {
            get
            {
                if (_pixel == null)
                {
                    _pixel = new Texture2D(ScreenEngine.Manager.Game.GraphicsDevice, 1, 1);
                    _pixel.SetData(new[] { Color.White });
                }
                return _pixel;

            }
        }



        public Color Color { get; set; }

        public SolidColorBrush()
        {
            Color = Color.White;
        }

        public SolidColorBrush(Color color)
        {
            Color = color;
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect)
        {
            spriteBatch.Draw(Pixel, rect, Color);
        }
    }
}