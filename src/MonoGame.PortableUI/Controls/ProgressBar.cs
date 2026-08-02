using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class ProgressBar : Control
    {
        private float _minimum;
        private float _maximum = 100;
        private float _value;

        public ProgressBar()
        {
            var theme = PortableTheme.ResolveCurrent();

            Height = theme.ProgressBarHeight;
            Width = theme.ProgressBarWidth;
            FillBrush = theme.ProgressBarFillBrush;
        }

        protected override Brush? GetThemeBackgroundBrush(PortableTheme theme)
        {
            return theme.ProgressBarBackgroundBrush;
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

            if (Height.Equals(oldTheme.ProgressBarHeight))
                Height = newTheme.ProgressBarHeight;
            if (Width.Equals(oldTheme.ProgressBarWidth))
                Width = newTheme.ProgressBarWidth;
            if (ReferenceEquals(FillBrush, oldTheme.ProgressBarFillBrush))
                FillBrush = newTheme.ProgressBarFillBrush;
        }

        public float Minimum
        {
            get { return _minimum; }
            set
            {
                _minimum = value;
                if (_maximum < _minimum)
                    _maximum = _minimum;
                Value = _value;
            }
        }

        public float Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = Math.Max(Minimum, value);
                Value = _value;
            }
        }

        public float Value
        {
            get { return _value; }
            set
            {
                var clamped = MathHelper.Clamp(value, Minimum, Maximum);
                if (Math.Abs(_value - clamped) < 0.0001f)
                    return;

                _value = clamped;
                InvalidateLayout(false);
            }
        }

        public Brush FillBrush { get; set; }

        /// <summary>When true, ignores Value and shows a sweeping marquee block instead.</summary>
        public bool IsIndeterminate { get; set; }

        /// <summary>Duration of one marquee sweep in indeterminate mode.</summary>
        public TimeSpan IndeterminateCycle { get; set; } = TimeSpan.FromSeconds(1.6);

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            var width = Width.IsFixed() ? Width : PortableTheme.ResolveCurrent().ProgressBarWidth;
            var height = Height.IsFixed() ? Height : PortableTheme.ResolveCurrent().ProgressBarHeight;
            return ApplyConstraints(new Size(width, height) + Margin);
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            if (IsIndeterminate)
            {
                DrawIndeterminate(spriteBatch, rect);
                return;
            }

            var fillRect = GetFillRect(rect);
            if (fillRect.Width > 0 && fillRect.Height > 0)
                FillBrush.Draw(spriteBatch, fillRect, RenderOpacity);
        }

        private void DrawIndeterminate(SpriteBatch spriteBatch, Rect rect)
        {
            var cycle = Math.Max(0.1, IndeterminateCycle.TotalSeconds);
            var t = (float)(ScreenSystem.TotalTime.TotalSeconds % cycle / cycle);
            var blockWidth = rect.Width * 0.3f;
            var left = rect.Left - blockWidth + (rect.Width + blockWidth) * t;
            var visibleLeft = Math.Max(left, rect.Left);
            var visibleRight = Math.Min(left + blockWidth, rect.Right);
            if (visibleRight > visibleLeft)
                FillBrush.Draw(spriteBatch, new Rect(visibleLeft, rect.Top, visibleRight - visibleLeft, rect.Height), RenderOpacity);
        }

        internal Rect GetFillRect(Rect rect)
        {
            var range = Maximum - Minimum;
            var percent = range <= 0 ? 0 : MathHelper.Clamp((Value - Minimum) / range, 0, 1);
            return new Rect(rect.Left, rect.Top, rect.Width * percent, rect.Height);
        }
    }
}
