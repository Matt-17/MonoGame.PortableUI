using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Animation;
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
    /// <see cref="Track"/> and <see cref="Knob"/> are real controls, so whatever works on a control works on them
    /// (<see cref="Control.Deformation"/>, glow, scale transitions). The knob slides by its
    /// <see cref="Control.Translation"/> as <see cref="KnobMotion"/> says (the theme's
    /// <see cref="PortableTheme.ToggleSwitchKnobMotion"/> unless set: a drop on modern themes).
    /// </summary>
    public class ToggleSwitch : Control
    {
        private readonly Part _track;
        private readonly Part _knob;
        private readonly PartMoveProgress _move = new PartMoveProgress();
        private bool _isOn;
        private float _anim; // 0 = off, 1 = on; a spring may overshoot
        private float _from;
        private float _to;
        private float _travel;
        private TimeSpan _lastStep;
        private bool _hasLastStep;
        private PartMotion? _knobMotion;
        private PartMotion? _attachedMotion;
        private Deformation? _givenDeformation;
        private float? _trackWidth;
        private float? _trackHeight;
        private float? _knobSize;
        private float? _knobTravelInset;
        private float _knobInset;

        public ToggleSwitch()
        {
            var theme = PortableTheme.ResolveCurrent();

            _track = new Part(this, isKnob: false);
            _knob = new Part(this, isKnob: true);
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
            ActivatesOnKeys = true;
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

        /// <summary>The track as a control (drawn with the track brushes; layout and input belong to the switch).</summary>
        public Control Track => _track;

        /// <summary>The knob as a control. It rests at the off end and slides by its <see cref="Control.Translation"/>,
        /// which the switch drives; its deformation, scale, glow and the like are free to set.</summary>
        public Control Knob => _knob;

        /// <summary>How the knob travels (<see cref="PartMotion.None"/>, <see cref="PartMotion.Ease"/>,
        /// <see cref="PartMotion.Spring"/>, <see cref="PartMotion.KnobDrop"/>, <see cref="PartMotion.Lift"/>, combined
        /// or your own); null = the theme's <see cref="PortableTheme.ToggleSwitchKnobMotion"/>.</summary>
        public PartMotion? KnobMotion
        {
            get => _knobMotion;
            set
            {
                if (_knobMotion == value)
                    return;
                _knobMotion = value;
                RequestRedraw();
            }
        }

        private PartMotion EffectiveKnobMotion => _knobMotion ?? ResolveTheme().ToggleSwitchKnobMotion ?? PartMotion.None;

        /// <summary>Knob while on; null keeps <see cref="KnobBrush"/> (Material: the accent).</summary>
        public Brush? OnKnobBrush { get; set; }

        /// <summary>Track of a disabled switch; null keeps the on/off tracks.</summary>
        public Brush? DisabledTrackBrush { get; set; }

        /// <summary>Knob of a disabled switch; null keeps the on/off knobs.</summary>
        public Brush? DisabledKnobBrush { get; set; }

        /// <summary>Track width, centered in the control; null fills the control's width.</summary>
        public float? TrackWidth
        {
            get => _trackWidth;
            set => SetGeometry(ref _trackWidth, value);
        }

        /// <summary>Track height, centered in the control; null fills the control's height.</summary>
        public float? TrackHeight
        {
            get => _trackHeight;
            set => SetGeometry(ref _trackHeight, value);
        }

        /// <summary>Knob diameter; null fits it inside the track (track height minus twice <see cref="KnobInset"/>).</summary>
        public float? KnobSize
        {
            get => _knobSize;
            set => SetGeometry(ref _knobSize, value);
        }

        /// <summary>Distance from a track end to the knob's center at that end; null = half the track height.</summary>
        public float? KnobTravelInset
        {
            get => _knobTravelInset;
            set => SetGeometry(ref _knobTravelInset, value);
        }

        /// <summary>Shadow under the knob (Material: a soft elevation shadow); null = none.</summary>
        public ShadowStyle? KnobShadow { get; set; }

        /// <summary>Padding between the knob and the track edge.</summary>
        public float KnobInset
        {
            get => _knobInset;
            set
            {
                if (_knobInset.Equals(value))
                    return;
                _knobInset = value;
                InvalidateLayout(true);
            }
        }

        public Brush OffTrackBrush { get; set; }
        public Brush OnTrackBrush { get; set; }
        public Brush KnobBrush { get; set; }

        /// <summary>Seconds for the knob to travel end-to-end at a constant speed: sets <see cref="KnobMotion"/> to
        /// <see cref="PartMotion.Linear"/> with that duration.</summary>
        [Obsolete("Set KnobMotion (e.g. PartMotion.Linear with { Duration = ... }).")]
        public float SlideSeconds
        {
            get => KnobMotion is { TravelSpring: null } motion ? motion.Duration : 0.16f;
            set => KnobMotion = PartMotion.Linear with { Duration = Math.Max(0, value) };
        }

        public bool IsOn
        {
            get { return _isOn; }
            set
            {
                if (_isOn == value)
                    return;

                _isOn = value;
                Toggled?.Invoke(this, new CheckedEventArgs { IsChecked = _isOn });
                RequestRedraw();
            }
        }

        public event EventHandler<CheckedEventArgs>? Toggled;

        /// <summary>
        ///     Ripple around the knob while pressed; null uses the theme's <see cref="PortableTheme.ToggleRipple"/>
        ///     (off by default).
        /// </summary>
        public RippleStyle? Ripple { get; set; }

        private RippleStyle? EffectiveRipple => Ripple ?? ResolveTheme().ToggleRipple;

        private readonly RippleEffect _ripple = new RippleEffect();

        public override IEnumerable<Control> GetDescendants()
        {
            yield return _track;
            yield return _knob;
        }

        protected internal override int VisualChildCount => 2;

        protected internal override Control GetVisualChild(int index) => index == 0 ? _track : _knob;

        internal override void OnFrameUpdate()
        {
            base.OnFrameUpdate();
            if (_ripple.Track(IsPointerPressedVisual, ClippingRect, LastPressPosition) && EffectiveRipple != null)
                RequestRedraw();
            AdvanceAnimation();
        }

        protected internal override Thickness GetInkOverflow() => RippleEffect.Overflow(base.GetInkOverflow(), EffectiveRipple);

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

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            var box = ClippingRect;
            var trackWidth = TrackWidth is { } tw ? Math.Min(tw, box.Width) : box.Width;
            var trackHeight = TrackHeight is { } th ? Math.Min(th, box.Height) : box.Height;
            var track = new Rect(box.Left + (box.Width - trackWidth) / 2, box.Top + (box.Height - trackHeight) / 2, trackWidth, trackHeight);
            var knobSize = KnobSize ?? track.Height - KnobInset * 2;
            // The knob's center travels between the two ends, KnobTravelInset in from each; it rests at the off end.
            var travelInset = KnobTravelInset ?? track.Height / 2;
            _travel = track.Width - travelInset * 2;
            _track.UpdateLayout(track);
            _knob.UpdateLayout(new Rect(track.Left + travelInset - knobSize / 2, track.Top + (track.Height - knobSize) / 2, knobSize, knobSize));
            PlaceKnob();
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            // The ripple follows the knob, behind the track.
            if (EffectiveRipple is not { } ripple)
                return;
            var knob = _knob.ClippingRect;
            var x = knob.Left + knob.Width / 2 - ClippingRect.Left + _travel * _anim;
            var y = knob.Top + knob.Height / 2 - ClippingRect.Top;
            var center = new Vector2(rect.Left + ToRender(x), rect.Top + ToRender(y));
            _ripple.DrawUnbounded(spriteBatch, center, ToRender(ripple.Radius), ripple, _isOn ? ripple.CheckedColor ?? ripple.Color : ripple.Color, RenderOpacity);
        }

        // How far on the switch looks: the track and knob colours mix by it (the knob's place may overshoot).
        private float Mix => Math.Clamp(_anim, 0, 1);

        private void DrawTrack(SpriteBatch spriteBatch, Rect track, float opacity)
        {
            var device = spriteBatch.GraphicsDevice;
            var radius = new CornerRadius(track.Height / 2);
            // Track: draw the off colour, then fade the on colour over it by the progress.
            if (!IsEnabled && DisabledTrackBrush != null)
                DisabledTrackBrush.Draw(spriteBatch, new BrushContext(track, radius, opacity, device));
            else
            {
                OffTrackBrush.Draw(spriteBatch, new BrushContext(track, radius, opacity, device));
                if (Mix > 0.001f)
                    OnTrackBrush.Draw(spriteBatch, new BrushContext(track, radius, opacity * Mix, device));
            }
        }

        private void DrawKnob(SpriteBatch spriteBatch, Rect knob, float opacity, float scale)
        {
            var device = spriteBatch.GraphicsDevice;
            var knobRadius = new CornerRadius(knob.Width / 2);
            if (KnobShadow != null)
                ShadowRenderer.Draw(spriteBatch, knob, knobRadius, KnobShadow, opacity, scale);
            if (!IsEnabled && DisabledKnobBrush != null)
                DisabledKnobBrush.Draw(spriteBatch, new BrushContext(knob, knobRadius, opacity, device));
            else
            {
                KnobBrush.Draw(spriteBatch, new BrushContext(knob, knobRadius, opacity, device));
                if (OnKnobBrush != null && Mix > 0.001f)
                    OnKnobBrush.Draw(spriteBatch, new BrushContext(knob, knobRadius, opacity * Mix, device));
            }
        }

        private float KnobShadowExtent()
        {
            var extent = 0f;
            for (var shadow = KnobShadow; shadow != null; shadow = shadow.Also)
            {
                if (!shadow.Inset)
                    extent = Math.Max(extent, shadow.Blur + shadow.Spread + Math.Max(Math.Abs(shadow.Offset.X), Math.Abs(shadow.Offset.Y)));
            }

            return extent;
        }

        private void SetGeometry(ref float? field, float? value)
        {
            if (Nullable.Equals(field, value))
                return;
            field = value;
            InvalidateLayout(true);
        }

        private void PlaceKnob() => _knob.SetTranslationShown(new Vector2(_travel * _anim, 0));

        private void AdvanceAnimation()
        {
            var motion = EffectiveKnobMotion;
            if (!ReferenceEquals(motion, _attachedMotion))
            {
                _attachedMotion = motion;
                motion.AttachTo(_knob, ref _givenDeformation);
            }

            var now = ScreenSystem.TotalTime;
            // Clamped: with on-demand rendering the previous frame may be seconds old.
            var seconds = _hasLastStep ? Math.Min(0.05f, (float)(now - _lastStep).TotalSeconds) : 0f;
            _lastStep = now;
            _hasLastStep = true;

            var target = _isOn ? 1f : 0f;
            if (target != _to)
            {
                // A new move from where the knob is; one still running hands over its speed.
                var previous = _to - _from;
                _from = _anim;
                _to = target;
                if (seconds <= 0)
                    _move.Stop();
                else
                    _move.Start(motion, Math.Abs(_to - _from), previous / (_to - _from));
            }
            else if (!_move.IsMoving)
            {
                return;
            }

            _move.Step(motion, seconds);
            _anim = _from + (_to - _from) * _move.Progress;
            motion.Update(_knob, _move.Progress);
            PlaceKnob();
            RequestRedraw();
        }

        /// <summary>A moving part of the switch, drawn by it; it takes no input of its own.</summary>
        private sealed class Part : Control
        {
            private readonly ToggleSwitch _owner;
            private readonly bool _isKnob;

            public Part(ToggleSwitch owner, bool isKnob)
            {
                _owner = owner;
                _isKnob = isKnob;
                IsFocusable = false;
                IsHitTestVisible = false;
                Parent = owner;
            }

            protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
            {
                base.OnDraw(spriteBatch, rect);
                if (_isKnob)
                    _owner.DrawKnob(spriteBatch, rect, RenderOpacity, ToRender(1f));
                else
                    _owner.DrawTrack(spriteBatch, rect, RenderOpacity);
            }

            protected internal override Thickness GetInkOverflow()
            {
                var ink = base.GetInkOverflow();
                if (!_isKnob)
                    return ink;
                var extent = _owner.KnobShadowExtent();
                return new Thickness(Math.Max(ink.Left, extent), Math.Max(ink.Top, extent), Math.Max(ink.Right, extent), Math.Max(ink.Bottom, extent));
            }
        }
    }
}
