using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Common
{
    public struct Rect
    {
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

        public float Top { get; set; }
        public float Left { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

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
            return string.Format("({0}, {1}; {2}x{3})", Top, Left, Width, Height);
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
            throw new System.NotImplementedException();
        }
    }
}