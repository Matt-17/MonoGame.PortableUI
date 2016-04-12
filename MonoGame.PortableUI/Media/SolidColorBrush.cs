using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Media
{
    public class SolidColorBrush : Brush
    {
        public Color Color { get; set; }

        public SolidColorBrush()
        {
            Color = Color.Transparent;
        }

        public SolidColorBrush(Color color)
        {
            Color = color;
        }
    }
}