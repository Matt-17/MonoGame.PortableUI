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

        /// <summary>
        ///     Fully rounded: every corner gets half the control's shorter side (pills, round buttons), whatever
        ///     size it ends up with. Use it in a <c>StateStyle</c> or on a control.
        /// </summary>
        public static CornerRadius Full { get; } = new CornerRadius(float.PositiveInfinity);

        /// <summary>True when any corner is <see cref="Full"/> (resolved against the control size when drawn).</summary>
        public bool HasFullCorner => float.IsPositiveInfinity(TopLeft) || float.IsPositiveInfinity(TopRight)
            || float.IsPositiveInfinity(BottomRight) || float.IsPositiveInfinity(BottomLeft);

        /// <summary>The radius with <see cref="Full"/> corners (and any larger than fits) limited to half the
        /// shorter side of a <paramref name="width"/> x <paramref name="height"/> box.</summary>
        public CornerRadius ClampTo(float width, float height)
        {
            var max = Math.Max(0, Math.Min(width, height) / 2);
            return new CornerRadius(Math.Min(TopLeft, max), Math.Min(TopRight, max), Math.Min(BottomRight, max), Math.Min(BottomLeft, max));
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
