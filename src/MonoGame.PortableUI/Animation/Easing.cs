using System;

namespace MonoGame.PortableUI.Animation
{
    public delegate double Easing(double progress);

    internal enum AnimationProperty
    {
        Scale,
        Translation,
        Opacity,
        Rotation,
        Skew,
        Color,
        Value
    }

    public static class Easings
    {
        public static double Linear(double progress)
        {
            return Clamp(progress);
        }

        /// <summary>
        ///     The "fast out, slow in" curve of Android and Material (cubic Bézier 0.4, 0, 0.2, 1): it gathers speed
        ///     instead of jumping off and settles long - the default for screen transitions.
        /// </summary>
        public static readonly Easing Standard = CubicBezier(0.4, 0, 0.2, 1);

        /// <summary>
        ///     A CSS-style <c>cubic-bezier(x1, y1, x2, y2)</c> curve from (0, 0) to (1, 1). Create it once and keep it:
        ///     evaluating allocates nothing.
        /// </summary>
        public static Easing CubicBezier(double x1, double y1, double x2, double y2)
        {
            x1 = Clamp(x1);
            x2 = Clamp(x2);
            return progress =>
            {
                var x = Clamp(progress);
                if (x <= 0 || x >= 1)
                    return x;

                // Solve bezier_x(t) = x: Newton steps, bisection when the slope is flat.
                var t = x;
                for (var i = 0; i < 8; i++)
                {
                    var error = Bezier(t, x1, x2) - x;
                    if (Math.Abs(error) < 1e-6)
                        return Bezier(t, y1, y2);
                    var slope = BezierSlope(t, x1, x2);
                    if (Math.Abs(slope) < 1e-6)
                        break;
                    t -= error / slope;
                }

                double low = 0, high = 1;
                t = x;
                for (var i = 0; i < 30; i++)
                {
                    var value = Bezier(t, x1, x2);
                    if (Math.Abs(value - x) < 1e-6)
                        break;
                    if (value < x)
                        low = t;
                    else
                        high = t;
                    t = (low + high) / 2;
                }
                return Bezier(t, y1, y2);
            };
        }

        // One coordinate of the curve through 0, p1, p2, 1.
        private static double Bezier(double t, double p1, double p2)
        {
            var u = 1 - t;
            return 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t;
        }

        private static double BezierSlope(double t, double p1, double p2)
        {
            var u = 1 - t;
            return 3 * u * u * p1 + 6 * u * t * (p2 - p1) + 3 * t * t * (1 - p2);
        }

        public static double CubicOut(double progress)
        {
            var t = Clamp(progress) - 1;
            return t * t * t + 1;
        }

        public static double CubicInOut(double progress)
        {
            var t = Clamp(progress);
            if (t < 0.5)
                return 4 * t * t * t;

            var eased = -2 * t + 2;
            return 1 - eased * eased * eased / 2;
        }

        public static double QuadIn(double progress)
        {
            var t = Clamp(progress);
            return t * t;
        }

        public static double QuadOut(double progress)
        {
            var t = Clamp(progress);
            return 1 - (1 - t) * (1 - t);
        }

        public static double QuadInOut(double progress)
        {
            var t = Clamp(progress);
            return t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;
        }

        public static double ExpoIn(double progress)
        {
            var t = Clamp(progress);
            return t <= 0 ? 0 : Math.Pow(2, 10 * t - 10);
        }

        public static double ExpoOut(double progress)
        {
            var t = Clamp(progress);
            return t >= 1 ? 1 : 1 - Math.Pow(2, -10 * t);
        }

        public static double ExpoInOut(double progress)
        {
            var t = Clamp(progress);
            if (t <= 0)
                return 0;
            if (t >= 1)
                return 1;
            return t < 0.5
                ? Math.Pow(2, 20 * t - 10) / 2
                : (2 - Math.Pow(2, -20 * t + 10)) / 2;
        }

        public static double BackOut(double progress)
        {
            const double c1 = 1.70158;
            const double c3 = c1 + 1;
            var t = Clamp(progress) - 1;
            return 1 + c3 * t * t * t + c1 * t * t;
        }

        public static double BackIn(double progress)
        {
            const double c1 = 1.70158;
            const double c3 = c1 + 1;
            var t = Clamp(progress);
            return c3 * t * t * t - c1 * t * t;
        }

        public static double BackInOut(double progress)
        {
            const double c1 = 1.70158;
            const double c2 = c1 * 1.525;
            var t = Clamp(progress);
            return t < 0.5
                ? Math.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2) / 2
                : (Math.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
        }

        public static double ElasticOut(double progress)
        {
            var t = Clamp(progress);
            if (t <= 0)
                return 0;
            if (t >= 1)
                return 1;

            const double c4 = 2 * Math.PI / 3;
            return Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c4) + 1;
        }

        public static double BounceOut(double progress)
        {
            var t = Clamp(progress);
            const double n1 = 7.5625;
            const double d1 = 2.75;

            if (t < 1 / d1)
                return n1 * t * t;
            if (t < 2 / d1)
            {
                t -= 1.5 / d1;
                return n1 * t * t + 0.75;
            }
            if (t < 2.5 / d1)
            {
                t -= 2.25 / d1;
                return n1 * t * t + 0.9375;
            }

            t -= 2.625 / d1;
            return n1 * t * t + 0.984375;
        }

        private static double Clamp(double progress)
        {
            return Math.Max(0, Math.Min(1, progress));
        }
    }
}
