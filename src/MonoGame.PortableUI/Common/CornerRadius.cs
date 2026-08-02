using System;

namespace MonoGame.PortableUI.Common
{
    public readonly struct CornerRadius : IEquatable<CornerRadius>
    {
        public CornerRadius(float uniformRadius)
            : this(uniformRadius, uniformRadius, uniformRadius, uniformRadius)
        {
        }

        public CornerRadius(float topLeft, float topRight, float bottomRight, float bottomLeft)
        {
            TopLeft = Math.Max(0, topLeft);
            TopRight = Math.Max(0, topRight);
            BottomRight = Math.Max(0, bottomRight);
            BottomLeft = Math.Max(0, bottomLeft);
        }

        public float TopLeft { get; }

        public float TopRight { get; }

        public float BottomRight { get; }

        public float BottomLeft { get; }

        public bool IsEmpty => TopLeft <= 0 && TopRight <= 0 && BottomRight <= 0 && BottomLeft <= 0;

        public bool IsUniform => TopLeft.Equals(TopRight) && TopLeft.Equals(BottomRight) && TopLeft.Equals(BottomLeft);

        public static implicit operator CornerRadius(float radius)
        {
            return new CornerRadius(radius);
        }

        public bool Equals(CornerRadius other)
        {
            return TopLeft.Equals(other.TopLeft)
                && TopRight.Equals(other.TopRight)
                && BottomRight.Equals(other.BottomRight)
                && BottomLeft.Equals(other.BottomLeft);
        }

        public override bool Equals(object? obj)
        {
            return obj is CornerRadius other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TopLeft, TopRight, BottomRight, BottomLeft);
        }

        public override string ToString()
        {
            return $"{TopLeft}, {TopRight}, {BottomRight}, {BottomLeft}";
        }
    }
}
