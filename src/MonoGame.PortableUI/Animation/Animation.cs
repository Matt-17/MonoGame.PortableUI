using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Animation
{
    public class Animation
    {
        private readonly Control _control;
        private readonly List<IAnimationTween> _tweens = new List<IAnimationTween>();
        private TimeSpan _duration = TimeSpan.FromMilliseconds(150);
        private Easing _easing = Easings.CubicOut;
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
                scaleTo));
            return this;
        }

        public Animation TranslateTo(Vector2 translationTo)
        {
            ReplaceTween(new Vector2AnimationTween(
                AnimationProperty.Translation,
                control => control.Translation,
                (control, value) => control.Translation = value,
                translationTo));
            return this;
        }

        public Animation FadeTo(double opacityTo)
        {
            ReplaceTween(new DoubleAnimationTween(
                AnimationProperty.Opacity,
                control => control.Opacity,
                (control, value) => control.Opacity = value,
                opacityTo));
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

            var progress = _duration == TimeSpan.Zero
                ? 1
                : (ScreenSystem.TotalTime - _startedAt).TotalMilliseconds / _duration.TotalMilliseconds;
            progress = Math.Max(0, Math.Min(1, progress));
            var eased = Math.Max(0, Math.Min(1, _easing(progress)));

            foreach (var tween in _tweens)
            {
                tween.Apply(_control, eased);
            }

            if (progress < 1)
                return false;

            Complete();
            return true;
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

        private interface IAnimationTween
        {
            AnimationProperty Property { get; }
            void CaptureStart(Control control);
            void Apply(Control control, double progress);
            void ApplyEnd(Control control);
        }

        private sealed class Vector2AnimationTween : IAnimationTween
        {
            private readonly Func<Control, Vector2> _getValue;
            private readonly Action<Control, Vector2> _setValue;
            private readonly Vector2 _target;
            private Vector2 _start;

            public Vector2AnimationTween(
                AnimationProperty property,
                Func<Control, Vector2> getValue,
                Action<Control, Vector2> setValue,
                Vector2 target)
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

            public void Apply(Control control, double progress)
            {
                _setValue(control, Vector2.Lerp(_start, _target, (float)progress));
            }

            public void ApplyEnd(Control control)
            {
                _setValue(control, _target);
            }
        }

        private sealed class DoubleAnimationTween : IAnimationTween
        {
            private readonly Func<Control, double> _getValue;
            private readonly Action<Control, double> _setValue;
            private readonly double _target;
            private double _start;

            public DoubleAnimationTween(
                AnimationProperty property,
                Func<Control, double> getValue,
                Action<Control, double> setValue,
                double target)
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

            public void Apply(Control control, double progress)
            {
                _setValue(control, _start + (_target - _start) * progress);
            }

            public void ApplyEnd(Control control)
            {
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

            public void Apply(Control control, double progress)
            {
                _setValue(control, Color.Lerp(_start, _target, (float)progress));
            }

            public void ApplyEnd(Control control)
            {
                _setValue(control, _target);
            }
        }
    }
}
