namespace MonoGame.PortableUI.Common
{
    public static class SizeEx
    {
        public static bool IsFixed(this float f)
        {
            return !float.IsNaN(f) && !float.IsInfinity(f);
        }
        public static bool IsBounded(this float f) { return !float.IsInfinity(f); }
    }
}