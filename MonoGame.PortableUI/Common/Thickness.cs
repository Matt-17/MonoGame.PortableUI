using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Common
{
    public struct Thickness
    {
        public float Left { get; set; }
        public float Top { get; set; }
        public float Right { get; set; }
        public float Bottom { get; set; }

        internal float Horizontal => Left + Right;

        internal float Vertical => Top + Bottom;

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

        public override string ToString()
        {
            return $"{Left}, {Top}, {Right}, {Bottom}";
        }

        public static implicit operator Thickness(float i)
        {
            return new Thickness(i);
        }

        public static Size operator +(Size rect, Thickness t)
        {
            return new Size(rect.Width + t.Right + t.Left, rect.Height + t.Bottom + t.Top).Clamp();
        }

        public static Size operator -(Size rect, Thickness t)
        {
            return new Size(rect.Width - t.Right - t.Left, rect.Height - t.Bottom - t.Top).Clamp();
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