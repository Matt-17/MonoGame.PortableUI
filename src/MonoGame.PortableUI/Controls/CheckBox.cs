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
            BoxSize = 20;
            BoxSpacing = 8;
            BoxBorderWidth = 2;
            BoxBackgroundBrush = Color.White;
            BoxBorderBrush = new Color(82, 101, 111);
            CheckMarkBrush = new Color(20, 126, 133);
            TextColor = Color.Black;
            ShowFocusVisual = true;
            Click += CheckBoxClick;
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
        public Brush? BoxBackgroundBrush { get; set; }
        public Brush? BoxBorderBrush { get; set; }
        public Brush? CheckMarkBrush { get; set; }

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
            BoxBackgroundBrush?.Draw(spriteBatch, box);

            if (IsChecked && CheckMarkBrush != null)
                DrawCheckMark(spriteBatch, box, BoxBorderWidth, CheckMarkBrush);

            if (BoxBorderBrush != null && BoxBorderWidth > 0)
                DrawBorder(spriteBatch, box, BoxBorderWidth, BoxBorderBrush);
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

        private static void DrawBorder(SpriteBatch spriteBatch, Rect rect, float width, Brush brush)
        {
            brush.Draw(spriteBatch, new Rect(rect.Left, rect.Top, rect.Width, width));
            brush.Draw(spriteBatch, new Rect(rect.Left, rect.Top, width, rect.Height));
            brush.Draw(spriteBatch, new Rect(rect.Right - width, rect.Top, width, rect.Height));
            brush.Draw(spriteBatch, new Rect(rect.Left, rect.Bottom - width, rect.Width, width));
        }

        private static void DrawCheckMark(SpriteBatch spriteBatch, Rect rect, float borderWidth, Brush brush)
        {
            foreach (var markRect in GetCheckMarkRects(rect, borderWidth))
                brush.Draw(spriteBatch, markRect);
        }

        internal static IEnumerable<Rect> GetCheckMarkRects(Rect rect, float borderWidth)
        {
            var mark = rect;
            if (mark.Width <= 0 || mark.Height <= 0)
                yield break;

            var stroke = Math.Max(2, Math.Min(mark.Width, mark.Height) * 0.18f);
            stroke = Math.Min(stroke, Math.Min(mark.Width, mark.Height));
            var halfStroke = stroke / 2;
            var steps = Math.Max(2, (int)Math.Ceiling(Math.Max(mark.Width, mark.Height)) + 1);

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
