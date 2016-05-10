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
            return new Size(rect.Width + t.Right + t.Left, rect.Height + t.Bottom + t.Top);
        }

        public static Size operator -(Size rect, Thickness t)
        {
            return new Size(rect.Width - t.Right - t.Left, rect.Height - t.Bottom - t.Top);
        }

        public static Rect operator -(Rect rect, Thickness t)
        {
            return new Rect(rect.Left + t.Left, rect.Top + t.Top, rect.Width - t.Right - t.Left, rect.Height - t.Bottom - t.Top);
        }

        public static Rect operator +(Rect rect, Thickness t)
        {
            return new Rect(rect.Left - t.Left, rect.Top - t.Top, rect.Width + t.Right + t.Left, rect.Height + t.Bottom + t.Top);
        }
    }
}