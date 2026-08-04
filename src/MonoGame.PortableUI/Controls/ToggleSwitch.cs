using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    /// An iOS-style pill toggle: a rounded track whose colour crossfades between
    /// <see cref="OffTrackBrush"/> and <see cref="OnTrackBrush"/> while a circular knob slides
    /// from one end to the other. Unlike <see cref="ToggleButton"/> (which only swaps its own
    /// background) this renders the classic switch affordance and animates the transition.
    /// </summary>
    public class ToggleSwitch : Control
    {
        private bool _isOn;
        private float _anim; // 0 = off, 1 = on; eased toward the target each draw
        private TimeSpan _lastDraw;
        private bool _hasLastDraw;

        public ToggleSwitch()
        {
            Width = 96;
            Height = 52;
            KnobInset = 5;
            OffTrackBrush = new SolidColorBrush(new Color(255, 255, 255, 60));
            OnTrackBrush = new SolidColorBrush(new Color(96, 226, 219));
            KnobBrush = new SolidColorBrush(Color.White);
            ShowFocusVisual = true;
            Click += (_, _) => IsOn = !IsOn;
        }

        /// <summary>Padding between the knob and the track edge.</summary>
        public float KnobInset { get; set; }

        public Brush OffTrackBrush { get; set; }
        public Brush OnTrackBrush { get; set; }
        public Brush KnobBrush { get; set; }

        /// <summary>Seconds for the knob to travel end-to-end.</summary>
        public float SlideSeconds { get; set; } = 0.16f;

        public bool IsOn
        {
            get { return _isOn; }
            set
            {
                if (_isOn == value)
                    return;

                _isOn = value;
                Toggled?.Invoke(this, new CheckedEventArgs { IsChecked = _isOn });
            }
        }

        public event EventHandler<CheckedEventArgs>? Toggled;

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            var width = Width.IsFixed() ? Width : 96;
            var height = Height.IsFixed() ? Height : 52;
            return ApplyConstraints(new Size(width, height) + Margin);
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            AdvanceAnimation();

            var radius = new CornerRadius(rect.Height / 2);

            // Track: draw the off colour, then fade the on colour over it by the eased progress.
            OffTrackBrush.Draw(spriteBatch, new BrushContext(rect, radius, RenderOpacity, spriteBatch.GraphicsDevice));
            if (_anim > 0.001f)
                OnTrackBrush.Draw(spriteBatch, new BrushContext(rect, radius, RenderOpacity * _anim, spriteBatch.GraphicsDevice));

            var knobSize = rect.Height - KnobInset * 2;
            var travel = rect.Width - knobSize - KnobInset * 2;
            var knobX = rect.Left + KnobInset + travel * _anim;
            var knob = new Rect(knobX, rect.Top + KnobInset, knobSize, knobSize);
            KnobBrush.Draw(spriteBatch, new BrushContext(knob, new CornerRadius(knobSize / 2), RenderOpacity, spriteBatch.GraphicsDevice));
        }

        private void AdvanceAnimation()
        {
            var now = ScreenSystem.TotalTime;
            var dt = _hasLastDraw ? (float)(now - _lastDraw).TotalSeconds : 0f;
            _lastDraw = now;
            _hasLastDraw = true;

            var target = _isOn ? 1f : 0f;
            if (SlideSeconds <= 0f || dt <= 0f)
            {
                _anim = target;
                return;
            }

            var step = dt / SlideSeconds;
            if (Math.Abs(target - _anim) <= step)
                _anim = target;
            else
                _anim += Math.Sign(target - _anim) * step;
        }
    }
}
