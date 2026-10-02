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

        public override void Draw(SpriteBatch spriteBatch, in BrushContext context)
        {
            // Rounded body via the frosted-glass path, then the sweep kept inside the shape.
            base.Draw(spriteBatch, in context);
            DrawSpecularSweep(spriteBatch, context.Rect, context.Radius, context.Opacity, context.TimeSeconds);
        }

        private void DrawSpecularSweep(SpriteBatch spriteBatch, Rect rect, CornerRadius radius, float opacity, float timeSeconds)
        {
            if (SpecularSweepStrength <= 0 || SpecularSweepSpeed <= 0 || rect.Width <= 0 || rect.Height <= 0 || !ScreenEngine.AnimatesDecorations)
                return;
            ScreenEngine.RequestAnimationFrame();

            var phase = timeSeconds * SpecularSweepSpeed % 1f;
            if (phase < 0)
                phase += 1;

            var sweepCenterX = rect.Left + (phase * 1.5f - 0.25f) * rect.Width;
            var strength = MathHelper.Clamp(SpecularSweepStrength, 0, 1) * MathHelper.Clamp(opacity, 0, 1);
            const int bands = 5;
            var bandWidth = Math.Max(8f, rect.Width * 0.05f);
            // Upright bands clipped to the body; the rounded ends stay inside the corner arcs.
            var inset = Math.Max(Math.Max(radius.TopLeft, radius.TopRight), Math.Max(radius.BottomLeft, radius.BottomRight));
            for (var i = 0; i < bands; i++)
            {
                var band = i - bands / 2;
                var falloff = 1 - Math.Abs(band) / (bands / 2f + 1);
                var color = Premultiply(new Color((byte)255, (byte)255, (byte)255, (byte)(64 * strength * falloff)));
                var left = Math.Max(rect.Left + inset * 0.3f, sweepCenterX + band * bandWidth - bandWidth / 2);
                var right = Math.Min(rect.Right - inset * 0.3f, sweepCenterX + band * bandWidth + bandWidth / 2);
                if (right <= left)
                    continue;
                var x0 = Math.Min(left - rect.Left, rect.Right - right);
                // Near the rounded ends shorten the band to stay within the arc.
                var cut = x0 < inset ? inset - (float)Math.Sqrt(Math.Max(0, inset * inset - (inset - x0) * (inset - x0))) : 0;
                spriteBatch.Draw(Primitives.Pixel(spriteBatch), new Rect(left, rect.Top + cut, right - left, rect.Height - 2 * cut), color);
            }
        }
    }
}
