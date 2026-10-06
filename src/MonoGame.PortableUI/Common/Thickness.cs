using System;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Common
{
    public struct Thickness : IEquatable<Thickness>
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

        public bool Equals(Thickness other)
            => Left.Equals(other.Left) && Top.Equals(other.Top) && Right.Equals(other.Right) && Bottom.Equals(other.Bottom);

        public override bool Equals(object? obj) => obj is Thickness other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);

        public static bool operator ==(Thickness a, Thickness b) => a.Equals(b);

        public static bool operator !=(Thickness a, Thickness b) => !a.Equals(b);

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