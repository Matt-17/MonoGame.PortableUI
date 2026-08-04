using System;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Common
{
    public static class HelperEx
    {
        public static Vector2 ToInts(this Vector2 v)
        {
            return new Vector2((int)v.X, (int)v.Y);
        }

        /// <summary>Darkens RGB by <paramref name="value"/> (0 = unchanged, 1 = black), preserving alpha.</summary>
        public static Color Darken(this Color color, float value)
        {
            value = MathHelper.Clamp(value, 0f, 1f);
            var scale = 1f - value;
            return new Color((byte)(color.R * scale), (byte)(color.G * scale), (byte)(color.B * scale), color.A);
        }

        /// <summary>Clamps a selection index into [0, itemCount - 1], or -1 when there is nothing to select.</summary>
        public static int ClampSelectionIndex(int value, int itemCount)
        {
            if (itemCount == 0 || value < 0)
                return -1;
            return Math.Max(0, Math.Min(value, itemCount - 1));
        }
    }
}