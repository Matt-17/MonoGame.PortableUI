using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Demo
{
    public sealed class GlassBackdropBrush : Brush
    {
        private readonly GradientBrush _baseGradient = new GradientBrush(
            new Color(9, 14, 28),
            new Color(42, 61, 78),
            GradientDirection.DiagonalDown);

        public override void Draw(SpriteBatch spriteBatch, Rect rect)
        {
            Draw(spriteBatch, rect, 1);
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            _baseGradient.Draw(spriteBatch, rect, opacity);
            DrawSoftBand(spriteBatch, rect, 0.18f, 0.16f, rect.Width * 0.75f, 72, -0.32f, new Color(84, 237, 218, 128), opacity);
            DrawSoftBand(spriteBatch, rect, 0.72f, 0.18f, rect.Width * 0.7f, 64, 0.28f, new Color(255, 149, 118, 116), opacity);
            DrawSoftBand(spriteBatch, rect, 0.54f, 0.64f, rect.Width * 0.9f, 92, -0.24f, new Color(118, 131, 255, 98), opacity);
            DrawSoftBand(spriteBatch, rect, 0.22f, 0.86f, rect.Width * 0.65f, 58, 0.2f, new Color(255, 217, 128, 82), opacity);
            DrawUnderlayCards(spriteBatch, rect, opacity);
            DrawGrid(spriteBatch, rect, opacity);
        }

        private static void DrawSoftBand(SpriteBatch spriteBatch, Rect rect, float x, float y, float width, float height, float rotation, Color color, float opacity)
        {
            for (var i = -3; i <= 3; i++)
            {
                var alphaScale = 1f - System.Math.Abs(i) * 0.18f;
                var bandColor = new Color(color.R, color.G, color.B, (byte)(color.A * alphaScale));
                DrawRotatedRect(spriteBatch, rect, x, y + i * 0.01f, width, height + System.Math.Abs(i) * 16, rotation, bandColor, opacity);
            }
        }

        private static void DrawUnderlayCards(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            DrawRect(spriteBatch, rect.Left + rect.Width * 0.05f, rect.Top + rect.Height * 0.2f, 250, 46, new Color(255, 255, 255, 35), opacity);
            DrawRect(spriteBatch, rect.Left + rect.Width * 0.08f, rect.Top + rect.Height * 0.29f, 165, 18, new Color(84, 237, 218, 84), opacity);
            DrawRect(spriteBatch, rect.Left + rect.Width * 0.08f, rect.Top + rect.Height * 0.34f, 220, 18, new Color(255, 255, 255, 28), opacity);

            DrawRect(spriteBatch, rect.Left + rect.Width * 0.72f, rect.Top + rect.Height * 0.2f, 210, 42, new Color(255, 149, 118, 70), opacity);
            DrawRect(spriteBatch, rect.Left + rect.Width * 0.76f, rect.Top + rect.Height * 0.28f, 150, 18, new Color(255, 255, 255, 30), opacity);
            DrawRect(spriteBatch, rect.Left + rect.Width * 0.78f, rect.Top + rect.Height * 0.34f, 92, 18, new Color(118, 131, 255, 60), opacity);

            DrawRect(spriteBatch, rect.Left + rect.Width * 0.38f, rect.Top + rect.Height * 0.77f, 300, 38, new Color(255, 255, 255, 28), opacity);
            DrawRect(spriteBatch, rect.Left + rect.Width * 0.42f, rect.Top + rect.Height * 0.84f, 190, 16, new Color(84, 237, 218, 58), opacity);
        }

        private static void DrawGrid(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            const int spacing = 44;
            var verticalColor = new Color(255, 255, 255, 18);
            var horizontalColor = new Color(255, 255, 255, 12);

            for (var x = rect.Left; x < rect.Right; x += spacing)
                DrawRect(spriteBatch, x, rect.Top, 1, rect.Height, verticalColor, opacity);

            for (var y = rect.Top; y < rect.Bottom; y += spacing)
                DrawRect(spriteBatch, rect.Left, y, rect.Width, 1, horizontalColor, opacity);
        }

        private static void DrawRotatedRect(SpriteBatch spriteBatch, Rect rect, float x, float y, float width, float height, float rotation, Color color, float opacity)
        {
            var position = new Vector2(rect.Left + rect.Width * x, rect.Top + rect.Height * y);
            spriteBatch.Draw(
                SolidColorBrush.Pixel,
                position,
                null,
                ApplyOpacity(color, opacity),
                rotation,
                new Vector2(0.5f, 0.5f),
                new Vector2(width, height),
                SpriteEffects.None,
                0);
        }

        private static void DrawRect(SpriteBatch spriteBatch, float left, float top, float width, float height, Color color, float opacity)
        {
            spriteBatch.Draw(SolidColorBrush.Pixel, new Rect(left, top, width, height), ApplyOpacity(color, opacity));
        }
    }
}
