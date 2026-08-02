using System;

namespace MonoGame.PortableUI.Animation
{
    public delegate double Easing(double progress);

    internal enum AnimationProperty
    {
        Scale,
        Translation,
        Opacity,
        Color
    }

    public static class Easings
    {
        public static double Linear(double progress)
        {
            return Clamp(progress);
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
