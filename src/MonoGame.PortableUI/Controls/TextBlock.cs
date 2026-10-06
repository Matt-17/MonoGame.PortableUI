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
        private float _textSize;
        private Color _textColor;
        // True while TextSize/TextColor hold the theme's value (seeded, not set by the app): only
        // those follow theme switches, so an explicit value equal to the old default survives.
        private bool _textSizeFromTheme = true;
        private bool _textColorFromTheme = true;
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

        /// <summary>Text colour, straight alpha (<c>Color.White.WithAlpha(0.6f)</c>, not <c>Color.White * 0.6f</c>;
        /// see <see cref="ColorAlpha"/>). Follows the theme until set explicitly.</summary>
        public Color TextColor
        {
            get { return _textColor; }
            set
            {
                _textColorFromTheme = false;
                ApplyTextColor(value);
            }
        }

        private void ApplyTextColor(Color value)
        {
            if (_textColor == value)
                return;
            _textColor = value;
            InvalidateLayout(false);
        }

        /// <summary>Sets <see cref="TextColor"/> to a theme value, keeping it theme-driven so the
        /// next theme switch replaces it with <see cref="GetThemeTextColor"/> of the new theme.</summary>
        protected void SeedThemeTextColor(Color value)
        {
            _textColorFromTheme = true;
            ApplyTextColor(value);
        }

        /// <summary>The theme's text colour for this kind of block (<see cref="PortableTheme.TextColor"/>).</summary>
        protected virtual Color GetThemeTextColor(PortableTheme theme) => theme.TextColor;
        public Vector2 MeasuredText { get; private set; }

        /// <summary>Soft drop-shadow colour (straight alpha); fully transparent (the default) disables the shadow.</summary>
        public Color ShadowColor { get; set; } = Color.Transparent;

        /// <summary>Offset of the drop shadow from the text, in design pixels.</summary>
        public Vector2 ShadowOffset { get; set; } = new Vector2(0, 3);

        /// <summary>Extra soft-spread radius; the shadow is stamped around the offset to blur it.
        /// 0 draws one sharp copy (a translucent colour keeps its alpha).</summary>
        public float ShadowBlur { get; set; } = 2f;

        private Brush? _textFill;
        private Vector2 _textFillSpan = new Vector2(0, 1);

        /// <summary>
        ///     Fills the glyphs with a brush instead of <see cref="TextColor"/>: a <see cref="LinearGradientBrush"/>
        ///     runs across each line (vertical or horizontal; diagonal angles use the nearer axis, hard steps stay
        ///     sharp), a <see cref="SolidColorBrush"/> is a plain colour; other brushes fall back to TextColor.
        ///     Works with every font. Drawn in up to 64 bands, one batch each - meant for titles and logos, not
        ///     for long body text. Outline, glow and shadow keep their own colours.
        /// </summary>
        public Brush? TextFill
        {
            get => _textFill;
            set
            {
                if (ReferenceEquals(_textFill, value))
                    return;
                _textFill = value;
                InvalidateLayout(false);
            }
        }

        /// <summary>
        ///     Where a gradient <see cref="TextFill"/> runs, as fractions of the line box (vertical) or of the
        ///     text width (horizontal): (0, 1) the whole line; e.g. (0.25, 0.8) roughly the cap height, so the
        ///     gradient's stops land on the letters. Outside it the end colours continue.
        /// </summary>
        public Vector2 TextFillSpan
        {
            get => _textFillSpan;
            set
            {
                if (_textFillSpan == value)
                    return;
                _textFillSpan = value;
                InvalidateLayout(false);
            }
        }

        private TextStroke? _stroke;
        private bool _strokeSet;
        private TextGlow? _glow;
        private bool _glowSet;

        /// <summary>
        ///     Round outline around the glyphs (colour, width). Unset, it follows the theme's
        ///     <see cref="Typography.TextStroke"/>; set null to switch a theme outline off for this block.
        ///     The outline draws outside the layout box (see <see cref="Control.InkOverflow"/>).
        /// </summary>
        public TextStroke? Stroke
        {
            get => _strokeSet ? _stroke : ResolveTheme().Typography?.TextStroke;
            set
            {
                _stroke = value;
                _strokeSet = true;
                InvalidateLayout(false);
            }
        }

        /// <summary>
        ///     Soft outer glow behind the text (colour, radius). Unset, it follows the theme's
        ///     <see cref="Typography.TextGlow"/>; set null to switch a theme glow off for this block.
        /// </summary>
        public TextGlow? Glow
        {
            get => _glowSet ? _glow : ResolveTheme().Typography?.TextGlow;
            set
            {
                _glow = value;
                _glowSet = true;
                InvalidateLayout(false);
            }
        }

        /// <summary>Ink outside the layout box: the font's own overflow, then outline, glow and shadow around it.</summary>
        protected internal override Thickness GetInkOverflow()
        {
            var own = base.GetInkOverflow();
            if (_text.Length == 0)
                return own;

            var font = ActiveDynamicFont is { } dynamicFont ? dynamicFont.GetInkOverflow(DynamicPixelSize(dynamicFont)) : default;
            var around = 0f;
            var stroke = Stroke;
            if (stroke != null && stroke.Width > 0 && stroke.Color.A > 0)
                around = stroke.Width;
            var glow = Glow;
            if (glow != null && glow.Radius > 0 && glow.Color.A > 0)
                around += glow.Radius;
            float left = around, top = around, right = around, bottom = around;
            if (ShadowColor.A > 0)
            {
                var blur = MathHelper.Clamp(ShadowBlur, 0f, 6f);
                left = Math.Max(left, blur - ShadowOffset.X);
                right = Math.Max(right, blur + ShadowOffset.X);
                top = Math.Max(top, blur - ShadowOffset.Y);
                bottom = Math.Max(bottom, blur + ShadowOffset.Y);
            }

            return new Thickness(
                Math.Max(own.Left, font.Left + left), Math.Max(own.Top, font.Top + top),
                Math.Max(own.Right, font.Right + right), Math.Max(own.Bottom, font.Bottom + bottom));
        }

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
                _textSizeFromTheme = true;
                ApplyTextSize(ThemeTextSize(ResolveTheme()));
            }
        }

        /// <summary>Text size in design pixels. Follows the theme (<see cref="Typography.TextSize"/> or
        /// <see cref="Typography.HeadingSize"/>) until set explicitly.</summary>
        public float TextSize
        {
            get { return _textSize; }
            set
            {
                _textSizeFromTheme = false;
                ApplyTextSize(value);
            }
        }

        private void ApplyTextSize(float value)
        {
            if (_textSize == value)
                return;
            _textSize = value;
            MeasuredText = MeasureText(Text);
            InvalidateLayout(true);
        }

        private float ThemeTextSize(PortableTheme theme)
            => _isHeading ? theme.Typography.HeadingSize : theme.TextSize;

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
            ApplyTextColor(theme.TextColor);
            ApplyTextSize(theme.TextSize);
            TextAlignment = TextAlignment.Left;
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

            if (_textColorFromTheme)
                ApplyTextColor(GetThemeTextColor(newTheme));
            if (_textSizeFromTheme)
                ApplyTextSize(ThemeTextSize(newTheme));

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

            var glow = Glow;
            if (glow != null && glow.Color.A > 0 && glow.Radius > 0)
                DrawGlow(spriteBatch, text, offset, glow);

            if (ShadowColor.A > 0)
                DrawShadow(spriteBatch, text, offset);

            var stroke = Stroke;
            if (stroke != null && stroke.Color.A > 0 && stroke.Width > 0)
                DrawStroke(spriteBatch, text, offset, stroke);

            if (_textFill is LinearGradientBrush gradient && gradient.Stops.Count > 1)
                DrawGradientFill(spriteBatch, text, offset, gradient);
            else
                DrawText(spriteBatch, text, offset, Brush.ApplyOpacity(_textFill is SolidColorBrush solid ? solid.Color : TextColor, RenderOpacity));
        }

        private const int MaxFillBands = 64;

        // The fill drawn once per band: each band is a scissor slice across the gradient axis, drawn in the
        // gradient's colour at its centre (one render pixel per band up to 64 pixels, so steps stay sharp).
        private void DrawGradientFill(SpriteBatch spriteBatch, string text, Vector2 offset, LinearGradientBrush gradient)
        {
            var radians = MathHelper.ToRadians(gradient.AngleDegrees);
            var direction = new Vector2(MathF.Cos(radians), MathF.Sin(radians));
            var vertical = Math.Abs(direction.Y) >= Math.Abs(direction.X);
            var reversed = vertical ? direction.Y < 0 : direction.X < 0;
            float boxStart, boxLength;
            if (vertical)
            {
                var lineHeight = LineHeight * RenderScale.Y;
                boxStart = offset.Y + lineHeight * _textFillSpan.X;
                boxLength = lineHeight * (_textFillSpan.Y - _textFillSpan.X);
            }
            else
            {
                var width = MeasureText(text).X * RenderScale.X;
                boxStart = offset.X + width * _textFillSpan.X;
                boxLength = width * (_textFillSpan.Y - _textFillSpan.X);
            }

            var device = spriteBatch.GraphicsDevice;
            var clip = device.ScissorRectangle;
            var clipStart = vertical ? clip.Top : clip.Left;
            var clipEnd = vertical ? clip.Bottom : clip.Right;
            var bands = Math.Clamp((int)MathF.Ceiling(Math.Abs(boxLength)), 1, MaxFillBands);
            // Draws the stroke/shadow queued so far before the scissor changes.
            spriteBatch.End();
            for (var band = -1; band <= bands; band++)
            {
                if (!FillBand(band, bands, boxStart, boxLength, clipStart, clipEnd, out var low, out var high, out var t))
                    continue;
                device.ScissorRectangle = vertical
                    ? new Rectangle(clip.X, low, clip.Width, high - low)
                    : new Rectangle(low, clip.Y, high - low, clip.Height);
                Screen.ResumeControlBatch(spriteBatch);
                DrawText(spriteBatch, text, offset, Brush.ApplyOpacity(gradient.ColorAt(reversed ? 1 - t : t), RenderOpacity));
                spriteBatch.End();
            }
            device.ScissorRectangle = clip;
            Screen.ResumeControlBatch(spriteBatch);
        }

        /// <summary>
        ///     Pixel range [<paramref name="low"/>, <paramref name="high"/>) of fill band <paramref name="band"/> of
        ///     <paramref name="bands"/> along the gradient axis, clipped to the control's scissor, and the
        ///     gradient offset drawn there. Band -1 is everything before the span (offset 0), band
        ///     <paramref name="bands"/> everything after it (offset 1). False when the band is empty.
        /// </summary>
        internal static bool FillBand(int band, int bands, float boxStart, float boxLength, int clipStart, int clipEnd,
            out int low, out int high, out float t)
        {
            low = band < 0 ? clipStart : (int)MathF.Round(boxStart + boxLength * band / bands);
            high = band >= bands ? clipEnd : (int)MathF.Round(boxStart + boxLength * (band + 1) / bands);
            t = band < 0 ? 0f : band >= bands ? 1f : (band + 0.5f) / bands;
            low = Math.Max(low, clipStart);
            high = Math.Min(high, clipEnd);
            return high > low;
        }

        private void DrawShadow(SpriteBatch spriteBatch, string text, Vector2 offset)
        {
            var shadow = Brush.ApplyOpacity(ShadowColor, RenderOpacity);
            var blur = MathHelper.Clamp(ShadowBlur, 0f, 6f);
            if (blur < 0.01f)
            {
                // One sharp copy: nine stamps on one spot would turn a translucent shadow opaque.
                var at = offset + ShadowOffset * RenderScale;
                DrawText(spriteBatch, text, SnapToPixel ? at.ToInts() : at, shadow);
                return;
            }

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

        // Stamps the glyphs on rings every 1.5 render pixels from the outline width inwards, with a stamp
        // every Quality pixels of circumference, so the edge stays round at any scale (no shader needed).
        private void DrawStroke(SpriteBatch spriteBatch, string text, Vector2 offset, TextStroke stroke)
        {
            var color = Brush.ApplyOpacity(stroke.Color, RenderOpacity);
            var width = stroke.Width * Math.Max(RenderScale.X, RenderScale.Y);
            var spacing = Math.Max(0.5f, stroke.Quality);
            for (var radius = width; radius > 0.01f; radius -= 1.5f)
            {
                var count = Math.Max(8, (int)MathF.Ceiling(MathHelper.TwoPi * radius / spacing));
                for (var i = 0; i < count; i++)
                {
                    var angle = MathHelper.TwoPi * i / count;
                    DrawText(spriteBatch, text, offset + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius), color);
                }
            }
        }

        private const int GlowRings = 3;
        private const int GlowStampsPerRing = 8;

        // Soft copies on three rings up to the radius; each stamp's alpha is chosen so the full overlap at
        // the glyphs reaches the glow colour's alpha, and the edge fades where fewer copies overlap.
        private void DrawGlow(SpriteBatch spriteBatch, string text, Vector2 offset, TextGlow glow)
        {
            var strength = Math.Min(glow.Color.A / 255f, 0.999f);
            var perStamp = 1f - MathF.Pow(1f - strength, 1f / (GlowRings * GlowStampsPerRing));
            var color = Brush.ApplyOpacity(glow.Color.WithAlpha(perStamp), RenderOpacity);
            var stroke = Stroke;
            var inner = stroke != null && stroke.Color.A > 0 ? Math.Max(0f, stroke.Width) : 0f;
            var scale = Math.Max(RenderScale.X, RenderScale.Y);
            for (var ring = 1; ring <= GlowRings; ring++)
            {
                var radius = (inner + glow.Radius * ring / GlowRings) * scale;
                // Rings are rotated against each other so the copies do not line up into spokes.
                var phase = ring * MathHelper.Pi / (GlowStampsPerRing * GlowRings);
                for (var i = 0; i < GlowStampsPerRing; i++)
                {
                    var angle = phase + MathHelper.TwoPi * i / GlowStampsPerRing;
                    DrawText(spriteBatch, text, offset + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius), color);
                }
            }
        }
    }
}
