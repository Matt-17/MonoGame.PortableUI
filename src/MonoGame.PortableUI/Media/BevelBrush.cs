using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    ///     Classic 3D-chrome brush (Win95/Amiga/BeOS/NeXT era): a face fill with light strips on
    ///     the top/left and dark strips on the bottom/right (inverted when <see cref="Sunken"/>).
    ///     Drawn from solid rects — resolution independent and device-free.
    /// </summary>
    public sealed class BevelBrush : Brush
    {
        public BevelBrush(Color face, Color outerLight, Color innerLight, Color innerDark, Color outerDark)
        {
            Face = face;
            OuterLight = outerLight;
            InnerLight = innerLight;
            InnerDark = innerDark;
            OuterDark = outerDark;
        }

        /// <summary>Single-line bevel (BeOS-style): only the outer light/dark strips.</summary>
        public BevelBrush(Color face, Color light, Color dark)
            : this(face, light, face, face, dark)
        {
            _singleLine = true;
        }

        private readonly bool _singleLine;

        public Color Face { get; }
        public Color OuterLight { get; }
        public Color InnerLight { get; }
        public Color InnerDark { get; }
        public Color OuterDark { get; }
        public bool Sunken { get; set; }

        public override Thickness ContentInset => new Thickness(_singleLine ? 1 : 2);

        public BevelBrush AsSunken()
        {
            return _singleLine
                ? new BevelBrush(Face, OuterLight, OuterDark) { Sunken = true }
                : new BevelBrush(Face, OuterLight, InnerLight, InnerDark, OuterDark) { Sunken = true };
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect)
        {
            Draw(spriteBatch, rect, 1);
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity) => Draw(spriteBatch, rect, opacity, 1);

        public override void Draw(SpriteBatch spriteBatch, in BrushContext context) => Draw(spriteBatch, context.Rect, context.Opacity, context.Scale);

        private void Draw(SpriteBatch spriteBatch, Rect rect, float opacity, float scale)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;
            // Bevel lines are one layout pixel: whole render pixels under LayoutScale/HiDPI.
            var line = MathF.Max(1, MathF.Round(scale));

            var face = ApplyOpacity(Face, opacity);
            var topLeftOuter = ApplyOpacity(Sunken ? OuterDark : OuterLight, opacity);
            var bottomRightOuter = ApplyOpacity(Sunken ? OuterLight : OuterDark, opacity);

            spriteBatch.Draw(Primitives.Pixel(spriteBatch), rect, face);
            DrawFrame(spriteBatch, rect, topLeftOuter, bottomRightOuter, line);

            if (!_singleLine && rect.Width > 4 * line && rect.Height > 4 * line)
            {
                var inner = new Rect(rect.Left + line, rect.Top + line, rect.Width - 2 * line, rect.Height - 2 * line);
                var topLeftInner = ApplyOpacity(Sunken ? InnerDark : InnerLight, opacity);
                var bottomRightInner = ApplyOpacity(Sunken ? InnerLight : InnerDark, opacity);
                DrawFrame(spriteBatch, inner, topLeftInner, bottomRightInner, line);
            }
        }

        private static void DrawFrame(SpriteBatch spriteBatch, Rect rect, Color topLeft, Color bottomRight, float line)
        {
            spriteBatch.Draw(Primitives.Pixel(spriteBatch), new Rect(rect.Left, rect.Top, rect.Width, line), topLeft);
            spriteBatch.Draw(Primitives.Pixel(spriteBatch), new Rect(rect.Left, rect.Top, line, rect.Height), topLeft);
            spriteBatch.Draw(Primitives.Pixel(spriteBatch), new Rect(rect.Left, rect.Bottom - line, rect.Width, line), bottomRight);
            spriteBatch.Draw(Primitives.Pixel(spriteBatch), new Rect(rect.Right - line, rect.Top, line, rect.Height), bottomRight);
        }
    }
}
