using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Controls
{
    public class Thickness
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }

        public Thickness() : this(0)
        {
        }

        public Thickness(double thickness) : this(thickness, thickness)
        {
        }

        public Thickness(double horizontal, double vertical) : this(horizontal, vertical, horizontal, vertical)
        {
        }

        public Thickness(double left, double top, double right, double bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public static Rectangle operator -(Rectangle rect, Thickness t)
        {
            if (t == null)
                return rect;
            return new Rectangle((int)(rect.X + t.Left), (int)(rect.Y + t.Top), (int)(rect.Width - t.Right - t.Left), (int)(rect.Height - t.Bottom - t.Top));
        }
    }
}