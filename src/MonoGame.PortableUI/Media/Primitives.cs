using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    ///     Primitive drawing helpers for custom-drawn controls: filled rects, outlines, lines,
    ///     dashed lines, crosses, diamonds, and text. Uses one shared 1x1 white pixel per
    ///     GraphicsDevice, so custom controls no longer need to create (and leak) their own.
    /// </summary>
    public static class Primitives
    {
        // One pixel per device; the weak table drops the entry when a device is collected.
        private static readonly ConditionalWeakTable<GraphicsDevice, Texture2D> PixelPerDevice = new();

        public static Texture2D Pixel(GraphicsDevice device)
        {
            if (device is null)
                throw new ArgumentNullException(nameof(device));

            if (PixelPerDevice.TryGetValue(device, out var pixel) && !pixel.IsDisposed)
                return pixel;

            pixel = new Texture2D(device, 1, 1);
            pixel.SetData(new[] { Color.White });
            PixelPerDevice.Remove(device);
            PixelPerDevice.Add(device, pixel);
            return pixel;
        }

        public static Texture2D Pixel(SpriteBatch spriteBatch) => Pixel(spriteBatch.GraphicsDevice);

        public static void Fill(SpriteBatch spriteBatch, Rect rect, Color color) =>
            spriteBatch.Draw(Pixel(spriteBatch), rect, color);

        public static void RectOutline(SpriteBatch spriteBatch, Rect rect, Color color, float thickness = 1f)
        {
            Fill(spriteBatch, new Rect(rect.Left, rect.Top, rect.Width, thickness), color);
            Fill(spriteBatch, new Rect(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
            Fill(spriteBatch, new Rect(rect.Left, rect.Top, thickness, rect.Height), color);
            Fill(spriteBatch, new Rect(rect.Right - thickness, rect.Top, thickness, rect.Height), color);
        }

        public static void Line(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness = 1f)
        {
            var delta = end - start;
            var length = delta.Length();
            if (length <= 0)
                return;

            spriteBatch.Draw(
                Pixel(spriteBatch),
                start,
                null,
                color,
                MathF.Atan2(delta.Y, delta.X),
                Vector2.Zero,
                new Vector2(length, thickness),
                SpriteEffects.None,
                0);
        }

        public static void DashedLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness = 1f, float dashLength = 6f)
        {
            var delta = end - start;
            var length = delta.Length();
            if (length <= 0 || dashLength <= 0)
                return;

            var direction = delta / length;
            for (var offset = 0f; offset < length; offset += dashLength * 2f)
            {
                var segmentEnd = Math.Min(offset + dashLength, length);
                Line(spriteBatch, start + direction * offset, start + direction * segmentEnd, color, thickness);
            }
        }

        public static void Cross(SpriteBatch spriteBatch, Vector2 center, Color color, float radius, float thickness = 2f)
        {
            Line(spriteBatch, center + new Vector2(-radius, 0), center + new Vector2(radius, 0), color, thickness);
            Line(spriteBatch, center + new Vector2(0, -radius), center + new Vector2(0, radius), color, thickness);
        }

        public static void Diamond(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, float thickness = 2f)
        {
            Line(spriteBatch, center + new Vector2(-radius, 0), center + new Vector2(0, -radius), color, thickness);
            Line(spriteBatch, center + new Vector2(0, -radius), center + new Vector2(radius, 0), color, thickness);
            Line(spriteBatch, center + new Vector2(radius, 0), center + new Vector2(0, radius), color, thickness);
            Line(spriteBatch, center + new Vector2(0, radius), center + new Vector2(-radius, 0), color, thickness);
        }

        public static void Text(SpriteBatch spriteBatch, string text, Vector2 position, Color color)
        {
            if (FontManager.DefaultFont is null || string.IsNullOrEmpty(text))
                return;
            spriteBatch.DrawString(FontManager.DefaultFont, text, position, color);
        }
    }
}
