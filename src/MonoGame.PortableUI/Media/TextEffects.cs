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
        private DrawnBy _drawnBy;
        private Color _color = Color.Black;
        private float _width = 1.5f;
        private float _quality = 2f;

        /// <summary>Outline colour, straight alpha. Overlapping stamps make a translucent outline
        /// slightly denser than its alpha; it is multiplied with the text's render opacity.
        /// Changing it on a shown outline redraws (like a brush).</summary>
        public Color Color
        {
            get => _color;
            set
            {
                if (_color == value)
                    return;
                _color = value;
                _drawnBy.Changed();
            }
        }

        /// <summary>Outline width outside the glyphs, in design pixels.</summary>
        public float Width
        {
            get => _width;
            set
            {
                if (_width == value)
                    return;
                _width = value;
                _drawnBy.Changed();
            }
        }

        /// <summary>Spacing of the stamps along each ring, in render pixels (smaller = rounder, more draws).</summary>
        public float Quality
        {
            get => _quality;
            set
            {
                if (_quality == value)
                    return;
                _quality = value;
                _drawnBy.Changed();
            }
        }

        /// <summary>Called while drawing the outline, so a change redraws the engine showing it.</summary>
        internal void MarkDrawn() => _drawnBy.Mark();

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
        private DrawnBy _drawnBy;
        private Color _color = Color.White.WithAlpha(0.45f);
        private float _radius = 4f;

        /// <summary>Glow colour, straight alpha: the alpha is the strength where the copies overlap.
        /// Changing it on a shown glow redraws (like a brush).</summary>
        public Color Color
        {
            get => _color;
            set
            {
                if (_color == value)
                    return;
                _color = value;
                _drawnBy.Changed();
            }
        }

        /// <summary>How far the glow reaches past the glyphs, in design pixels.</summary>
        public float Radius
        {
            get => _radius;
            set
            {
                if (_radius == value)
                    return;
                _radius = value;
                _drawnBy.Changed();
            }
        }

        /// <summary>Called while drawing the glow, so a change redraws the engine showing it.</summary>
        internal void MarkDrawn() => _drawnBy.Mark();

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
