using System;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     How a <see cref="ScrollViewer"/> coasts after the finger lifts: the release speed becomes a
    ///     <see cref="FlingTrajectory"/> (distance and duration fixed at the start, like a platform scroller).
    ///     Velocities and distances are in layout units (per second).
    ///     <list type="bullet">
    ///         <item><see cref="Android(float, float, float, float)"/>: the spline of Android's <c>OverScroller</c>
    ///         (the default on Android, with the system's friction and fling limits).</item>
    ///         <item><see cref="IOS"/>: UIKit's <c>UIScrollView</c> deceleration (exponential, rate 0.998 per ms).</item>
    ///         <item><see cref="Exponential"/>: exponential braking with a chosen rate (the default elsewhere,
    ///         from <see cref="ScrollViewer.FlingDeceleration"/>).</item>
    ///     </list>
    /// </summary>
    public abstract class FlingPhysics
    {
        /// <summary>Releases slower than this do not fling (layout units per second).</summary>
        public float MinimumVelocity { get; init; }

        /// <summary>Faster releases are capped to this speed (layout units per second).</summary>
        public float MaximumVelocity { get; init; } = float.PositiveInfinity;

        /// <summary>A finger that rested this long before lifting has no speed left.</summary>
        public TimeSpan RestTime { get; init; } = TimeSpan.FromMilliseconds(100);

        /// <summary>The coast for a release speed (layout units per second, &gt; 0).</summary>
        public abstract FlingTrajectory Start(float speed);

        /// <summary>Applies <see cref="MinimumVelocity"/> and <see cref="MaximumVelocity"/>: 0 when too slow.</summary>
        public float LimitSpeed(float speed)
            => speed < MinimumVelocity ? 0 : Math.Min(speed, MaximumVelocity);

        /// <summary>Exponential braking: the speed falls by <c>e^(-deceleration·t)</c>; the distance is speed / deceleration.</summary>
        public static FlingPhysics Exponential(float deceleration) => new ExponentialFlingPhysics(deceleration);

        /// <summary>
        ///     UIKit <c>UIScrollView.DecelerationRate.normal</c>: the speed keeps 0.998 of itself per
        ///     millisecond (deceleration ≈ 2.0 per second).
        /// </summary>
        public static FlingPhysics IOS { get; } = new ExponentialFlingPhysics(-1000f * MathF.Log(0.998f));

        /// <summary>
        ///     Android's <c>OverScroller</c> spline (AOSP <c>SplineOverScroller</c>): the distance grows
        ///     with the logarithm of the speed, the coast eases out to a stop.
        /// </summary>
        /// <param name="unitsPerInch">Layout units per inch; Android uses density × 160 pixels, so 160 for dp.</param>
        /// <param name="friction"><c>ViewConfiguration.getScrollFriction()</c> (0.015).</param>
        /// <param name="minimumVelocity"><c>getScaledMinimumFlingVelocity()</c> in layout units (50 dp/s).</param>
        /// <param name="maximumVelocity"><c>getScaledMaximumFlingVelocity()</c> in layout units (8000 dp/s).</param>
        public static FlingPhysics Android(float unitsPerInch = 160f, float friction = 0.015f,
            float minimumVelocity = 50f, float maximumVelocity = 8000f)
            => new AndroidFlingPhysics(unitsPerInch, friction)
            {
                MinimumVelocity = minimumVelocity,
                MaximumVelocity = maximumVelocity,
                // VelocityTracker's ASSUME_POINTER_STOPPED_TIME.
                RestTime = TimeSpan.FromMilliseconds(40)
            };

#if ANDROID
        /// <summary>
        ///     <see cref="Android(float, float, float, float)"/> with this device's values: friction and
        ///     fling limits from <c>ViewConfiguration</c>, inches from the display density.
        /// </summary>
        /// <param name="pixelsPerUnit">Screen pixels per layout unit (the engine's render scale, e.g. the density).</param>
        public static FlingPhysics FromAndroidSystem(float pixelsPerUnit)
        {
            if (pixelsPerUnit <= 0)
                pixelsPerUnit = 1;
            var context = global::Android.App.Application.Context;
            var density = context.Resources?.DisplayMetrics?.Density ?? 1f;
            var configuration = global::Android.Views.ViewConfiguration.Get(context);
            return Android(
                unitsPerInch: density * 160f / pixelsPerUnit,
                friction: global::Android.Views.ViewConfiguration.ScrollFriction,
                minimumVelocity: (configuration?.ScaledMinimumFlingVelocity ?? 50 * density) / pixelsPerUnit,
                maximumVelocity: (configuration?.ScaledMaximumFlingVelocity ?? 8000 * density) / pixelsPerUnit);
        }
#endif
    }

    /// <summary>One coast: the travelled distance and speed over time, along the release direction.</summary>
    public abstract class FlingTrajectory
    {
        // Duration is a TimeSpan (100 ns ticks): at Duration the coast counts as stopped.
        private protected const double EndTolerance = 1e-6;

        /// <summary>Total distance (layout units).</summary>
        public abstract float Distance { get; }

        /// <summary>Time until the coast stops.</summary>
        public abstract TimeSpan Duration { get; }

        /// <summary>Distance travelled after <paramref name="seconds"/>.</summary>
        public abstract float PositionAt(double seconds);

        /// <summary>Speed after <paramref name="seconds"/> (0 once stopped).</summary>
        public abstract float VelocityAt(double seconds);
    }

    internal sealed class ExponentialFlingPhysics : FlingPhysics
    {
        // Below this speed (layout units per second) the coast counts as stopped.
        internal const float StopSpeed = 12f;

        public ExponentialFlingPhysics(float deceleration)
        {
            Deceleration = Math.Max(0.01f, deceleration);
        }

        public float Deceleration { get; }

        public override FlingTrajectory Start(float speed) => new Trajectory(speed, Deceleration);

        private sealed class Trajectory : FlingTrajectory
        {
            private readonly float _speed;
            private readonly float _k;
            private readonly double _duration;

            public Trajectory(float speed, float k)
            {
                _speed = speed;
                _k = k;
                _duration = speed > StopSpeed ? Math.Log(speed / StopSpeed) / k : 0;
                Distance = PositionAt(_duration);
            }

            public override float Distance { get; }

            public override TimeSpan Duration => TimeSpan.FromSeconds(_duration);

            public override float PositionAt(double seconds)
                => (float)(_speed / _k * (1 - Math.Exp(-_k * Math.Clamp(seconds, 0, _duration))));

            public override float VelocityAt(double seconds)
                => seconds >= _duration - EndTolerance ? 0 : (float)(_speed * Math.Exp(-_k * Math.Max(0, seconds)));
        }
    }

    /// <summary>Port of AOSP <c>OverScroller.SplineOverScroller</c> (Apache License 2.0), fling part.</summary>
    internal sealed class AndroidFlingPhysics : FlingPhysics
    {
        private const int Samples = 100;
        private const float Inflexion = 0.35f;
        private const float StartTension = 0.5f;
        private const float EndTension = 1.0f;
        private const float P1 = StartTension * Inflexion;
        private const float P2 = 1.0f - EndTension * (1.0f - Inflexion);
        private const float GravityEarth = 9.80665f;
        private static readonly double DecelerationRate = Math.Log(0.78) / Math.Log(0.9);
        private static readonly float[] SplinePosition = BuildSplinePosition();

        private readonly double _frictionTimesCoeff;

        public AndroidFlingPhysics(float unitsPerInch, float friction)
        {
            // "look and feel tuning" factor 0.84 and inches per meter as in AOSP.
            var physicalCoeff = GravityEarth * 39.37f * unitsPerInch * 0.84f;
            _frictionTimesCoeff = friction * physicalCoeff;
        }

        public override FlingTrajectory Start(float speed)
        {
            if (speed <= 0)
                return new Trajectory(0, 0);
            var l = Math.Log(Inflexion * speed / _frictionTimesCoeff);
            var distance = _frictionTimesCoeff * Math.Exp(DecelerationRate / (DecelerationRate - 1) * l);
            var duration = Math.Exp(l / (DecelerationRate - 1));
            return new Trajectory((float)distance, duration);
        }

        private static float[] BuildSplinePosition()
        {
            var position = new float[Samples + 1];
            float xMin = 0;
            for (var i = 0; i < Samples; i++)
            {
                var alpha = (float)i / Samples;
                float xMax = 1, x, coef;
                while (true)
                {
                    x = xMin + (xMax - xMin) / 2f;
                    coef = 3f * x * (1f - x);
                    var tx = coef * ((1f - x) * P1 + x * P2) + x * x * x;
                    if (Math.Abs(tx - alpha) < 1e-5f)
                        break;
                    if (tx > alpha)
                        xMax = x;
                    else
                        xMin = x;
                }
                position[i] = coef * ((1f - x) * StartTension + x) + x * x * x;
            }
            position[Samples] = 1f;
            return position;
        }

        private sealed class Trajectory : FlingTrajectory
        {
            private readonly double _duration;

            public Trajectory(float distance, double duration)
            {
                Distance = distance;
                _duration = duration;
            }

            public override float Distance { get; }

            public override TimeSpan Duration => TimeSpan.FromSeconds(_duration);

            public override float PositionAt(double seconds)
            {
                Sample(seconds, out var distanceCoef, out _);
                return distanceCoef * Distance;
            }

            public override float VelocityAt(double seconds)
            {
                Sample(seconds, out _, out var velocityCoef);
                return _duration > 0 ? (float)(velocityCoef * Distance / _duration) : 0;
            }

            private void Sample(double seconds, out float distanceCoef, out float velocityCoef)
            {
                distanceCoef = 1f;
                velocityCoef = 0f;
                if (_duration <= 0 || seconds >= _duration - EndTolerance)
                    return;
                var t = (float)(Math.Max(0, seconds) / _duration);
                var index = (int)(Samples * t);
                if (index >= Samples)
                    return;
                var tInf = (float)index / Samples;
                var tSup = (float)(index + 1) / Samples;
                var dInf = SplinePosition[index];
                var dSup = SplinePosition[index + 1];
                velocityCoef = (dSup - dInf) / (tSup - tInf);
                distanceCoef = dInf + (t - tInf) * velocityCoef;
            }
        }
    }
}
