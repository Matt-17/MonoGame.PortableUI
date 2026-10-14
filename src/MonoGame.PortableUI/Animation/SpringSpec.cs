using System;

namespace MonoGame.PortableUI.Animation
{
    /// <summary>
    ///     A damped spring for <see cref="Animation.Spring(SpringSpec)"/>: motion follows physics instead of a
    ///     duration and an easing curve, overshoots when under-damped, settles on its own and keeps its
    ///     velocity when a new animation of the same property takes over mid-flight.
    /// </summary>
    public readonly struct SpringSpec : IEquatable<SpringSpec>
    {
        /// <summary>Settles quickly without overshoot.</summary>
        public static readonly SpringSpec Gentle = FromResponse(0.55, 1.0);

        /// <summary>Fast, with a barely visible overshoot - good for presses and small moves.</summary>
        public static readonly SpringSpec Snappy = FromResponse(0.3, 0.85);

        /// <summary>A clear bounce past the target.</summary>
        public static readonly SpringSpec Bouncy = FromResponse(0.5, 0.6);

        /// <summary>Loose, several swings before it rests.</summary>
        public static readonly SpringSpec Wobbly = FromResponse(0.7, 0.35);

        public SpringSpec(double stiffness, double damping, double mass = 1)
        {
            if (!(stiffness > 0))
                throw new ArgumentOutOfRangeException(nameof(stiffness), "Stiffness must be positive.");
            if (!(damping >= 0))
                throw new ArgumentOutOfRangeException(nameof(damping), "Damping must not be negative.");
            if (!(mass > 0))
                throw new ArgumentOutOfRangeException(nameof(mass), "Mass must be positive.");
            Stiffness = stiffness;
            Damping = damping;
            Mass = mass;
        }

        public double Stiffness { get; }
        public double Damping { get; }
        public double Mass { get; }

        /// <summary>1 = critically damped (no overshoot), below 1 bounces, above 1 creeps in.</summary>
        public double DampingRatio => Damping / (2 * Math.Sqrt(Stiffness * Mass));

        /// <summary>
        ///     A spring described like SwiftUI's: <paramref name="responseSeconds"/> is the period of one
        ///     undamped swing (how quick it feels), <paramref name="dampingRatio"/> how much it bounces.
        /// </summary>
        public static SpringSpec FromResponse(double responseSeconds, double dampingRatio)
        {
            if (!(responseSeconds > 0))
                throw new ArgumentOutOfRangeException(nameof(responseSeconds), "Response must be positive.");
            var omega = 2 * Math.PI / responseSeconds;
            return new SpringSpec(omega * omega, 2 * Math.Max(0, dampingRatio) * omega);
        }

        /// <summary>
        ///     Displacement from the target and velocity after <paramref name="seconds"/>, starting at
        ///     displacement <paramref name="displacement0"/> with <paramref name="velocity0"/> (exact solution,
        ///     independent of the frame rate).
        /// </summary>
        internal void Evaluate(double displacement0, double velocity0, double seconds, out double displacement, out double velocity)
        {
            var omega0 = Math.Sqrt(Stiffness / Mass);
            var zeta = DampingRatio;
            var t = seconds;
            if (zeta < 0.9999)
            {
                var decay = zeta * omega0;
                var omegaD = omega0 * Math.Sqrt(1 - zeta * zeta);
                var a = displacement0;
                var b = (velocity0 + decay * a) / omegaD;
                var envelope = Math.Exp(-decay * t);
                var cos = Math.Cos(omegaD * t);
                var sin = Math.Sin(omegaD * t);
                displacement = envelope * (a * cos + b * sin);
                velocity = envelope * (velocity0 * cos - (decay * b + a * omegaD) * sin);
            }
            else if (zeta <= 1.0001)
            {
                var c = velocity0 + omega0 * displacement0;
                var envelope = Math.Exp(-omega0 * t);
                displacement = (displacement0 + c * t) * envelope;
                velocity = (c - omega0 * (displacement0 + c * t)) * envelope;
            }
            else
            {
                var root = Math.Sqrt(zeta * zeta - 1);
                var r1 = -omega0 * (zeta - root);
                var r2 = -omega0 * (zeta + root);
                var c2 = (velocity0 - r1 * displacement0) / (r2 - r1);
                var c1 = displacement0 - c2;
                var e1 = Math.Exp(r1 * t);
                var e2 = Math.Exp(r2 * t);
                displacement = c1 * e1 + c2 * e2;
                velocity = r1 * c1 * e1 + r2 * c2 * e2;
            }
        }

        public bool Equals(SpringSpec other) => Stiffness == other.Stiffness && Damping == other.Damping && Mass == other.Mass;
        public override bool Equals(object? obj) => obj is SpringSpec other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Stiffness, Damping, Mass);
        public static bool operator ==(SpringSpec left, SpringSpec right) => left.Equals(right);
        public static bool operator !=(SpringSpec left, SpringSpec right) => !left.Equals(right);
    }
}
