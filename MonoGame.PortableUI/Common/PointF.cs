using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Common
{
    public struct PointF
    {
        public bool Equals(PointF other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is PointF && Equals((PointF) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode()*397) ^ Y.GetHashCode();
            }
        }

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

        public static bool operator ==(PointF r1, PointF r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator !=(PointF r1, PointF r2)
        {
            return !(r1 == r2);
        }

    }
}