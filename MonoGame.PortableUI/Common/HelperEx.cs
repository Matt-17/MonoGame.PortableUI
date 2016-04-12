using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Common
{
    public static class HelperEx
    {
        public static Vector2 ToInts(this Vector2 v)
        {
            return new Vector2((int)v.X, (int)v.Y);
        }
    }
}