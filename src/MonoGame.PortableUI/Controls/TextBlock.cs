using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Controls
{
    public class TextBlock : Control
    {
        private TextAlignment _textAlignment;
        protected SpriteFont? Font;
        private SpriteFont? _fontOverride;
        private ITextMeasurer _textMeasurer;
        private string _text = "";
        private int _textSize;
        private Color _textColor;

        /// <summary>
        ///     Explicit SpriteFont for this block (e.g. a specific size/weight loaded via
        ///     <see cref="FontManager.GetFont"/>). Wins over theme/default font resolution;
        ///     set null to fall back to the default font again.
        /// </summary>
        public SpriteFont? FontOverride
        {
            get { return _fontOverride; }
            set
            {
                if (ReferenceEquals(_fontOverride, value))
                    return;
                _fontOverride = value;
                Font = value ?? FontManager.DefaultFont;
                _textMeasurer = Font != null ? new SpriteFontTextMeasurer(Font) : ApproximateTextMeasurer.Default;
                MeasuredText = MeasureText(Text);
                InvalidateLayout(true);
            }
        }

        public TextAlignment TextAlignment
        {
            get { return _textAlignment; }
            set
            {
                _textAlignment = value;
                InvalidateLayout(false);
            }
        }

        public Color TextColor
        {
            get { return _textColor; }
            set
            {
                if (_textColor == value)
                    return;
                _textColor = value;
                InvalidateLayout(false);
            }
        }
        public Vector2 MeasuredText { get; private set; }

        /// <summary>Soft drop-shadow colour; fully transparent (the default) disables the shadow.</summary>
        public Color ShadowColor { get; set; } = Color.Transparent;

        /// <summary>Offset of the drop shadow from the text, in design pixels.</summary>
        public Vector2 ShadowOffset { get; set; } = new Vector2(0, 3);

        /// <summary>Extra soft-spread radius; the shadow is stamped around the offset to blur it.</summary>
        public float ShadowBlur { get; set; } = 2f;

        public string Text
        {
            get { return _text; }
            set
            {
                value = value ?? "";
                if (_text == value)
                    return;
                _text = value;
                MeasuredText = MeasureText(_text);
                InvalidateLayout(true);
            }
        }

        public int TextSize
        {
            get { return _textSize; }
            set
            {
                if (_textSize == value)
                    return;
                _textSize = value;
                MeasuredText = MeasureText(Text);
                InvalidateLayout(true);
            }
        }

        /// <summary>
        ///     Scale applied to the (bitmap) font so it renders at <see cref="TextSize"/> rather than
        ///     the size it was baked at. 1 when the requested size matches the baked size.
        /// </summary>
        private float FontScale
        {
            get
            {
                if (Font == null || _textSize <= 0)
                    return 1f;
                var baked = FontManager.GetBakedSize(Font);
                return baked > 0 ? (float)_textSize / baked : 1f;
            }
        }

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            var measuredText = MeasureText(Text);
            var width = Width.IsFixed() ? Width : measuredText.X;
            var height = Height.IsFixed() ? Height : 0;
            if (measuredText.Y > height)
                height = measuredText.Y;

            return ApplyConstraints(new Size(width, height)) + Margin;
        }

        public TextBlock()
        {
            var theme = PortableTheme.ResolveCurrent();

            Font = FontManager.DefaultFont;
            _textMeasurer = Font != null ? new SpriteFontTextMeasurer(Font) : ApproximateTextMeasurer.Default;
            TextColor = theme.TextColor;
            TextSize = theme.TextSize;
            TextAlignment = TextAlignment.Left;
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

            if (TextColor.Equals(oldTheme.TextColor))
                TextColor = newTheme.TextColor;
            if (TextSize == oldTheme.TextSize)
                TextSize = newTheme.TextSize;

            var font = TryResolveThemeFont(newTheme);
            if (_fontOverride == null && font != null && !ReferenceEquals(Font, font))
            {
                Font = font;
                _textMeasurer = new SpriteFontTextMeasurer(Font);
                MeasuredText = MeasureText(Text);
                InvalidateLayout(true);
            }
        }

        private static SpriteFont? TryResolveThemeFont(PortableTheme theme)
        {
            var name = theme.Typography?.FontName;
            if (string.IsNullOrEmpty(name) || string.Equals(name, "default", StringComparison.OrdinalIgnoreCase))
                return FontManager.DefaultFont;

            // Theme font not built by the host — FontManager warns once and we stay on the default.
            return FontManager.GetFontOrDefault(name);
        }

        public ITextMeasurer TextMeasurer
        {
            get { return _textMeasurer; }
            set
            {
                _textMeasurer = value ?? ApproximateTextMeasurer.Default;
                MeasuredText = MeasureText(Text);
                InvalidateLayout(true);
            }
        }

        protected Vector2 MeasureText(string text)
        {
            if (Font != null)
                return Font.MeasureString(text ?? "") * FontScale;
            return TextMeasurer.MeasureString(text ?? "");
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {                    
            base.OnDraw(spriteBatch, rect);
            var offset = rect.Offset;
            // MeasuredText already includes FontScale; the draw scale must apply it on top of the
            // control-transform RenderScale so glyphs render at the requested TextSize.
            var drawScale = RenderScale * FontScale;
            var measuredText = new Vector2(MeasuredText.X * RenderScale.X, MeasuredText.Y * RenderScale.Y);
            offset.Y += (rect.Height - measuredText.Y) / 2;

            switch (TextAlignment)
            {
                case TextAlignment.Left:
                    break;
                case TextAlignment.Center:
                    offset.X += (rect.Width - measuredText.X) / 2;
                    break;
                case TextAlignment.Right:
                    offset.X += rect.Width - measuredText.X;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (SnapToPixel)
                offset = offset.ToInts();
            if (Font == null)
                return;

            if (ShadowColor.A > 0)
            {
                var shadow = Brush.ApplyOpacity(ShadowColor, RenderOpacity);
                var blur = MathHelper.Clamp(ShadowBlur, 0f, 6f);
                // A ring of low-alpha stamps around the offset reads as a soft blurred shadow
                // without a render target; the axis-aligned base stamp anchors it.
                Span<Vector2> spread = stackalloc Vector2[]
                {
                    Vector2.Zero,
                    new Vector2(blur, 0), new Vector2(-blur, 0),
                    new Vector2(0, blur), new Vector2(0, -blur),
                    new Vector2(blur, blur), new Vector2(-blur, blur),
                    new Vector2(blur, -blur), new Vector2(-blur, -blur),
                };
                foreach (var d in spread)
                {
                    var pos = offset + ShadowOffset * RenderScale + d;
                    if (SnapToPixel)
                        pos = pos.ToInts();
                    spriteBatch.DrawString(Font, Text, pos, shadow, 0, Vector2.Zero, drawScale, SpriteEffects.None, 0);
                }
            }

            spriteBatch.DrawString(Font, Text, offset, Brush.ApplyOpacity(TextColor, RenderOpacity), 0, Vector2.Zero, drawScale, SpriteEffects.None, 0);
        }
    }
}
