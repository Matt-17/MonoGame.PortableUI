using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    /// <summary>One ring of a <see cref="FrameBrush"/>: a color and a width in layout units.</summary>
    public readonly record struct FrameRing(Color Color, float Width);

    /// <summary>
    ///     A face inside concentric square rings, outermost first — the Game Boy double-line dialog
    ///     box, a default button's extra outline, LCD/pixel frames. Optionally notches the corners
    ///     by one ring width (pixel-art rounding). Solid rects only: device free and crisp.
    /// </summary>
    public sealed class FrameBrush : Brush
    {
        public FrameBrush(Color face, params FrameRing[] rings)
        {
            Face = face;
            Rings = rings ?? Array.Empty<FrameRing>();
        }

        public Color Face { get; }
        public FrameRing[] Rings { get; }

        /// <summary>Leaves the outer ring's corner pixels out (pixel-art rounded corners).</summary>
        public bool NotchCorners { get; init; }

        public override void Draw(SpriteBatch spriteBatch, Rect rect) => Draw(spriteBatch, rect, 1);

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity) => Draw(spriteBatch, rect, opacity, 1);

        public override void Draw(SpriteBatch spriteBatch, in BrushContext context) => Draw(spriteBatch, context.Rect, context.Opacity, context.Scale);

        private void Draw(SpriteBatch spriteBatch, Rect rect, float opacity, float scale)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;
            var pixel = Primitives.Pixel(spriteBatch);
            for (var i = 0; i < Rings.Length; i++)
            {
                var ring = Rings[i];
                var width = MathF.Max(1, MathF.Round(ring.Width * scale));
                if (rect.Width <= 2 * width || rect.Height <= 2 * width)
                    break;
                var color = ApplyOpacity(ring.Color, opacity);
                var notch = NotchCorners && i == 0 ? width : 0;
                spriteBatch.Draw(pixel, new Rect(rect.Left + notch, rect.Top, rect.Width - 2 * notch, width), color);
                spriteBatch.Draw(pixel, new Rect(rect.Left + notch, rect.Bottom - width, rect.Width - 2 * notch, width), color);
                spriteBatch.Draw(pixel, new Rect(rect.Left, rect.Top + width, width, rect.Height - 2 * width), color);
                spriteBatch.Draw(pixel, new Rect(rect.Right - width, rect.Top + width, width, rect.Height - 2 * width), color);
                rect = new Rect(rect.Left + width, rect.Top + width, rect.Width - 2 * width, rect.Height - 2 * width);
            }
            if (Face.A > 0 && rect.Width > 0 && rect.Height > 0)
                spriteBatch.Draw(pixel, rect, ApplyOpacity(Face, opacity));
        }
    }
}
