
using System;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Common
{
    public static class SizeEx
    {
        public static bool IsFixed(this float f)
        {
            return !float.IsNaN(f) && !float.IsInfinity(f);
        }
        public static bool IsBounded(this float f) { return !float.IsInfinity(f); }
    }

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

    public struct PointF
    {
        public PointF(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; set; }
        public float Y { get; set; }

        public static implicit operator PointF(Point s1)
        {
            return new PointF(s1.X, s1.Y);
        }
        public static implicit operator Point(PointF s1)
        {
            return new Point((int)s1.X, (int)s1.Y);
        }

        public static implicit operator Vector2(PointF p)
        {
            return new Vector2(p.X, p.Y);
        }

        public PointF ToInts()
        {
            return new PointF((int)X, (int)Y);
        }
    }
}