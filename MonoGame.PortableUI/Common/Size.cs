using System;

namespace MonoGame.PortableUI.Common
{
    /// <summary>
    /// Defines a structure to define sized elements without a position. 
    /// </summary>
    public struct Size
    {
        public const float Auto = float.NaN;
        public const float Infinity = float.PositiveInfinity;

        /// <summary>
        /// A size with width and height equal to zero.
        /// </summary>
        public static readonly Size Empty = new Size();

        /// <summary>
        /// Creates a new size with a given width and height.
        /// </summary>
        /// <param name="width">The width of the element.</param>
        /// <param name="height">The height of the element.</param>
        public Size(float width, float height)
        {
            Width = Math.Max(0, width);
            Height = Math.Max(0, height);
        }

        /// <summary>
        /// The width of the element.
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        /// The height of the element.
        /// </summary>
        public float Height { get; set; }

        public override string ToString()
        {
            return $"Width: {Width}, Height: {Height}";
        }

        public static Rect operator +(Size s, PointF p)
        {
            return new Rect(p.X, p.Y, s.Width, s.Height);
        }

        public static Rect operator +(PointF p, Size s)
        {
            return new Rect(p.X, p.Y, s.Width, s.Height);
        }

        public static Size operator +(Size s1, Size s2)
        {
            return new Size(s1.Width + s2.Width, s1.Height + s2.Height);
        }

        public static Size operator -(Size s1, Size s2)
        {
            return new Size(s1.Width - s2.Width, s1.Height - s2.Height).Clamp();
        }

        private Size Clamp()
        {
            return new Size(Math.Max(0, Width), Math.Max(0, Height));
        }

        public static Size operator *(Size s, float scale)
        {
            return new Size(s.Width * scale, s.Height * scale);
        }

        public static Size operator *(float scale, Size s)
        {
            return s * scale;
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