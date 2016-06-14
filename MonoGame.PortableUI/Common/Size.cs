
using System;

namespace MonoGame.PortableUI.Common
{
    public struct Size
    {
        public const float Auto = float.NaN;
        public const float Infinity = float.PositiveInfinity;

        public static Size Empty = new Size();

        public Size(float width, float height)
        {
            Width = width;
            Height = height;
        }

        public float Width { get; set; }
        public float Height { get; set; }

        public static Size operator +(Size s1, Size s2)
        {
            return new Size(s1.Width + s2.Width, s1.Height + s2.Height);
        }
        public static Rect operator +(Size s, PointF p)
        {
            return new Rect(p.X, p.Y, s.Width, s.Height);
        }

        public override string ToString()
        {
            return $"({Width}; {Height})";
        }

        public static Rect operator +(PointF p, Size s)
        {
            return new Rect(p.X, p.Y, s.Width, s.Height);
        }

        public static Size operator -(Size s1, Size s2)
        {
            return new Size(s1.Width - s2.Width, s1.Height - s2.Height).Clamp();
        }

        public Size Clamp()
        {
            return new Size(Math.Max(0, Width), Math.Max(0, Height));
        }

        public static Size operator *(Size s, float scale)
        {
            return new Size(s.Width * scale, s.Height * scale);
        }
        public static Size operator /(Size s, float scale)
        {
            return new Size(s.Width / scale, s.Height / scale);
        }

        public static implicit operator Rect(Size s1)
        {
            return new Rect(s1.Width, s1.Height);
        }

        public static explicit operator Size(Rect rect)
        {
            return new Size(rect.Width, rect.Height);
        }
    }
}