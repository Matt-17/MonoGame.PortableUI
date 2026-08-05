using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    /// A translucent glass fill with an animated specular sweep. The fill is a rounded solid tint
    /// (so corners are clean — no frosted backdrop seam), and the sweep is built from soft vertical
    /// bands whose vertical extent is inset by the corner radius, so the moving highlight can never
    /// poke out past a rounded corner. Drive it with <see cref="BrushContext.TimeSeconds"/>.
    /// </summary>
    public sealed class ShimmerGlassBrush : Brush
    {
        public ShimmerGlassBrush(Color tintColor)
        {
            TintColor = tintColor;
        }

        public Color TintColor { get; set; }
        public Color SweepColor { get; set; } = Color.White;

        /// <summary>Cycles per second of the sweep.</summary>
        public float SweepSpeed { get; set; } = 0.14f;

        /// <summary>Peak sweep opacity (0..1).</summary>
        public float SweepStrength { get; set; } = 0.25f;

        /// <summary>Total width of the highlight band as a fraction of the element width.</summary>
        public float BandWidthFraction { get; set; } = 0.26f;

        public override void Draw(SpriteBatch spriteBatch, Rect rect)
            => Draw(spriteBatch, new BrushContext(rect, default, 1f, spriteBatch.GraphicsDevice));

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
            => Draw(spriteBatch, new BrushContext(rect, default, opacity, spriteBatch.GraphicsDevice));

        public override void Draw(SpriteBatch spriteBatch, in BrushContext context)
        {
            var rect = context.Rect;
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            RoundedRectRenderer.DrawSolid(spriteBatch, rect, context.Radius, ApplyOpacity(TintColor, context.Opacity));
            DrawSweep(spriteBatch, rect, context.Radius, context.Opacity, context.TimeSeconds);
        }

        private void DrawSweep(SpriteBatch spriteBatch, Rect rect, CornerRadius radius, float opacity, float timeSeconds)
        {
            if (SweepStrength <= 0f || opacity <= 0f)
                return;

            var inset = Math.Max(Math.Max(radius.TopLeft, radius.TopRight), Math.Max(radius.BottomLeft, radius.BottomRight));
            var top = rect.Top + inset;
            var bottom = rect.Bottom - inset;
            if (bottom - top <= 1f)
                return;

            // sweepPos travels a little past both edges so the band enters, crosses, then leaves —
            // the >1 / <0 tail gives a quiet gap between shimmers.
            var phase = timeSeconds * SweepSpeed % 1f;
            if (phase < 0)
                phase += 1f;
            var sweepPos = phase * 1.6f - 0.3f;
            var centerX = rect.Left + sweepPos * rect.Width;

            const int bands = 11;
            var totalWidth = rect.Width * BandWidthFraction;
            var bandWidth = totalWidth / bands;
            for (var i = -bands; i <= bands; i++)
            {
                var d = i / (float)bands;
                var falloff = 1f - d * d;                 // soft gaussian-ish peak at the centre
                if (falloff <= 0f)
                    continue;

                var alpha = SweepStrength * opacity * falloff;
                var x = centerX + i * bandWidth;
                spriteBatch.Draw(SolidColorBrush.Pixel, new Rect(x - bandWidth * 0.5f, top, bandWidth + 1f, bottom - top), ApplyOpacity(SweepColor, alpha));
            }
        }
    }
}
