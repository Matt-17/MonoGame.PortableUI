using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    /// A pill toggle: a rounded track whose colour crossfades between <see cref="OffTrackBrush"/> and
    /// <see cref="OnTrackBrush"/> while a circular knob slides from one end to the other. By default
    /// iOS-style (the knob inside the track); with a <see cref="TrackHeight"/> smaller than the
    /// <see cref="KnobSize"/> and a small <see cref="KnobTravelInset"/> the knob overhangs a thin track
    /// like Android's Material switch. Unlike <see cref="ToggleButton"/> (which only swaps its own
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
            var theme = PortableTheme.ResolveCurrent();

            Width = theme.ToggleSwitchWidth;
            Height = theme.ToggleSwitchHeight;
            KnobInset = 5;
            OffTrackBrush = theme.ToggleSwitchOffTrackBrush;
            OnTrackBrush = theme.ToggleSwitchOnTrackBrush;
            KnobBrush = theme.ToggleSwitchKnobBrush;
            OnKnobBrush = theme.ToggleSwitchOnKnobBrush;
            DisabledTrackBrush = theme.ToggleSwitchDisabledTrackBrush;
            DisabledKnobBrush = theme.ToggleSwitchDisabledKnobBrush;
            TrackWidth = theme.ToggleSwitchTrackWidth;
            TrackHeight = theme.ToggleSwitchTrackHeight;
            KnobSize = theme.ToggleSwitchKnobSize;
            KnobTravelInset = theme.ToggleSwitchKnobTravelInset;
            KnobShadow = theme.ToggleSwitchKnobShadow;
            ShowFocusVisual = true;
            Click += (_, _) => IsOn = !IsOn;
            KeyPressed += ActivateOnKeyPressed;
        }

        protected override Audio.UiSound? GetClickSound() => IsOn ? Audio.UiSound.ToggleOn : Audio.UiSound.ToggleOff;

        protected override Audio.UiSound? GetPressSound() => IsOn ? Audio.UiSound.ToggleOff : Audio.UiSound.ToggleOn;

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

            if (ReferenceEquals(OffTrackBrush, oldTheme.ToggleSwitchOffTrackBrush))
                OffTrackBrush = newTheme.ToggleSwitchOffTrackBrush;
            if (ReferenceEquals(OnTrackBrush, oldTheme.ToggleSwitchOnTrackBrush))
                OnTrackBrush = newTheme.ToggleSwitchOnTrackBrush;
            if (ReferenceEquals(KnobBrush, oldTheme.ToggleSwitchKnobBrush))
                KnobBrush = newTheme.ToggleSwitchKnobBrush;
            if (ReferenceEquals(OnKnobBrush, oldTheme.ToggleSwitchOnKnobBrush))
                OnKnobBrush = newTheme.ToggleSwitchOnKnobBrush;
            if (ReferenceEquals(DisabledTrackBrush, oldTheme.ToggleSwitchDisabledTrackBrush))
                DisabledTrackBrush = newTheme.ToggleSwitchDisabledTrackBrush;
            if (ReferenceEquals(DisabledKnobBrush, oldTheme.ToggleSwitchDisabledKnobBrush))
                DisabledKnobBrush = newTheme.ToggleSwitchDisabledKnobBrush;
            if (ReferenceEquals(KnobShadow, oldTheme.ToggleSwitchKnobShadow))
                KnobShadow = newTheme.ToggleSwitchKnobShadow;
            if (Width.Equals(oldTheme.ToggleSwitchWidth))
                Width = newTheme.ToggleSwitchWidth;
            if (Height.Equals(oldTheme.ToggleSwitchHeight))
                Height = newTheme.ToggleSwitchHeight;
            if (Nullable.Equals(TrackWidth, oldTheme.ToggleSwitchTrackWidth))
                TrackWidth = newTheme.ToggleSwitchTrackWidth;
            if (Nullable.Equals(TrackHeight, oldTheme.ToggleSwitchTrackHeight))
                TrackHeight = newTheme.ToggleSwitchTrackHeight;
            if (Nullable.Equals(KnobSize, oldTheme.ToggleSwitchKnobSize))
                KnobSize = newTheme.ToggleSwitchKnobSize;
            if (Nullable.Equals(KnobTravelInset, oldTheme.ToggleSwitchKnobTravelInset))
                KnobTravelInset = newTheme.ToggleSwitchKnobTravelInset;
        }

        /// <summary>Knob while on; null keeps <see cref="KnobBrush"/> (Material: the accent).</summary>
        public Brush? OnKnobBrush { get; set; }

        /// <summary>Track of a disabled switch; null keeps the on/off tracks.</summary>
        public Brush? DisabledTrackBrush { get; set; }

        /// <summary>Knob of a disabled switch; null keeps the on/off knobs.</summary>
        public Brush? DisabledKnobBrush { get; set; }

        /// <summary>Track width, centered in the control; null fills the control's width.</summary>
        public float? TrackWidth { get; set; }

        /// <summary>Track height, centered in the control; null fills the control's height.</summary>
        public float? TrackHeight { get; set; }

        /// <summary>Knob diameter; null fits it inside the track (track height minus twice <see cref="KnobInset"/>).</summary>
        public float? KnobSize { get; set; }

        /// <summary>Distance from a track end to the knob's center at that end; null = half the track height.</summary>
        public float? KnobTravelInset { get; set; }

        /// <summary>Shadow under the knob (Material: a soft elevation shadow); null = none.</summary>
        public ShadowStyle? KnobShadow { get; set; }

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
            if (IsGoneShown)
                return Size.Empty;

            var width = Width.IsFixed() ? Width : 96;
            var height = Height.IsFixed() ? Height : 52;
            if (KnobSize is { } knob)
                height = Math.Max(height, knob);
            // Min/Max constrain the content box only; margin is added afterwards (same as Control).
            return ApplyConstraints(new Size(width, height)) + Margin;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            AdvanceAnimation();

            var device = spriteBatch.GraphicsDevice;
            var trackWidth = TrackWidth is { } tw ? Math.Min(ToRender(tw), rect.Width) : rect.Width;
            var trackHeight = TrackHeight is { } th ? Math.Min(ToRender(th), rect.Height) : rect.Height;
            var track = new Rect(rect.Left + (rect.Width - trackWidth) / 2, rect.Top + (rect.Height - trackHeight) / 2, trackWidth, trackHeight);
            var radius = new CornerRadius(track.Height / 2);

            // Track: draw the off colour, then fade the on colour over it by the eased progress.
            if (!IsEnabled && DisabledTrackBrush != null)
                DisabledTrackBrush.Draw(spriteBatch, new BrushContext(track, radius, RenderOpacity, device));
            else
            {
                OffTrackBrush.Draw(spriteBatch, new BrushContext(track, radius, RenderOpacity, device));
                if (_anim > 0.001f)
                    OnTrackBrush.Draw(spriteBatch, new BrushContext(track, radius, RenderOpacity * _anim, device));
            }

            var inset = ToRender(KnobInset);
            var knobSize = KnobSize is { } ks ? ToRender(ks) : track.Height - inset * 2;
            // The knob's center travels between the two ends, KnobTravelInset in from each.
            var travelInset = KnobTravelInset is { } ti ? ToRender(ti) : track.Height / 2;
            var centerX = track.Left + travelInset + (track.Width - travelInset * 2) * _anim;
            var knob = new Rect(centerX - knobSize / 2, track.Top + (track.Height - knobSize) / 2, knobSize, knobSize);
            var knobRadius = new CornerRadius(knobSize / 2);
            if (KnobShadow != null)
                ShadowRenderer.Draw(spriteBatch, knob, knobRadius, KnobShadow, RenderOpacity, ToRender(1f));
            if (!IsEnabled && DisabledKnobBrush != null)
                DisabledKnobBrush.Draw(spriteBatch, new BrushContext(knob, knobRadius, RenderOpacity, device));
            else
            {
                KnobBrush.Draw(spriteBatch, new BrushContext(knob, knobRadius, RenderOpacity, device));
                if (OnKnobBrush != null && _anim > 0.001f)
                    OnKnobBrush.Draw(spriteBatch, new BrushContext(knob, knobRadius, RenderOpacity * _anim, device));
            }
        }

        private void AdvanceAnimation()
        {
            var now = ScreenSystem.TotalTime;
            // Clamped: with on-demand rendering the previous frame may be seconds old.
            var dt = _hasLastDraw ? Math.Min(0.05f, (float)(now - _lastDraw).TotalSeconds) : 0f;
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
            {
                _anim += Math.Sign(target - _anim) * step;
                ScreenEngine.RequestAnimationFrame();
            }
        }
    }
}
