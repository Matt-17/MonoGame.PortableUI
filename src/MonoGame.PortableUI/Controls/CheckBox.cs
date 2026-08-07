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
            TextColor = theme.CheckBoxTextColor;
            ShowFocusVisual = true;
            Click += CheckBoxClick;
            KeyPressed += ActivateOnKeyPressed;
        }

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
        public CheckBoxGlyphKind GlyphKind { get; set; }

        public event EventHandler<CheckedEventArgs>? Checked;

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            var contentSize = Content?.MeasureLayout() ?? Size.Empty;
            var hasContent = Content != null;
            var width = Width.IsFixed()
                ? Width
                : BoxSize + (hasContent ? BoxSpacing + contentSize.Width : 0);
            var height = Height.IsFixed()
                ? Height
                : Math.Max(BoxSize, contentSize.Height);

            return ApplyConstraints(new Size(width, height) + Padding) + Margin;
        }

        public override void UpdateLayout(Rect rect)
        {
            if (IsGone)
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

            var box = GetBoxRect(rect - Padding);
            if (BoxCornerRadius.IsEmpty)
                BoxBackgroundBrush?.Draw(spriteBatch, box);
            else
                BoxBackgroundBrush?.Draw(spriteBatch, new BrushContext(box, BoxCornerRadius, RenderOpacity, spriteBatch.GraphicsDevice));

            if (IsChecked && CheckMarkBrush != null)
                DrawCheckMark(spriteBatch, box, BoxBorderWidth, CheckMarkBrush, GlyphKind);

            if (BoxBorderBrush != null && BoxBorderWidth > 0)
            {
                // Same convention as Control's chrome: rounded borders need a solid color; other
                // brushes fall back to the square border.
                if (!BoxCornerRadius.IsEmpty && BoxBorderBrush is SolidColorBrush solidBorder)
                    RoundedRectRenderer.DrawBorder(spriteBatch, box, BoxCornerRadius, new Thickness(BoxBorderWidth), Brush.ApplyOpacity(solidBorder.Color, RenderOpacity));
                else
                    BorderRenderer.Draw(spriteBatch, box, BoxBorderWidth, BoxBorderBrush);
            }
        }

        internal override void ChangeVisualState()
        {
            var textBlock = Content as TextBlock;
            if (textBlock != null)
                textBlock.TextColor = TextColor;
        }

        private void CheckBoxClick(object? sender, EventArgs e)
        {
            IsChecked = !IsChecked;
        }

        protected virtual void OnChecked(bool isChecked)
        {
            Checked?.Invoke(this, new CheckedEventArgs { IsChecked = isChecked });
        }

        private Rect GetBoxRect(Rect contentRect)
        {
            return new Rect(
                contentRect.Left,
                contentRect.Top + Math.Max(0, (contentRect.Height - BoxSize) / 2),
                BoxSize,
                BoxSize);
        }

        private static void DrawCheckMark(SpriteBatch spriteBatch, Rect rect, float borderWidth, Brush brush, CheckBoxGlyphKind glyphKind)
        {
            foreach (var markRect in GetCheckMarkRects(rect, borderWidth, glyphKind))
                brush.Draw(spriteBatch, markRect);
        }

        internal static IEnumerable<Rect> GetCheckMarkRects(Rect rect, float borderWidth, CheckBoxGlyphKind glyphKind = CheckBoxGlyphKind.Cross)
        {
            var mark = rect;
            if (mark.Width <= 0 || mark.Height <= 0)
                yield break;

            var stroke = Math.Max(2, Math.Min(mark.Width, mark.Height) * 0.18f);
            stroke = Math.Min(stroke, Math.Min(mark.Width, mark.Height));
            var halfStroke = stroke / 2;
            var steps = Math.Max(2, (int)Math.Ceiling(Math.Max(mark.Width, mark.Height)) + 1);

            if (glyphKind == CheckBoxGlyphKind.Check)
            {
                foreach (var rectOnLine in GetStrokeRectsOnLine(mark, stroke, new PointF(0.2f, 0.55f), new PointF(0.42f, 0.78f)))
                    yield return rectOnLine;
                foreach (var rectOnLine in GetStrokeRectsOnLine(mark, stroke, new PointF(0.42f, 0.78f), new PointF(0.82f, 0.25f)))
                    yield return rectOnLine;
                yield break;
            }

            for (var i = 0; i < steps; i++)
            {
                var t = steps == 1 ? 0 : i / (float)(steps - 1);
                var centerX = mark.Left + mark.Width * t;
                var centerY = mark.Top + mark.Height * t;
                var inverseCenterY = mark.Bottom - mark.Height * t;

                var first = ClipToBounds(new Rect(centerX - halfStroke, centerY - halfStroke, stroke, stroke), mark);
                if (first.Width > 0 && first.Height > 0)
                    yield return first;

                var second = ClipToBounds(new Rect(centerX - halfStroke, inverseCenterY - halfStroke, stroke, stroke), mark);
                if (second.Width > 0 && second.Height > 0)
                    yield return second;
            }
        }

        private static IEnumerable<Rect> GetStrokeRectsOnLine(Rect bounds, float stroke, PointF start, PointF end)
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
                    yield return rect;
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
