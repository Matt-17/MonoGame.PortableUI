using System;

namespace MonoGame.PortableUI.Animation
{
    public delegate double Easing(double progress);

    internal enum AnimationProperty
    {
        Scale,
        Translation,
        Opacity
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

        private static double Clamp(double progress)
        {
            return Math.Max(0, Math.Min(1, progress));
        }
    }
}
