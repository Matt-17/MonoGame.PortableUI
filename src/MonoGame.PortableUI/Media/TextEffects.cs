using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    ///     A round outline around text (<see cref="Controls.TextBlock.Stroke"/>, theme default
    ///     <see cref="Typography.TextStroke"/>). Drawn by stamping the glyphs on rings around the text, so it
    ///     works with SpriteFonts and every <see cref="Text.UIFont"/> backend; the edge stays round at any size.
    /// </summary>
    public sealed class TextStroke
    {
        /// <summary>Outline colour, straight alpha. Overlapping stamps make a translucent outline
        /// slightly denser than its alpha; it is multiplied with the text's render opacity.</summary>
        public Color Color { get; set; } = Color.Black;

        /// <summary>Outline width outside the glyphs, in design pixels.</summary>
        public float Width { get; set; } = 1.5f;

        /// <summary>Spacing of the stamps along each ring, in render pixels (smaller = rounder, more draws).</summary>
        public float Quality { get; set; } = 2f;

        public TextStroke()
        {
        }

        public TextStroke(Color color, float width)
        {
            Color = color;
            Width = width;
        }
    }

    /// <summary>
    ///     A soft outer glow behind text (<see cref="Controls.TextBlock.Glow"/>, theme default
    ///     <see cref="Typography.TextGlow"/>): soft copies spread around the glyphs up to <see cref="Radius"/>.
    /// </summary>
    public sealed class TextGlow
    {
        /// <summary>Glow colour, straight alpha: the alpha is the strength where the copies overlap.</summary>
        public Color Color { get; set; } = Color.White.WithAlpha(0.45f);

        /// <summary>How far the glow reaches past the glyphs, in design pixels.</summary>
        public float Radius { get; set; } = 4f;

        public TextGlow()
        {
        }

        public TextGlow(Color color, float radius)
        {
            Color = color;
            Radius = radius;
        }
    }
}
