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
        private string? _wrapCacheText;
        private float _wrapCacheWidth = -1f;
        private float _wrapCacheScale = -1f;

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

            if (TextWrapping == TextWrapping.Wrap && TryGetWrapMeasureWidth(out var wrapWidth))
            {
                var lines = GetWrappedLines(wrapWidth);
                float maxLineWidth = 0;
                foreach (var line in lines)
                    maxLineWidth = Math.Max(maxLineWidth, MeasureText(line).X);

                var wrappedWidth = Width.IsFixed() ? Width : Math.Min(wrapWidth, maxLineWidth);
                var wrappedHeight = Height.IsFixed() ? Height : lines.Count * LineHeight;
                return ApplyConstraints(new Size(wrappedWidth, wrappedHeight)) + Margin;
            }

            var measuredText = MeasureText(Text);
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

        private float LineHeight => Font != null
            ? Font.LineSpacing * FontScale
            : MeasureText("Ag").Y;

        private IReadOnlyList<string> GetWrappedLines(float availableWidth)
        {
            var scale = FontScale;
            if (_wrappedLines != null &&
                _wrapCacheText == _text &&
                Math.Abs(_wrapCacheWidth - availableWidth) < 0.5f &&
                Math.Abs(_wrapCacheScale - scale) < 0.0001f)
            {
                return _wrappedLines;
            }

            _wrappedLines = WrapText(_text, availableWidth);
            _wrapCacheText = _text;
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
            if (Font == null)
                return;

            if (TextWrapping == TextWrapping.Wrap)
            {
                DrawWrapped(spriteBatch, rect);
                return;
            }

            var renderText = TextTrimming == TextTrimming.Ellipsis && RenderScale.X > 0
                ? TrimWithEllipsis(Text, rect.Width / RenderScale.X)
                : Text;
            var measured = ReferenceEquals(renderText, Text) || renderText == Text
                ? MeasuredText
                : MeasureText(renderText);

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

            foreach (var line in lines)
            {
                var lineWidth = MeasureText(line).X * RenderScale.X;
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
            if (Font == null || text.Length == 0)
                return;

            // MeasuredText already includes FontScale; the draw scale must apply it on top of the
            // control-transform RenderScale so glyphs render at the requested TextSize.
            var drawScale = RenderScale * FontScale;
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
                    spriteBatch.DrawString(Font, text, pos, shadow, 0, Vector2.Zero, drawScale, SpriteEffects.None, 0);
                }
            }

            spriteBatch.DrawString(Font, text, offset, Brush.ApplyOpacity(TextColor, RenderOpacity), 0, Vector2.Zero, drawScale, SpriteEffects.None, 0);
        }
    }
}
