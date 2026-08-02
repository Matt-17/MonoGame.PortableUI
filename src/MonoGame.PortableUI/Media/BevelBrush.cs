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

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            var face = ApplyOpacity(Face, opacity);
            var topLeftOuter = ApplyOpacity(Sunken ? OuterDark : OuterLight, opacity);
            var bottomRightOuter = ApplyOpacity(Sunken ? OuterLight : OuterDark, opacity);

            spriteBatch.Draw(SolidColorBrush.Pixel, rect, face);
            DrawFrame(spriteBatch, rect, topLeftOuter, bottomRightOuter);

            if (!_singleLine && rect.Width > 4 && rect.Height > 4)
            {
                var inner = new Rect(rect.Left + 1, rect.Top + 1, rect.Width - 2, rect.Height - 2);
                var topLeftInner = ApplyOpacity(Sunken ? InnerDark : InnerLight, opacity);
                var bottomRightInner = ApplyOpacity(Sunken ? InnerLight : InnerDark, opacity);
                DrawFrame(spriteBatch, inner, topLeftInner, bottomRightInner);
            }
        }

        private static void DrawFrame(SpriteBatch spriteBatch, Rect rect, Color topLeft, Color bottomRight)
        {
            spriteBatch.Draw(SolidColorBrush.Pixel, new Rect(rect.Left, rect.Top, rect.Width, 1), topLeft);
            spriteBatch.Draw(SolidColorBrush.Pixel, new Rect(rect.Left, rect.Top, 1, rect.Height), topLeft);
            spriteBatch.Draw(SolidColorBrush.Pixel, new Rect(rect.Left, rect.Bottom - 1, rect.Width, 1), bottomRight);
            spriteBatch.Draw(SolidColorBrush.Pixel, new Rect(rect.Right - 1, rect.Top, 1, rect.Height), bottomRight);
        }
    }
}
