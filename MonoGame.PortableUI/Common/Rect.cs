using System;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Common
{
    public struct Rect
    {
        public bool Equals(Rect other)
        {
            return Top.Equals(other.Top) && Left.Equals(other.Left) && Width.Equals(other.Width) && Height.Equals(other.Height);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Rect && Equals((Rect)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Top.GetHashCode();
                hashCode = (hashCode * 397) ^ Left.GetHashCode();
                hashCode = (hashCode * 397) ^ Width.GetHashCode();
                hashCode = (hashCode * 397) ^ Height.GetHashCode();
                return hashCode;
            }
        }

        public static Rect Empty = new Rect();

        public Rect(float left, float top, float width, float height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        public Rect(float width, float height) : this()
        {
            Width = width;
            Height = height;
        }

        public Rect(PointF offset, Size size)  : this(offset.X, offset.Y, size.Width, size.Height)
        {                               
        }

        public float Top { get; set; }
        public float Left { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public float Right
        {
            get { return Left + Width; }
            set { Width = value - Left; }
        }
        public float Bottom
        {
            get { return Top + Height; }
            set { Height = value - Top; }
        }

        public PointF Offset
        {
            get { return new PointF(Left, Top); }
            set
            {
                Left = value.X;
                Top = value.Y;
            }
        }

        public static bool operator ==(Rect r1, Rect r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator !=(Rect r1, Rect r2)
        {
            return !(r1 == r2);
        }


        public static Rect operator +(Rect r1, Rect r2)
        {
            return new Rect(r1.Left + r2.Left, r1.Top + r2.Top, r1.Width + r2.Width, r1.Height + r2.Height);
        }

        public static Rect operator -(Rect r1, Rect r2)
        {
            return new Rect(r1.Left - r2.Left, r1.Top - r2.Top, r1.Width - r2.Width, r1.Height - r2.Height);
        }

        public Rect AtOrigin()
        {
            return new Rect(Width, Height);
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}; {2}x{3})", Left, Top, Width, Height);
        }

        public static Rect operator ^(Rect r1, Rect r2)
        {
            return r1.Intersects(r2);
        }

        public static implicit operator Rectangle(Rect rect)
        {
            return new Rectangle((int)rect.Left, (int)rect.Top, (int)rect.Width, (int)rect.Height);
        }

        private Rect Intersects(Rect other)
        {
            Rect result = new Rect
            {
                Left = Math.Max(Left, other.Left),
                Top = Math.Max(Top, other.Top),
                Right = Math.Min(Right, other.Right),
                Bottom = Math.Min(Bottom, other.Bottom)
            };

            return result;
        }

        public bool Contains(PointF position)
        {
            return position.X > Left && position.Y > Top && position.X < Left + Width && position.Y < Top + Height;
        }
    }
}