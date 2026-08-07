using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class Border : ContentControl
    {
        public Border()
        {
            IsFocusable = false;
        }

        public Brush? BorderColor
        {
            get { return BorderBrush; }
            set { BorderBrush = value; }
        }

        public Thickness BorderWidth
        {
            get { return BorderThickness; }
            set { BorderThickness = value; }
        }
    }
}
