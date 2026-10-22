using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class CheckBox : ContentControl
    {
        protected override bool UsesThemeMinimumSize => true;

        private bool _isChecked;
        private Color _textColor;

        public CheckBox()
        {
            var theme = PortableTheme.ResolveCurrent();

            BoxSize = theme.CheckBoxBoxSize;
            BoxSpacing = theme.CheckBoxBoxSpacing;
            BoxBorderWidth = theme.CheckBoxBoxBorderWidth;
            BoxBackgroundBrush = theme.CheckBoxBoxBackgroundBrush;
            BoxBorderBrush = theme.CheckBoxBoxBorderBrush;
            CheckMarkBrush = theme.CheckBoxCheckMarkBrush;
            GlyphKind = theme.CheckBoxGlyphKind;
            BoxCornerRadius = theme.CheckBoxBoxCornerRadius;
            CheckedBoxBackgroundBrush = theme.CheckBoxCheckedBoxBackgroundBrush;
            CheckedBoxBorderBrush = theme.CheckBoxCheckedBoxBorderBrush;
            DisabledBoxBrush = theme.CheckBoxDisabledBoxBrush;
            TextColor = theme.CheckBoxTextColor;
            ShowFocusVisual = true;
            Click += CheckBoxClick;
            ActivatesOnKeys = true;
        }

        protected override Audio.UiSound? GetClickSound() => IsChecked ? Audio.UiSound.ToggleOn : Audio.UiSound.ToggleOff;

        protected override Audio.UiSound? GetPressSound() => IsChecked ? Audio.UiSound.ToggleOff : Audio.UiSound.ToggleOn;

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

            if (BoxSize.Equals(oldTheme.CheckBoxBoxSize))
                BoxSize = newTheme.CheckBoxBoxSize;
            if (BoxSpacing.Equals(oldTheme.CheckBoxBoxSpacing))
                BoxSpacing = newTheme.CheckBoxBoxSpacing;
            if (BoxBorderWidth.Equals(oldTheme.CheckBoxBoxBorderWidth))
                BoxBorderWidth = newTheme.CheckBoxBoxBorderWidth;
            if (ReferenceEquals(BoxBackgroundBrush, oldTheme.CheckBoxBoxBackgroundBrush))
                BoxBackgroundBrush = newTheme.CheckBoxBoxBackgroundBrush;
            if (ReferenceEquals(BoxBorderBrush, oldTheme.CheckBoxBoxBorderBrush))
                BoxBorderBrush = newTheme.CheckBoxBoxBorderBrush;
            if (ReferenceEquals(CheckMarkBrush, oldTheme.CheckBoxCheckMarkBrush))
                CheckMarkBrush = newTheme.CheckBoxCheckMarkBrush;
            if (GlyphKind == oldTheme.CheckBoxGlyphKind)
                GlyphKind = newTheme.CheckBoxGlyphKind;
            if (BoxCornerRadius.Equals(oldTheme.CheckBoxBoxCornerRadius))
                BoxCornerRadius = newTheme.CheckBoxBoxCornerRadius;
            if (ReferenceEquals(CheckedBoxBackgroundBrush, oldTheme.CheckBoxCheckedBoxBackgroundBrush))
                CheckedBoxBackgroundBrush = newTheme.CheckBoxCheckedBoxBackgroundBrush;
            if (ReferenceEquals(CheckedBoxBorderBrush, oldTheme.CheckBoxCheckedBoxBorderBrush))
                CheckedBoxBorderBrush = newTheme.CheckBoxCheckedBoxBorderBrush;
            if (ReferenceEquals(DisabledBoxBrush, oldTheme.CheckBoxDisabledBoxBrush))
                DisabledBoxBrush = newTheme.CheckBoxDisabledBoxBrush;
            if (TextColor.Equals(oldTheme.CheckBoxTextColor))
                TextColor = newTheme.CheckBoxTextColor;
        }

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked == value)
                    return;

                _isChecked = value;
                InvalidateLayout(false);
                OnChecked(_isChecked);
            }
        }

        public string Text
        {
            get
            {
                var textBlock = Content as TextBlock;
                return textBlock?.Text ?? "";
            }
            set
            {
                var textBlock = Content as TextBlock;
                if (textBlock == null && Content == null)
                {
                    textBlock = new TextBlock();
                    Content = textBlock;
                    ChangeVisualState();
                }

                if (textBlock != null && textBlock.Text != value)
                    textBlock.Text = value;
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
                ChangeVisualState();
                InvalidateLayout(false);
            }
        }

        public float BoxSize { get; set; }
        public float BoxSpacing { get; set; }
        public float BoxBorderWidth { get; set; }

        /// <summary>Corner radius of the check box chrome (0 = square, matching earlier versions).</summary>
        public CornerRadius BoxCornerRadius { get; set; }
        public Brush? BoxBackgroundBrush { get; set; }
        public Brush? BoxBorderBrush { get; set; }
        public Brush? CheckMarkBrush { get; set; }

        /// <summary>Box fill while checked; null keeps <see cref="BoxBackgroundBrush"/>.</summary>
        public Brush? CheckedBoxBackgroundBrush { get; set; }

        /// <summary>Box border while checked; null keeps <see cref="BoxBorderBrush"/>.</summary>
        public Brush? CheckedBoxBorderBrush { get; set; }

        /// <summary>Box border (and fill while checked) when disabled; null keeps the enabled brushes.</summary>
        public Brush? DisabledBoxBrush { get; set; }
        public CheckBoxGlyphKind GlyphKind { get; set; }

        public event EventHandler<CheckedEventArgs>? Checked;

        /// <summary>
        ///     Ripple around the box while pressed; null uses the theme's <see cref="PortableTheme.ToggleRipple"/>
        ///     (off by default).
        /// </summary>
        public RippleStyle? Ripple { get; set; }

        private RippleStyle? EffectiveRipple => Ripple ?? ResolveTheme().ToggleRipple;

        private readonly RippleEffect _ripple = new RippleEffect();

        internal override void OnFrameUpdate()
        {
            base.OnFrameUpdate();
            if (_ripple.Track(IsPointerPressedVisual, ClippingRect, LastPressPosition) && EffectiveRipple != null)
                RequestRedraw();
        }

        protected internal override Thickness GetInkOverflow() => RippleEffect.Overflow(base.GetInkOverflow(), EffectiveRipple);

        private void DrawRipple(SpriteBatch spriteBatch, Rect around, bool on)
        {
            if (EffectiveRipple is not { } ripple)
                return;
            var center = new Vector2(around.Left + around.Width / 2, around.Top + around.Height / 2);
            _ripple.DrawUnbounded(spriteBatch, center, ToRender(ripple.Radius), ripple, on ? ripple.CheckedColor ?? ripple.Color : ripple.Color, RenderOpacity);
        }

        public override Size MeasureLayout()
        {
            if (IsGoneShown)
                return Size.Empty;

            var contentSize = Content?.Measure() ?? Size.Empty;
            var hasContent = Content != null;
            // A fixed Width/Height is final (like ContentControl); padding only grows auto axes.
            var width = Width.IsFixed()
                ? Width
                : BoxSize + (hasContent ? BoxSpacing + contentSize.Width : 0) + Padding.Horizontal;
            var height = Height.IsFixed()
                ? Height
                : Math.Max(BoxSize, contentSize.Height) + Padding.Vertical;

            return ApplyConstraints(new Size(width, height)) + Margin;
        }

        public override void UpdateLayout(Rect rect)
        {
            if (IsGoneShown)
            {
                BoundingRect = Rect.Empty;
                return;
            }

            base.UpdateLayout(rect);

            if (Content == null)
                return;

            var contentRect = BoundingRect - Margin - Padding;
            var contentLeft = contentRect.Left + BoxSize + BoxSpacing;
            Content.UpdateLayout(new Rect(
                contentLeft,
                contentRect.Top,
                Math.Max(0, contentRect.Right - contentLeft),
                contentRect.Height));
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);

            var box = GetBoxRect(rect - ToRender(Padding), ToRender(BoxSize), centered: Content == null);
            var boxRadius = ToRender(BoxCornerRadius);
            var boxBorder = ToRender(BoxBorderWidth);
            DrawRipple(spriteBatch, box, IsChecked);
            var disabled = !IsEnabled && DisabledBoxBrush != null;
            var background = IsChecked ? (disabled ? DisabledBoxBrush : CheckedBoxBackgroundBrush ?? BoxBackgroundBrush) : BoxBackgroundBrush;
            var border = disabled ? DisabledBoxBrush : IsChecked ? CheckedBoxBorderBrush ?? BoxBorderBrush : BoxBorderBrush;
            if (BoxCornerRadius.IsEmpty)
                background?.Draw(spriteBatch, box, RenderOpacity);
            else
                background?.Draw(spriteBatch, new BrushContext(box, boxRadius, RenderOpacity, spriteBatch.GraphicsDevice));

            if (border != null && BoxBorderWidth > 0)
            {
                // Same convention as Control's chrome: rounded borders need a solid color; other
                // brushes fall back to the square border.
                if (!BoxCornerRadius.IsEmpty && border is SolidColorBrush solidBorder)
                    RoundedRectRenderer.DrawBorder(spriteBatch, box, boxRadius, new Thickness(boxBorder), Brush.ApplyOpacity(solidBorder.Color, RenderOpacity));
                else
                    BorderRenderer.Draw(spriteBatch, box, boxBorder, border, RenderOpacity);
            }

            if (IsChecked && CheckMarkBrush != null)
            {
                if (GlyphKind == CheckBoxGlyphKind.SmoothCheck && CheckMarkBrush is SolidColorBrush solidMark)
                    DrawSmoothCheck(spriteBatch, box, solidMark.Color, RenderOpacity);
                else
                    DrawCheckMark(spriteBatch, box, boxBorder, CheckMarkBrush, GlyphKind == CheckBoxGlyphKind.SmoothCheck ? CheckBoxGlyphKind.Check : GlyphKind, RenderOpacity);
            }
        }

        internal override void ChangeVisualState()
        {
            var textBlock = Content as TextBlock;
            if (textBlock != null)
                textBlock.SetOwnerTextColor(TextColor);
        }

        private void CheckBoxClick(object? sender, EventArgs e)
        {
            IsChecked = !IsChecked;
        }

        protected virtual void OnChecked(bool isChecked)
        {
            Checked?.Invoke(this, new CheckedEventArgs { IsChecked = isChecked });
        }

        private Rect GetBoxRect(Rect contentRect) => GetBoxRect(contentRect, BoxSize, centered: Content == null);

        // Vertically centered; without content (a label beside it) also horizontally, e.g. in a 44 dp touch target.
        private static Rect GetBoxRect(Rect contentRect, float boxSize, bool centered)
        {
            return new Rect(
                centered ? contentRect.Left + Math.Max(0, (contentRect.Width - boxSize) / 2) : contentRect.Left,
                contentRect.Top + Math.Max(0, (contentRect.Height - boxSize) / 2),
                boxSize,
                boxSize);
        }

        // The check of Material's check_box icon, in the coordinates of its 18 unit box, stretched over the box.
        private static readonly PathGeometry SmoothCheckGeometry = new PathGeometry(18, 18)
            .MoveTo(7, 14).LineTo(2, 9).LineTo(3.41f, 7.59f).LineTo(7, 11.17f).LineTo(14.59f, 3.58f).LineTo(16, 5).Close();

        // One anti-aliased texture per pixel size and color, shared by all check boxes of a device.
        private static readonly Dictionary<(int Size, uint Color), Texture2D> SmoothChecks = new Dictionary<(int Size, uint Color), Texture2D>();
        private static GraphicsDevice? _smoothCheckDevice;
        private static int _smoothCheckReset = -1;

        private static void DrawSmoothCheck(SpriteBatch spriteBatch, Rect box, Color color, float opacity)
        {
            var device = spriteBatch.GraphicsDevice;
            var size = (int)Math.Round(Math.Min(box.Width, box.Height));
            if (size <= 0)
                return;
            var reset = PathRasterizer.GetResetGeneration(device);
            if (!ReferenceEquals(device, _smoothCheckDevice) || reset != _smoothCheckReset)
            {
                foreach (var stale in SmoothChecks.Values)
                    stale.Dispose();
                SmoothChecks.Clear();
                _smoothCheckDevice = device;
                _smoothCheckReset = reset;
            }
            if (!SmoothChecks.TryGetValue((size, color.PackedValue), out var texture) || texture.IsDisposed)
            {
                texture = PathRasterizer.CreateTexture(device, SmoothCheckGeometry, size, size, color);
                SmoothChecks[(size, color.PackedValue)] = texture;
            }
            var at = new Vector2(MathF.Round(box.Left + (box.Width - size) / 2), MathF.Round(box.Top + (box.Height - size) / 2));
            spriteBatch.Draw(texture, at, Color.White * opacity);
        }

        private static void DrawCheckMark(SpriteBatch spriteBatch, Rect rect, float borderWidth, Brush brush, CheckBoxGlyphKind glyphKind, float opacity)
        {
            // Straight to the brush: the rects came from nested iterators before, garbage per checked box per frame.
            var sink = new DrawSink(spriteBatch, brush, opacity);
            EmitCheckMark(rect, glyphKind, ref sink);
        }

        internal static IEnumerable<Rect> GetCheckMarkRects(Rect rect, float borderWidth, CheckBoxGlyphKind glyphKind = CheckBoxGlyphKind.Cross)
        {
            var sink = new ListSink(new List<Rect>());
            EmitCheckMark(rect, glyphKind, ref sink);
            return sink.Rects;
        }

        private interface IRectSink
        {
            void Add(Rect rect);
        }

        private readonly struct DrawSink(SpriteBatch spriteBatch, Brush brush, float opacity) : IRectSink
        {
            public void Add(Rect rect) => brush.Draw(spriteBatch, rect, opacity);
        }

        private readonly struct ListSink(List<Rect> rects) : IRectSink
        {
            public List<Rect> Rects => rects;

            public void Add(Rect rect) => rects.Add(rect);
        }

        // A struct sink through a generic constraint: no boxing, no delegate.
        private static void EmitCheckMark<TSink>(Rect rect, CheckBoxGlyphKind glyphKind, ref TSink sink) where TSink : struct, IRectSink
        {
            var mark = rect;
            if (mark.Width <= 0 || mark.Height <= 0)
                return;

            var stroke = Math.Max(2, Math.Min(mark.Width, mark.Height) * 0.18f);
            stroke = Math.Min(stroke, Math.Min(mark.Width, mark.Height));
            var halfStroke = stroke / 2;
            var steps = Math.Max(2, (int)Math.Ceiling(Math.Max(mark.Width, mark.Height)) + 1);

            if (glyphKind == CheckBoxGlyphKind.Check)
            {
                EmitStrokeOnLine(mark, stroke, new PointF(0.2f, 0.55f), new PointF(0.42f, 0.78f), ref sink);
                EmitStrokeOnLine(mark, stroke, new PointF(0.42f, 0.78f), new PointF(0.82f, 0.25f), ref sink);
                return;
            }

            for (var i = 0; i < steps; i++)
            {
                var t = steps == 1 ? 0 : i / (float)(steps - 1);
                var centerX = mark.Left + mark.Width * t;
                var centerY = mark.Top + mark.Height * t;
                var inverseCenterY = mark.Bottom - mark.Height * t;

                var first = ClipToBounds(new Rect(centerX - halfStroke, centerY - halfStroke, stroke, stroke), mark);
                if (first.Width > 0 && first.Height > 0)
                    sink.Add(first);

                var second = ClipToBounds(new Rect(centerX - halfStroke, inverseCenterY - halfStroke, stroke, stroke), mark);
                if (second.Width > 0 && second.Height > 0)
                    sink.Add(second);
            }
        }

        private static void EmitStrokeOnLine<TSink>(Rect bounds, float stroke, PointF start, PointF end, ref TSink sink) where TSink : struct, IRectSink
        {
            var halfStroke = stroke / 2;
            var steps = Math.Max(2, (int)Math.Ceiling(Math.Max(bounds.Width, bounds.Height)) + 1);
            for (var i = 0; i < steps; i++)
            {
                var t = steps == 1 ? 0 : i / (float)(steps - 1);
                var centerX = bounds.Left + bounds.Width * (start.X + (end.X - start.X) * t);
                var centerY = bounds.Top + bounds.Height * (start.Y + (end.Y - start.Y) * t);
                var rect = ClipToBounds(new Rect(centerX - halfStroke, centerY - halfStroke, stroke, stroke), bounds);
                if (rect.Width > 0 && rect.Height > 0)
                    sink.Add(rect);
            }
        }

        private static Rect ClipToBounds(Rect rect, Rect bounds)
        {
            var left = Math.Max(rect.Left, bounds.Left);
            var top = Math.Max(rect.Top, bounds.Top);
            var right = Math.Min(rect.Right, bounds.Right);
            var bottom = Math.Min(rect.Bottom, bounds.Bottom);
            return new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
        }
    }
}
