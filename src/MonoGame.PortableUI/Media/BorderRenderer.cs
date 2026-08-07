using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    /// Shared square border drawing (four edge rects with a brush), previously duplicated across
    /// Control, CheckBox and Slider. Rounded borders go through <see cref="RoundedRectRenderer.DrawBorder"/>.
    /// </summary>
    internal static class BorderRenderer
    {
        public static void Draw(SpriteBatch spriteBatch, Rect rect, Thickness thickness, Brush brush, float opacity = 1f)
        {
            if (thickness.Top > 0)
                brush.Draw(spriteBatch, new Rect(rect.Left, rect.Top, rect.Width, thickness.Top), opacity);
            if (thickness.Left > 0)
                brush.Draw(spriteBatch, new Rect(rect.Left, rect.Top, thickness.Left, rect.Height), opacity);
            if (thickness.Right > 0)
                brush.Draw(spriteBatch, new Rect(rect.Right - thickness.Right, rect.Top, thickness.Right, rect.Height), opacity);
            if (thickness.Bottom > 0)
                brush.Draw(spriteBatch, new Rect(rect.Left, rect.Bottom - thickness.Bottom, rect.Width, thickness.Bottom), opacity);
        }

        public static void Draw(SpriteBatch spriteBatch, Rect rect, float width, Brush brush, float opacity = 1f)
        {
            Draw(spriteBatch, rect, new Thickness(width), brush, opacity);
        }
    }
}
