using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Common
{
    public static class HelperEx
    {
        public static Vector2 ToInts(this Vector2 v)
        {
            return new Vector2((int)v.X, (int)v.Y);
        }

        public static Color Darken(this Color color, float value)
        {
            var multiply = new Color(color.A, color.R - 16, color.G - 16, color.B - 16);
            return multiply;
        }
    }
}