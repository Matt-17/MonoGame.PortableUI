using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    /// A translucent glass fill with an animated specular sweep. The fill is a rounded solid tint
    /// (so corners are clean — no frosted backdrop seam), and the sweep is a soft <em>diagonal</em>
    /// streak of light that rakes across the surface, like a reflection travelling over wet glass.
    /// The streak's vertical extent is inset by the corner radius and every slice is clipped to the
    /// element bounds, so the moving highlight can never poke past a rounded corner or spill onto the
    /// scene behind it. Drive it with <see cref="BrushContext.TimeSeconds"/>.
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

        /// <summary>
        /// Diagonal lean of the streak: the horizontal offset between the top and bottom edges as a
        /// fraction of the element height. 0 is a straight vertical band; positive leans the top left
        /// of the bottom. ~0.3–0.5 reads as a natural glass glint.
        /// </summary>
        public float SweepSkew { get; set; } = 0.4f;

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
            var height = bottom - top;
            if (height <= 1f)
                return;

            // sweepPos travels a little past both edges so the band enters, crosses, then leaves —
            // the >1 / <0 tail gives a quiet gap between shimmers.
            var phase = timeSeconds * SweepSpeed % 1f;
            if (phase < 0)
                phase += 1f;
            var sweepPos = phase * 1.6f - 0.3f;
            var centerX = rect.Left + sweepPos * rect.Width;

            // Slice the height into rows and shear each row's centre sideways, so the streak's
            // constant-intensity lines lean over into a diagonal (a straight vertical band sliding
            // horizontally only ever reads as a moving gradient).
            var shear = height * SweepSkew;               // horizontal offset from top row to bottom row
            var rows = Math.Max(4, (int)(height / 6f));
            var rowHeight = height / rows;

            // Many thin overlapping slices so the horizontal falloff reads as a smooth band, not stripes.
            const int bands = 18;
            var totalWidth = rect.Width * BandWidthFraction;
            var bandWidth = totalWidth / bands;

            for (var r = 0; r < rows; r++)
            {
                var rowY = top + r * rowHeight;
                var vFrac = (r + 0.5f) / rows - 0.5f;      // -0.5 (top) .. +0.5 (bottom)
                var rowCenterX = centerX + vFrac * shear;

                for (var i = -bands; i <= bands; i++)
                {
                    var d = i / (float)bands;
                    var falloff = 1f - d * d;             // soft gaussian-ish peak at the centre
                    falloff *= falloff;                   // sharpen so the tails fade out gently
                    if (falloff <= 0.001f)
                        continue;

                    var alpha = SweepStrength * opacity * falloff;
                    var x = rowCenterX + i * bandWidth;
                    // Clip to the element bounds so the streak can't spill past the rounded edges.
                    var x0 = Math.Max(rect.Left, x - bandWidth);
                    var x1 = Math.Min(rect.Right, x + bandWidth + 1f);
                    if (x1 <= x0)
                        continue;
                    spriteBatch.Draw(SolidColorBrush.Pixel, new Rect(x0, rowY, x1 - x0, rowHeight + 1f), ApplyOpacity(SweepColor, alpha));
                }
            }
        }
    }
}
