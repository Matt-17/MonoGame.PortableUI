using System;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Input
{
    /// <summary>
    ///     Finger velocity like Android's <c>VelocityTracker</c> (LSQ2 strategy): a least-squares quadratic
    ///     through the positions of the last 100 ms, differentiated at the newest sample. Robust against
    ///     the uneven frame spacing that made a per-frame average under- or overshoot.
    /// </summary>
    internal sealed class TouchVelocityTracker
    {
        private const int Capacity = 20;
        private const double HorizonSeconds = 0.1;

        private readonly double[] _time = new double[Capacity];
        private readonly float[] _x = new float[Capacity];
        private readonly float[] _y = new float[Capacity];
        private int _count;
        private int _newest = -1;

        public TimeSpan LastSampleTime { get; private set; }

        public void Clear()
        {
            _count = 0;
            _newest = -1;
        }

        /// <summary>Adds a position; a second one at the same time replaces the first (one sample per frame).</summary>
        public void Add(TimeSpan time, PointF position)
        {
            var seconds = time.TotalSeconds;
            if (_count == 0 || seconds > _time[_newest])
            {
                _newest = (_newest + 1) % Capacity;
                _count = Math.Min(_count + 1, Capacity);
            }
            _time[_newest] = seconds;
            _x[_newest] = position.X;
            _y[_newest] = position.Y;
            LastSampleTime = time;
        }

        /// <summary>Velocity in position units per second (zero without two samples in the horizon).</summary>
        public PointF GetVelocity()
        {
            if (_count < 2)
                return new PointF();
            Span<double> t = stackalloc double[Capacity];
            Span<double> xs = stackalloc double[Capacity];
            Span<double> ys = stackalloc double[Capacity];
            var newestTime = _time[_newest];
            var n = 0;
            for (var i = 0; i < _count; i++)
            {
                var index = (_newest - i + Capacity) % Capacity;
                var age = newestTime - _time[index];
                if (age > HorizonSeconds)
                    break;
                t[n] = -age;
                xs[n] = _x[index];
                ys[n] = _y[index];
                n++;
            }
            if (n < 2)
                return new PointF();
            return new PointF((float)Slope(t[..n], xs[..n]), (float)Slope(t[..n], ys[..n]));
        }

        /// <summary>Derivative at t = 0 of the least-squares fit (quadratic from 3 samples, else linear).</summary>
        private static double Slope(ReadOnlySpan<double> t, ReadOnlySpan<double> v)
        {
            double s0 = t.Length, s1 = 0, s2 = 0, s3 = 0, s4 = 0, b0 = 0, b1 = 0, b2 = 0;
            for (var i = 0; i < t.Length; i++)
            {
                var ti = t[i];
                var t2 = ti * ti;
                s1 += ti;
                s2 += t2;
                s3 += t2 * ti;
                s4 += t2 * t2;
                b0 += v[i];
                b1 += v[i] * ti;
                b2 += v[i] * t2;
            }

            if (t.Length >= 3)
            {
                // Normal equations of v = a + b·t + c·t², solved for b by Cramer's rule.
                var det = s0 * (s2 * s4 - s3 * s3) - s1 * (s1 * s4 - s3 * s2) + s2 * (s1 * s3 - s2 * s2);
                if (Math.Abs(det) > 1e-18)
                    return (s0 * (b1 * s4 - s3 * b2) - b0 * (s1 * s4 - s3 * s2) + s2 * (s1 * b2 - b1 * s2)) / det;
            }

            var linearDet = s0 * s2 - s1 * s1;
            return Math.Abs(linearDet) > 1e-18 ? (s0 * b1 - s1 * b0) / linearDet : 0;
        }
    }
}
