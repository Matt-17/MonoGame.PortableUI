using System;
using System.Collections.Generic;
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
        private TextWrapping _textWrapping;

        // Wrapped-line cache: wrapping measures every word, so it only recomputes when the
        // text, the available width, or the effective font scale changes.
        private List<string>? _wrappedLines;
        // Two fonts baked at the same size give the same FontScale but different line breaks.
        private SpriteFont? _wrapCacheFont;
        private SpriteFont? _bakedSizeFont;
        private int _bakedSize;
        private string? _wrapCacheText;
        private float _wrapCacheWidth = -1f;
        private float _wrapCacheScale = -1f;
        private readonly List<float> _wrappedLineWidths = new List<float>();
        // Ellipsis result for the last (text, width, font, scale): trimming re-measures per
        // removed character, which must not run every frame.
        private string? _trimCacheText;
        private SpriteFont? _trimCacheFont;
        private float _trimCacheWidth = -1f;
        private float _trimCacheScale = -1f;
        private string _trimmedText = "";
        private Vector2 _trimmedSize;

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
                FontManager.EnsureFallbackCharacter(value);
                _fontOverride = value;
                Font = value ?? FontManager.DefaultFont;
                _textMeasurer = Font != null ? new SpriteFontTextMeasurer(Font) : ApproximateTextMeasurer.Default;
                MeasuredText = MeasureText(Text);
                InvalidateLayout(true);
            }
        }

        private UIFont? _dynamicFont;

        /// <summary>
        ///     Runtime-rasterizing font for this block (e.g. FontStashSharp): any size, any character.
        ///     Wins over <see cref="FontOverride"/> and <see cref="FontManager.DefaultDynamicFont"/>.
        /// </summary>
        public UIFont? DynamicFont
        {
            get => _dynamicFont;
            set
            {
                if (ReferenceEquals(_dynamicFont, value))
                    return;
                _dynamicFont = value;
                OnTextScaleChanged();
            }
        }

        /// <summary>The dynamic font in effect, or null for the SpriteFont path: this block's own, else its
        /// theme's (<see cref="Typography.DynamicFont"/>, per surface/island), else the app default.</summary>
        protected UIFont? ActiveDynamicFont
            => _dynamicFont ?? (_fontOverride == null ? ResolveTheme().Typography?.DynamicFont ?? FontManager.DefaultDynamicFont : null);

        /// <summary>Pixel size text is measured and drawn at with a dynamic font.</summary>
        protected float DynamicPixelSize(UIFont font) => (_textSize > 0 ? _textSize : font.DefaultSize) * TextScaling.Factor;

        /// <summary>True when there is some font to draw with.</summary>
        protected bool HasDrawableFont => Font != null || ActiveDynamicFont != null;

        /// <summary>Draws a run of text with the active backend at the block's size and render scale.</summary>
        protected void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color)
        {
            var dynamicFont = ActiveDynamicFont;
            if (dynamicFont != null)
                dynamicFont.DrawString(spriteBatch, text, position, color, DynamicPixelSize(dynamicFont), RenderScale);
            else if (Font != null)
                spriteBatch.DrawString(Font, text, position, color, 0, Vector2.Zero, RenderScale * FontScale, SpriteEffects.None, 0);
        }

        protected void DrawText(SpriteBatch spriteBatch, System.Text.StringBuilder text, Vector2 position, Color color)
        {
            var dynamicFont = ActiveDynamicFont;
            if (dynamicFont != null)
                dynamicFont.DrawString(spriteBatch, text, position, color, DynamicPixelSize(dynamicFont), RenderScale);
            else if (Font != null)
                spriteBatch.DrawString(Font, text, position, color, 0, Vector2.Zero, RenderScale * FontScale, SpriteEffects.None, 0);
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

        /// <summary>Wrap long text onto multiple lines. For wrapped *measurement* the block
        /// needs a finite width (fixed <see cref="Control.Width"/> or <see cref="Control.MaxWidth"/>);
        /// otherwise the text wraps visually to the arranged width at draw time.</summary>
        public TextWrapping TextWrapping
        {
            get { return _textWrapping; }
            set
            {
                if (_textWrapping == value)
                    return;
                _textWrapping = value;
                _wrappedLines = null;
                InvalidateLayout(true);
            }
        }

        /// <summary>NoWrap only: trim overflowing text with an ellipsis instead of overdrawing.</summary>
        public TextTrimming TextTrimming { get; set; }

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

        private bool _isHeading;

        /// <summary>
        ///     Marks the block as a heading: its <see cref="TextSize"/> follows the theme's
        ///     <see cref="Typography.HeadingSize"/> (re-applied on theme switches unless TextSize was
        ///     changed explicitly afterwards).
        /// </summary>
        public bool IsHeading
        {
            get => _isHeading;
            set
            {
                if (_isHeading == value)
                    return;
                _isHeading = value;
                var typography = ResolveTheme().Typography;
                TextSize = value ? typography.HeadingSize : typography.TextSize;
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
        protected float FontScale
        {
            get
            {
                if (Font == null || _textSize <= 0)
                    return TextScaling.Factor;
                // Read many times per frame; the baked size only changes with the font.
                if (!ReferenceEquals(_bakedSizeFont, Font))
                {
                    _bakedSizeFont = Font;
                    _bakedSize = FontManager.GetBakedSize(Font);
                }
                return (_bakedSize > 0 ? (float)_textSize / _bakedSize : 1f) * TextScaling.Factor;
            }
        }

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            if (TextWrapping == TextWrapping.Wrap && TryGetWrapMeasureWidth(out var wrapWidth))
            {
                var lines = GetWrappedLines(wrapWidth);
                float maxLineWidth = 0;
                foreach (var lineWidth in _wrappedLineWidths)
                    maxLineWidth = Math.Max(maxLineWidth, lineWidth);

                var wrappedWidth = Width.IsFixed() ? Width : Math.Min(wrapWidth, maxLineWidth);
                var wrappedHeight = Height.IsFixed() ? Height : lines.Count * LineHeight;
                return ApplyConstraints(new Size(wrappedWidth, wrappedHeight)) + Margin;
            }

            // MeasuredText is kept current by every setter that affects it (text, size, font).
            var measuredText = MeasuredText;
            var width = Width.IsFixed() ? Width : measuredText.X;
            var height = Height.IsFixed() ? Height : 0;
            if (measuredText.Y > height)
                height = measuredText.Y;

            return ApplyConstraints(new Size(width, height)) + Margin;
        }

        private bool TryGetWrapMeasureWidth(out float wrapWidth)
        {
            wrapWidth = Width.IsFixed() ? Width : MaxWidth.IsFixed() ? MaxWidth : float.NaN;
            return wrapWidth.IsFixed() && wrapWidth > 0;
        }

        /// <summary>Height of one line in the current font, size and text scale.</summary>
        internal float CurrentLineHeight => LineHeight;

        private float LineHeight => ActiveDynamicFont is { } dynamicFont
            ? dynamicFont.GetLineHeight(DynamicPixelSize(dynamicFont))
            : Font != null
                ? Font.LineSpacing * FontScale
                : MeasureText("Ag").Y;

        private IReadOnlyList<string> GetWrappedLines(float availableWidth)
        {
            var scale = FontScale;
            if (_wrappedLines != null &&
                _wrapCacheText == _text &&
                ReferenceEquals(_wrapCacheFont, Font) &&
                Math.Abs(_wrapCacheWidth - availableWidth) < 0.5f &&
                Math.Abs(_wrapCacheScale - scale) < 0.0001f)
            {
                return _wrappedLines;
            }

            _wrappedLines = WrapText(_text, availableWidth);
            _wrappedLineWidths.Clear();
            foreach (var line in _wrappedLines)
                _wrappedLineWidths.Add(MeasureText(line).X);
            _wrapCacheText = _text;
            _wrapCacheFont = Font;
            _wrapCacheWidth = availableWidth;
            _wrapCacheScale = scale;
            return _wrappedLines;
        }

        /// <summary>Greedy word wrap; explicit newlines are respected, and a single word wider
        /// than the available width hard-breaks by characters.</summary>
        private List<string> WrapText(string text, float maxWidth)
        {
            var lines = new List<string>();
            foreach (var paragraph in text.Split('\n'))
            {
                if (maxWidth <= 0 || MeasureText(paragraph).X <= maxWidth)
                {
                    lines.Add(paragraph);
                    continue;
                }

                var current = string.Empty;
                foreach (var word in paragraph.Split(' '))
                {
                    var candidate = current.Length == 0 ? word : current + " " + word;
                    if (MeasureText(candidate).X <= maxWidth)
                    {
                        current = candidate;
                        continue;
                    }

                    if (current.Length > 0)
                        lines.Add(current);

                    current = word;
                    while (current.Length > 1 && MeasureText(current).X > maxWidth)
                    {
                        var cut = current.Length - 1;
                        while (cut > 1 && MeasureText(current[..cut]).X > maxWidth)
                            cut--;
                        lines.Add(current[..cut]);
                        current = current[cut..];
                    }
                }

                lines.Add(current);
            }

            return lines;
        }

        private void GetTrimmedText(float maxWidth, out string text, out Vector2 size)
        {
            var scale = FontScale;
            if (!ReferenceEquals(_trimCacheText, _text)
                || !ReferenceEquals(_trimCacheFont, Font)
                || Math.Abs(_trimCacheWidth - maxWidth) >= 0.5f
                || Math.Abs(_trimCacheScale - scale) >= 0.0001f)
            {
                _trimmedText = TrimWithEllipsis(_text, maxWidth);
                _trimmedSize = ReferenceEquals(_trimmedText, _text) ? MeasuredText : MeasureText(_trimmedText);
                _trimCacheText = _text;
                _trimCacheFont = Font;
                _trimCacheWidth = maxWidth;
                _trimCacheScale = scale;
            }

            text = _trimmedText;
            size = _trimmedSize;
        }

        private string TrimWithEllipsis(string text, float maxWidth)
        {
            const string ellipsis = "...";
            if (maxWidth <= 0 || MeasureText(text).X <= maxWidth)
                return text;

            var cut = text.Length;
            while (cut > 0 && MeasureText(text[..cut] + ellipsis).X > maxWidth)
                cut--;
            return cut <= 0 ? ellipsis : text[..cut] + ellipsis;
        }

        public TextBlock()
        {
            var theme = PortableTheme.ResolveCurrent();

            IsFocusable = false; // plain labels must not steal focus; TextBox re-enables this
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
            if (IsHeading)
            {
                if (TextSize == oldTheme.Typography.HeadingSize)
                    TextSize = newTheme.Typography.HeadingSize;
            }
            else if (TextSize == oldTheme.TextSize)
            {
                TextSize = newTheme.TextSize;
            }

            var font = TryResolveThemeFont(newTheme);
            if (_fontOverride == null && font != null && !ReferenceEquals(Font, font))
            {
                Font = font;
                _textMeasurer = new SpriteFontTextMeasurer(Font);
                MeasuredText = MeasureText(Text);
                InvalidateLayout(true);
            }
            // Moving into a theme (surface, island) with another dynamic font re-measures the text.
            if (_dynamicFont == null && !ReferenceEquals(oldTheme.Typography?.DynamicFont, newTheme.Typography?.DynamicFont))
                OnTextScaleChanged();
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

        /// <summary>Width of <paramref name="text"/> in this block's resolved font and size.</summary>
        internal float MeasureTextWidth(string? text) => MeasureText(text ?? "").X;

        protected Vector2 MeasureText(string text)
        {
            if (ActiveDynamicFont is { } dynamicFont)
                return dynamicFont.MeasureString(text ?? "", DynamicPixelSize(dynamicFont));
            if (Font != null)
                return Font.MeasureString(text ?? "") * FontScale;
            return TextMeasurer.MeasureString(text ?? "") * TextScaling.Factor;
        }

        /// <summary>Re-measures after <see cref="TextScaling.Factor"/> changed (called by the screen).</summary>
        internal virtual void OnTextScaleChanged()
        {
            _wrappedLines = null;
            _trimCacheText = null;
            MeasuredText = MeasureText(Text);
            InvalidateLayout(true);
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            if (!HasDrawableFont)
                return;

            if (TextWrapping == TextWrapping.Wrap)
            {
                DrawWrapped(spriteBatch, rect);
                return;
            }

            var renderText = Text;
            var measured = MeasuredText;
            if (TextTrimming == TextTrimming.Ellipsis && RenderScale.X > 0)
                GetTrimmedText(rect.Width / RenderScale.X, out renderText, out measured);

            var offset = rect.Offset;
            var measuredText = new Vector2(measured.X * RenderScale.X, measured.Y * RenderScale.Y);
            offset.Y += (rect.Height - measuredText.Y) / 2;
            offset.X += AlignmentOffsetX(rect.Width, measuredText.X);
            DrawTextRun(spriteBatch, renderText, offset);
        }

        private void DrawWrapped(SpriteBatch spriteBatch, Rect rect)
        {
            if (RenderScale.X <= 0 || RenderScale.Y <= 0)
                return;

            var lines = GetWrappedLines(rect.Width / RenderScale.X);
            var lineHeight = LineHeight * RenderScale.Y;
            var totalHeight = lines.Count * lineHeight;
            var top = rect.Top + (rect.Height - totalHeight) / 2;

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var lineWidth = _wrappedLineWidths[i] * RenderScale.X;
                var offset = new PointF(rect.Left + AlignmentOffsetX(rect.Width, lineWidth), top);
                DrawTextRun(spriteBatch, line, offset);
                top += lineHeight;
            }
        }

        private float AlignmentOffsetX(float availableWidth, float textWidth)
        {
            switch (TextAlignment)
            {
                case TextAlignment.Left:
                    return 0;
                case TextAlignment.Center:
                    return (availableWidth - textWidth) / 2;
                case TextAlignment.Right:
                    return availableWidth - textWidth;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DrawTextRun(SpriteBatch spriteBatch, string text, PointF offset)
        {
            if (!HasDrawableFont || text.Length == 0)
                return;

            if (SnapToPixel)
                offset = offset.ToInts();

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
                    DrawText(spriteBatch, text, pos, shadow);
                }
            }

            DrawText(spriteBatch, text, offset, Brush.ApplyOpacity(TextColor, RenderOpacity));
        }
    }
}
