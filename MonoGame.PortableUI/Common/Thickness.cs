using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Common
{
    public struct Thickness
    {
        public float Left { get; set; }
        public float Top { get; set; }
        public float Right { get; set; }
        public float Bottom { get; set; }

        public Thickness(float thickness) : this(thickness, thickness)
        {
        }

        public Thickness(float horizontal, float vertical) : this(horizontal, vertical, horizontal, vertical)
        {
        }

        public Thickness(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public static Size operator +(Size rect, Thickness t)
        {
            return new Size(rect.Width - t.Right - t.Left, rect.Height - t.Bottom - t.Top);
        }


        public static Rectangle operator -(Rectangle rect, Thickness t)
        {
            return new Rectangle((int)(rect.X + t.Left), (int)(rect.Y + t.Top), (int)(rect.Width - t.Right - t.Left), (int)(rect.Height - t.Bottom - t.Top));
        }
    }
}