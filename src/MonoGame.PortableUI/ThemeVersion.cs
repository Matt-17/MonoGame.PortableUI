using System.Threading;

namespace MonoGame.PortableUI
{
    internal static class ThemeVersion
    {
        private static long _current;

        public static long Current => Interlocked.Read(ref _current);

        public static long Next()
        {
            return Interlocked.Increment(ref _current);
        }
    }
}
