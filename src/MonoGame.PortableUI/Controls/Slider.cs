using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class Slider : Control
    {
        private float _minimum;
        private float _maximum = 100;
        private float _value;
        private bool _isDragging;

        public Slider()
        {
            var theme = PortableTheme.ResolveCurrent();

            Height = theme.SliderHeight;
            Width = theme.SliderWidth;
            TrackHeight = theme.SliderTrackHeight;
            ThumbSize = theme.SliderThumbSize;
            TrackBrush = theme.SliderTrackBrush;
            FillBrush = theme.SliderFillBrush;
            ThumbBrush = theme.SliderThumbBrush;
            ThumbBorderBrush = theme.SliderThumbBorderBrush;
            ShowFocusVisual = true;
            MouseDown += SliderMouseDown;
            MouseMove += SliderMouseMove;
            MouseUp += SliderMouseUp;
            TouchDown += SliderTouchDown;
            TouchMove += SliderTouchMove;
            TouchUp += SliderTouchUp;
            TouchCancel += SliderTouchCancel;
            KeyPressed += SliderKeyPressed;
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

            if (Height.Equals(oldTheme.SliderHeight))
                Height = newTheme.SliderHeight;
            if (Width.Equals(oldTheme.SliderWidth))
                Width = newTheme.SliderWidth;
            if (TrackHeight.Equals(oldTheme.SliderTrackHeight))
                TrackHeight = newTheme.SliderTrackHeight;
            if (ThumbSize.Equals(oldTheme.SliderThumbSize))
                ThumbSize = newTheme.SliderThumbSize;
            if (ReferenceEquals(TrackBrush, oldTheme.SliderTrackBrush))
                TrackBrush = newTheme.SliderTrackBrush;
            if (ReferenceEquals(FillBrush, oldTheme.SliderFillBrush))
                FillBrush = newTheme.SliderFillBrush;
            if (ReferenceEquals(ThumbBrush, oldTheme.SliderThumbBrush))
                ThumbBrush = newTheme.SliderThumbBrush;
            if (ReferenceEquals(ThumbBorderBrush, oldTheme.SliderThumbBorderBrush))
                ThumbBorderBrush = newTheme.SliderThumbBorderBrush;
        }

        // Horizontal track: Left/Right change the value, Up/Down leave the slider (gamepad-friendly).
        protected internal override bool HandlesDirection(FocusDirection direction)
        {
            return direction is FocusDirection.Left or FocusDirection.Right;
        }

        public float Minimum
        {
            get { return _minimum; }
            set
            {
                if (Math.Abs(_minimum - value) < float.Epsilon)
                    return;

                _minimum = value;
                Value = _value;
                InvalidateLayout(false);
            }
        }

        /// <summary>The requested maximum is kept and coerced on read (WPF style), so setting
        /// Maximum before Minimum gives the same range as the other way round.</summary>
        public float Maximum
        {
            get { return Math.Max(_minimum, _maximum); }
            set
            {
                if (Math.Abs(_maximum - value) < float.Epsilon)
                    return;

                _maximum = value;
                Value = _value;
                InvalidateLayout(false);
            }
        }

        public float Value
        {
            get { return _value; }
            set
            {
                if (float.IsNaN(value))
                    return;
                var clamped = ClampValue(value);
                if (Math.Abs(_value - clamped) < 0.0001f)
                    return;

                var old = _value;
                _value = clamped;
                InvalidateLayout(false);
                ValueChanged?.Invoke(this, new ValueChangedEventArgs { OldValue = old, NewValue = _value });
            }
        }

        public float SmallChange { get; set; } = 1;

        public float LargeChange { get; set; } = 10;

        public float TrackHeight { get; set; }

        public float ThumbSize { get; set; }

        public Brush TrackBrush { get; set; }

        public Brush FillBrush { get; set; }

        public Brush ThumbBrush { get; set; }

        public Brush? ThumbBorderBrush { get; set; }

        public float ThumbBorderWidth { get; set; } = 1;

        public event EventHandler<ValueChangedEventArgs>? ValueChanged;

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            var width = Width.IsFixed() ? Width : PortableTheme.ResolveCurrent().SliderWidth;
            var height = Height.IsFixed() ? Height : PortableTheme.ResolveCurrent().SliderHeight;
            // Min/Max constrain the content box only; margin is added afterwards (same as Control).
            return ApplyConstraints(new Size(width, height)) + Margin;
        }

        /// <summary>Corner radius of track and fill (0 = square, matching earlier versions).</summary>
        public CornerRadius TrackCornerRadius { get; set; }

        /// <summary>Corner radius of the thumb (0 = square; ThumbSize/2 yields a circle).</summary>
        public CornerRadius ThumbCornerRadius { get; set; }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            var scale = Math.Min(RenderScale.X, RenderScale.Y);
            var track = GetTrackRect(rect, scale);
            DrawChrome(spriteBatch, TrackBrush, track, ToRender(TrackCornerRadius));

            var fill = track;
            fill.Width = Math.Max(0, GetThumbCenterX(rect, scale) - track.Left);
            DrawChrome(spriteBatch, FillBrush, fill, ToRender(TrackCornerRadius));

            var thumb = GetThumbRect(rect, scale);
            var thumbRadius = ToRender(ThumbCornerRadius);
            DrawChrome(spriteBatch, ThumbBrush, thumb, thumbRadius);
            if (ThumbBorderBrush != null && ThumbBorderWidth > 0)
            {
                var thumbBorder = ToRender(ThumbBorderWidth);
                if (!ThumbCornerRadius.IsEmpty && ThumbBorderBrush is SolidColorBrush solidBorder)
                    RoundedRectRenderer.DrawBorder(spriteBatch, thumb, thumbRadius, new Thickness(thumbBorder), Brush.ApplyOpacity(solidBorder.Color, RenderOpacity));
                else
                    BorderRenderer.Draw(spriteBatch, thumb, thumbBorder, ThumbBorderBrush, RenderOpacity);
            }
        }

        private void DrawChrome(SpriteBatch spriteBatch, Brush brush, Rect rect, CornerRadius radius)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            if (radius.IsEmpty)
                brush.Draw(spriteBatch, rect, RenderOpacity);
            else
                brush.Draw(spriteBatch, new BrushContext(rect, radius, RenderOpacity, spriteBatch.GraphicsDevice));
        }

        // scale: the render scale when rect is a draw rect (1 for layout space).
        internal Rect GetTrackRect(Rect rect, float scale = 1f)
        {
            var thumbSize = ThumbSize * scale;
            var trackHeight = TrackHeight * scale;
            var trackWidth = Math.Max(0, rect.Width - thumbSize);
            return new Rect(
                rect.Left + thumbSize / 2,
                rect.Top + (rect.Height - trackHeight) / 2,
                trackWidth,
                trackHeight);
        }

        internal Rect GetThumbRect(Rect rect, float scale = 1f)
        {
            var thumbSize = ThumbSize * scale;
            return new Rect(
                GetThumbCenterX(rect, scale) - thumbSize / 2,
                rect.Top + (rect.Height - thumbSize) / 2,
                thumbSize,
                thumbSize);
        }

        private void SliderMouseDown(object? sender, MouseEventArgs args)
        {
            if (!args.Buttons.Contains(MouseButton.Left))
                return;

            _isDragging = true;
            Screen?.CaptureMouse(this);
            SetValueFromPosition(args.Position);
            args.Handled = true;
        }

        private void SliderMouseMove(object? sender, MouseEventArgs args)
        {
            if (!_isDragging)
                return;

            if (!args.Buttons.Contains(MouseButton.Left))
            {
                StopDragging();
                args.Handled = true;
                return;
            }

            SetValueFromPosition(args.Position);
            args.Handled = true;
        }

        private void SliderMouseUp(object? sender, MouseEventArgs args)
        {
            if (!_isDragging)
                return;

            SetValueFromPosition(args.Position);
            StopDragging();
            args.Handled = true;
        }

        private void SliderTouchDown(object? sender, TouchEventArgs args)
        {
            _isDragging = true;
            SetValueFromPosition(args.Position);
            args.Handled = true;
        }

        private void SliderTouchMove(object? sender, TouchEventArgs args)
        {
            if (!_isDragging)
                return;

            SetValueFromPosition(args.Position);
            args.Handled = true;
        }

        private void SliderTouchUp(object? sender, TouchEventArgs args)
        {
            if (!_isDragging)
                return;

            SetValueFromPosition(args.Position);
            StopDragging();
            args.Handled = true;
        }

        private void SliderTouchCancel(object? sender, TouchEventArgs args)
        {
            // Touch has no capture: the finger drifting off the control ends the drag.
            if (_isDragging)
                StopDragging();
        }

        private void SliderKeyPressed(object? sender, KeyEventArgs args)
        {
            if (args.InputType != InputType.Command)
                return;

            switch (args.Command)
            {
                case KeyboardCommand.CursorLeft:
                case KeyboardCommand.CursorDown:
                    Value -= SmallChange;
                    break;
                case KeyboardCommand.CursorRight:
                case KeyboardCommand.CursorUp:
                    Value += SmallChange;
                    break;
                case KeyboardCommand.Home:
                    Value = Minimum;
                    break;
                case KeyboardCommand.End:
                    Value = Maximum;
                    break;
            }
        }

        private void SetValueFromPosition(PointF position)
        {
            var track = GetTrackRect(ClippingRect);
            if (track.Width <= 0)
                return;

            var percent = MathHelper.Clamp((position.X - track.Left) / track.Width, 0, 1);
            Value = Minimum + (Maximum - Minimum) * percent;
        }

        private float GetThumbCenterX(Rect rect, float scale = 1f)
        {
            var percent = Maximum <= Minimum ? 0 : (_value - Minimum) / (Maximum - Minimum);
            var track = GetTrackRect(rect, scale);
            return track.Left + track.Width * MathHelper.Clamp(percent, 0, 1);
        }

        private float ClampValue(float value)
        {
            return MathHelper.Clamp(value, Minimum, Maximum);
        }

        private void StopDragging()
        {
            _isDragging = false;
            Screen?.ReleaseMouse(this);
        }
    }
}
