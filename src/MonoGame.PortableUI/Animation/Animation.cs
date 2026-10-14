using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Animation
{
    public class Animation
    {
        // A spring that never comes to rest (no damping) still ends after this long.
        private const double MaxSpringSeconds = 30;

        private readonly Control _control;
        private readonly List<IAnimationTween> _tweens = new List<IAnimationTween>();
        private TimeSpan _duration = TimeSpan.FromMilliseconds(150);
        private Easing _easing = Easings.CubicOut;
        private bool _useSpring;
        private SpringSpec _springX;
        private SpringSpec _springY;
        private Action? _completed;
        private TimeSpan _startedAt;
        private bool _isRunning;
        private bool _isFinished;

        internal Animation(Control control)
        {
            _control = control;
        }

        public Animation Scale(double scaleTo)
        {
            return Scale(new Vector2((float)scaleTo, (float)scaleTo));
        }

        public Animation Scale(Vector2 scaleTo)
        {
            ReplaceTween(new Vector2AnimationTween(
                AnimationProperty.Scale,
                control => control.Scale,
                (control, value) => control.Scale = value,
                scaleTo,
                ScaleRestDelta));
            return this;
        }

        public Animation TranslateTo(Vector2 translationTo)
        {
            ReplaceTween(new Vector2AnimationTween(
                AnimationProperty.Translation,
                control => control.Translation,
                (control, value) => control.Translation = value,
                translationTo,
                TranslationRestDelta));
            return this;
        }

        public Animation FadeTo(double opacityTo)
        {
            ReplaceTween(new DoubleAnimationTween(
                AnimationProperty.Opacity,
                control => control.Opacity,
                (control, value) => control.Opacity = value,
                opacityTo,
                ScaleRestDelta));
            return this;
        }

        public Animation ColorTo(Func<Control, Color> getValue, Action<Control, Color> setValue, Color colorTo)
        {
            ReplaceTween(new ColorAnimationTween(
                AnimationProperty.Color,
                getValue,
                setValue,
                colorTo));
            return this;
        }

        public Animation TextColorTo(Color colorTo)
        {
            return ColorTo(
                control => control is TextBlock textBlock ? textBlock.TextColor : Color.Transparent,
                (control, value) =>
                {
                    if (control is TextBlock textBlock)
                        textBlock.TextColor = value;
                },
                colorTo);
        }

        public Animation Duration(TimeSpan duration)
        {
            _duration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
            return this;
        }

        public Animation Ease(Easing easing)
        {
            _easing = easing ?? Easings.Linear;
            return this;
        }

        /// <summary>
        ///     Moves every property with a damped spring instead of <see cref="Duration"/> and
        ///     <see cref="Ease"/>: the animation ends when the spring has come to rest, and it starts
        ///     with the velocity of an animation of the same property it interrupts.
        /// </summary>
        public Animation Spring(SpringSpec spring)
        {
            return Spring(spring, spring);
        }

        /// <summary>
        ///     Like <see cref="Spring(SpringSpec)"/>, with separate springs for the X and Y axes of
        ///     <see cref="Scale(Vector2)"/> and <see cref="TranslateTo"/>: slightly different springs swing
        ///     out of phase, which reads as organic. Opacity and colours follow <paramref name="springX"/>.
        /// </summary>
        public Animation Spring(SpringSpec springX, SpringSpec springY)
        {
            if (!(springX.Stiffness > 0) || !(springY.Stiffness > 0))
                throw new ArgumentException("Use a SpringSpec preset or constructor; default(SpringSpec) has no stiffness.");
            _useSpring = true;
            _springX = springX;
            _springY = springY;
            return this;
        }

        public Animation OnCompleted(Action completed)
        {
            _completed += completed;
            return this;
        }

        public Animation Start()
        {
            if (_tweens.Count == 0 || _isRunning)
                return this;

            _isFinished = false;
            _isRunning = true;
            _startedAt = ScreenSystem.TotalTime;
            foreach (var tween in _tweens)
            {
                tween.CaptureStart(_control);
            }

            _control.StartAnimation(this);
            Update();
            return this;
        }

        public void Cancel(bool complete = false)
        {
            if (_isFinished)
                return;

            if (complete)
            {
                Complete();
                return;
            }

            _isRunning = false;
            _isFinished = true;
            _control.RemoveAnimation(this);
        }

        /// <summary>Takes over the current velocity of <paramref name="other"/>'s tweens of the same
        /// properties, so a spring continues an interrupted motion instead of starting from rest.</summary>
        internal void InheritVelocity(Animation other)
        {
            if (!_useSpring)
                return;

            foreach (var tween in _tweens)
            {
                foreach (var previous in other._tweens)
                {
                    if (previous.Property == tween.Property)
                        tween.InheritVelocity(previous);
                }
            }
        }

        internal bool RemoveConflictingTweens(Animation other)
        {
            for (var i = _tweens.Count - 1; i >= 0; i--)
            {
                if (other.Animates(_tweens[i].Property))
                    _tweens.RemoveAt(i);
            }

            if (_tweens.Count > 0)
                return false;

            CancelFromControl();
            return true;
        }

        internal void CancelFromControl()
        {
            if (_isFinished)
                return;

            _isRunning = false;
            _isFinished = true;
        }

        internal bool Update()
        {
            if (!_isRunning)
                return _isFinished;

            var elapsed = ScreenSystem.TotalTime - _startedAt;
            if (_useSpring)
                return UpdateSpring(Math.Max(0, elapsed.TotalSeconds));

            var progress = _duration == TimeSpan.Zero
                ? 1
                : elapsed.TotalMilliseconds / _duration.TotalMilliseconds;
            progress = Math.Max(0, Math.Min(1, progress));
            var eased = Math.Max(0, Math.Min(1, _easing(progress)));
            var easedPerSecond = progress < 1 ? EasedSlope(progress) / _duration.TotalSeconds : 0;

            foreach (var tween in _tweens)
            {
                tween.Apply(_control, eased, easedPerSecond);
            }

            if (progress < 1)
                return false;

            Complete();
            return true;
        }

        private bool UpdateSpring(double seconds)
        {
            var settled = true;
            foreach (var tween in _tweens)
            {
                if (!tween.ApplySpring(_control, _springX, _springY, seconds))
                    settled = false;
            }

            if (!settled && seconds < MaxSpringSeconds)
                return false;

            Complete();
            return true;
        }

        // Slope of the easing curve, for the velocity a spring inherits from an eased tween.
        private double EasedSlope(double progress)
        {
            const double h = 0.001;
            var from = Math.Max(0, progress - h);
            var to = Math.Min(1, progress + h);
            return (_easing(to) - _easing(from)) / (to - from);
        }

        private void ReplaceTween(IAnimationTween tween)
        {
            for (var i = _tweens.Count - 1; i >= 0; i--)
            {
                if (_tweens[i].Property == tween.Property)
                    _tweens.RemoveAt(i);
            }

            _tweens.Add(tween);
        }

        private bool Animates(AnimationProperty property)
        {
            foreach (var tween in _tweens)
            {
                if (tween.Property == property)
                    return true;
            }

            return false;
        }

        private void Complete()
        {
            if (_isFinished)
                return;

            foreach (var tween in _tweens)
            {
                tween.ApplyEnd(_control);
            }

            _isRunning = false;
            _isFinished = true;
            _control.RemoveAnimation(this);
            _completed?.Invoke();
        }

        // A spring rests once it is this close to its target and slower than ten times that per second.
        private const double ScaleRestDelta = 0.0005;
        private const double TranslationRestDelta = 0.01;
        private const double ProgressRestDelta = 0.001;

        private static bool AtRest(double displacement, double velocity, double restDelta)
            => Math.Abs(displacement) < restDelta && Math.Abs(velocity) < restDelta * 10;

        private interface IAnimationTween
        {
            AnimationProperty Property { get; }
            void CaptureStart(Control control);

            /// <summary>Eased tween: <paramref name="progress"/> 0..1 and its rate per second.</summary>
            void Apply(Control control, double progress, double progressPerSecond);

            /// <summary>Spring tween after <paramref name="seconds"/>; true once at rest.</summary>
            bool ApplySpring(Control control, in SpringSpec springX, in SpringSpec springY, double seconds);

            void InheritVelocity(IAnimationTween previous);
            void ApplyEnd(Control control);
        }

        private sealed class Vector2AnimationTween : IAnimationTween
        {
            private readonly Func<Control, Vector2> _getValue;
            private readonly Action<Control, Vector2> _setValue;
            private readonly Vector2 _target;
            private readonly double _restDelta;
            private Vector2 _start;
            private Vector2 _initialVelocity;
            private Vector2 _velocity;

            public Vector2AnimationTween(
                AnimationProperty property,
                Func<Control, Vector2> getValue,
                Action<Control, Vector2> setValue,
                Vector2 target,
                double restDelta)
            {
                Property = property;
                _getValue = getValue;
                _setValue = setValue;
                _target = target;
                _restDelta = restDelta;
            }

            public AnimationProperty Property { get; }

            public void CaptureStart(Control control)
            {
                _start = _getValue(control);
                _velocity = _initialVelocity;
            }

            public void Apply(Control control, double progress, double progressPerSecond)
            {
                _velocity = (_target - _start) * (float)progressPerSecond;
                _setValue(control, Vector2.Lerp(_start, _target, (float)progress));
            }

            public bool ApplySpring(Control control, in SpringSpec springX, in SpringSpec springY, double seconds)
            {
                springX.Evaluate(_start.X - _target.X, _initialVelocity.X, seconds, out var dx, out var vx);
                springY.Evaluate(_start.Y - _target.Y, _initialVelocity.Y, seconds, out var dy, out var vy);
                _velocity = new Vector2((float)vx, (float)vy);
                _setValue(control, new Vector2(_target.X + (float)dx, _target.Y + (float)dy));
                return AtRest(dx, vx, _restDelta) && AtRest(dy, vy, _restDelta);
            }

            public void InheritVelocity(IAnimationTween previous)
            {
                if (previous is Vector2AnimationTween tween)
                    _initialVelocity = tween._velocity;
            }

            public void ApplyEnd(Control control)
            {
                _velocity = Vector2.Zero;
                _setValue(control, _target);
            }
        }

        private sealed class DoubleAnimationTween : IAnimationTween
        {
            private readonly Func<Control, double> _getValue;
            private readonly Action<Control, double> _setValue;
            private readonly double _target;
            private readonly double _restDelta;
            private double _start;
            private double _initialVelocity;
            private double _velocity;

            public DoubleAnimationTween(
                AnimationProperty property,
                Func<Control, double> getValue,
                Action<Control, double> setValue,
                double target,
                double restDelta)
            {
                Property = property;
                _getValue = getValue;
                _setValue = setValue;
                _target = target;
                _restDelta = restDelta;
            }

            public AnimationProperty Property { get; }

            public void CaptureStart(Control control)
            {
                _start = _getValue(control);
                _velocity = _initialVelocity;
            }

            public void Apply(Control control, double progress, double progressPerSecond)
            {
                _velocity = (_target - _start) * progressPerSecond;
                _setValue(control, _start + (_target - _start) * progress);
            }

            public bool ApplySpring(Control control, in SpringSpec springX, in SpringSpec springY, double seconds)
            {
                springX.Evaluate(_start - _target, _initialVelocity, seconds, out var d, out var v);
                _velocity = v;
                _setValue(control, _target + d);
                return AtRest(d, v, _restDelta);
            }

            public void InheritVelocity(IAnimationTween previous)
            {
                if (previous is DoubleAnimationTween tween)
                    _initialVelocity = tween._velocity;
            }

            public void ApplyEnd(Control control)
            {
                _velocity = 0;
                _setValue(control, _target);
            }
        }

        private sealed class ColorAnimationTween : IAnimationTween
        {
            private readonly Func<Control, Color> _getValue;
            private readonly Action<Control, Color> _setValue;
            private readonly Color _target;
            private Color _start;

            public ColorAnimationTween(
                AnimationProperty property,
                Func<Control, Color> getValue,
                Action<Control, Color> setValue,
                Color target)
            {
                Property = property;
                _getValue = getValue;
                _setValue = setValue;
                _target = target;
            }

            public AnimationProperty Property { get; }

            public void CaptureStart(Control control)
            {
                _start = _getValue(control);
            }

            public void Apply(Control control, double progress, double progressPerSecond)
            {
                _setValue(control, Color.Lerp(_start, _target, (float)progress));
            }

            // Colours spring along the start-to-target progress (an overshoot clamps at the target).
            public bool ApplySpring(Control control, in SpringSpec springX, in SpringSpec springY, double seconds)
            {
                springX.Evaluate(1, 0, seconds, out var remaining, out var v);
                _setValue(control, Color.Lerp(_start, _target, (float)Math.Max(0, Math.Min(1, 1 - remaining))));
                return AtRest(remaining, v, ProgressRestDelta);
            }

            public void InheritVelocity(IAnimationTween previous)
            {
            }

            public void ApplyEnd(Control control)
            {
                _setValue(control, _target);
            }
        }
    }
}
