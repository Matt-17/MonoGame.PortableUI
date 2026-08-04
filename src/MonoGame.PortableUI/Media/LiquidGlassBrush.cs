using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public sealed class LiquidGlassBrush : AcrylicBrush
    {
        public LiquidGlassBrush()
            : base(new Color(255, 255, 255, 54))
        {
            BlurRadius = 20;
            GrainOpacity = 0.05f;
            SaturationBoost = 0.32f;
        }

        public float EdgeRefractionStrength { get; set; } = 0.018f;
        public float SpecularSweepStrength { get; set; } = 0.28f;
        public float SpecularSweepSpeed { get; set; } = 0.08f;
        public CornerStyle CornerStyle { get; set; } = CornerStyle.Squircle;

        public override void Draw(SpriteBatch spriteBatch, in BrushContext context)
        {
            Draw(spriteBatch, context.Rect, context.Opacity);
            DrawSpecularSweep(spriteBatch, context.Rect, context.Opacity, context.TimeSeconds);
        }

        private void DrawSpecularSweep(SpriteBatch spriteBatch, Rect rect, float opacity, float timeSeconds)
        {
            if (SpecularSweepStrength <= 0 || SpecularSweepSpeed <= 0 || rect.Width <= 0 || rect.Height <= 0)
                return;

            var phase = timeSeconds * SpecularSweepSpeed % 1f;
            if (phase < 0)
                phase += 1;

            var sweepCenterX = rect.Left + (phase * 1.5f - 0.25f) * rect.Width;
            var strength = MathHelper.Clamp(SpecularSweepStrength, 0, 1) * MathHelper.Clamp(opacity, 0, 1);
            const int bands = 5;
            var bandWidth = Math.Max(8f, rect.Width * 0.05f);
            var centerY = rect.Top + rect.Height / 2;

            for (var i = 0; i < bands; i++)
            {
                var band = i - bands / 2;
                var falloff = 1 - Math.Abs(band) / (bands / 2f + 1);
                var color = Premultiply(new Color((byte)255, (byte)255, (byte)255, (byte)(64 * strength * falloff)));
                spriteBatch.Draw(
                    SolidColorBrush.Pixel,
                    new Vector2(sweepCenterX + band * bandWidth, centerY),
                    null,
                    color,
                    0.35f,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(bandWidth, rect.Height * 1.8f),
                    SpriteEffects.None,
                    0);
            }
        }
    }
}
