using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Animation
{
    /// <summary>
    ///     A fluent animation of one control. Tweens (<see cref="Scale(Vector2)"/>, <see cref="FadeTo"/>, ...)
    ///     and timing (<see cref="Duration"/>, <see cref="Ease"/>, <see cref="Spring(SpringSpec)"/>,
    ///     <see cref="Delay"/>) apply to the current track. <see cref="With"/> adds a track that runs in
    ///     parallel with its own timing; <see cref="Then()"/> starts a step that begins when every track of
    ///     the previous one has finished and animates from wherever the previous step left the control.
    ///     An animation can be started again after it finished (<see cref="Restart"/>), so a hover or press
    ///     effect is built once.
    /// </summary>
    public class Animation
    {
        private readonly Control _control;
        private readonly List<Step> _steps = new List<Step>();
        private Track _current;
        private Action? _completed;
        private int _stepIndex;
        private bool _stepStarted;
        private TimeSpan _stepStartedAt;
        private bool _isRunning;
        private bool _isFinished;

        internal Animation(Control control)
        {
            _control = control;
            var step = new Step();
            _steps.Add(step);
            _current = step.AddTrack();
        }

        /// <summary>True from <see cref="Start"/> until it completes or is cancelled.</summary>
        public bool IsRunning => _isRunning;

        public Animation Scale(double scaleTo)
        {
            return Scale(new Vector2((float)scaleTo, (float)scaleTo));
        }

        public Animation Scale(Vector2 scaleTo)
        {
            _current.Replace(new Vector2AnimationTween(
                AnimationProperty.Scale,
                control => control.Scale,
                (control, value) => control.Scale = value,
                scaleTo,
                ScaleRestDelta));
            return this;
        }

        public Animation TranslateTo(Vector2 translationTo)
        {
            _current.Replace(new Vector2AnimationTween(
                AnimationProperty.Translation,
                control => control.Translation,
                (control, value) => control.Translation = value,
                translationTo,
                TranslationRestDelta));
            return this;
        }

        public Animation FadeTo(double opacityTo)
        {
            _current.Replace(new DoubleAnimationTween(
                AnimationProperty.Opacity,
                control => control.Opacity,
                (control, value) => control.Opacity = value,
                opacityTo,
                ScaleRestDelta));
            return this;
        }

        public Animation ColorTo(Func<Control, Color> getValue, Action<Control, Color> setValue, Color colorTo)
        {
            _current.Replace(new ColorAnimationTween(
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

        /// <summary>Duration of the current track (default 150 ms).</summary>
        public Animation Duration(TimeSpan duration)
        {
            _current.Duration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
            return this;
        }

        /// <summary>Easing of the current track (default <see cref="Easings.CubicOut"/>).</summary>
        public Animation Ease(Easing easing)
        {
            _current.Easing = easing ?? Easings.Linear;
            return this;
        }

        /// <summary>
        ///     Moves the current track's properties with a damped spring instead of <see cref="Duration"/>
        ///     and <see cref="Ease"/>: the track ends when the spring has come to rest, and it starts with
        ///     the velocity the property had (from an animation it interrupts or the previous step).
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
            _current.UseSpring = true;
            _current.SpringX = springX;
            _current.SpringY = springY;
            return this;
        }

        /// <summary>Waits <paramref name="delay"/> after its step began before the current track moves.</summary>
        public Animation Delay(TimeSpan delay)
        {
            _current.Delay = delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
            return this;
        }

        /// <summary>
        ///     Starts a new step: the tweens after it run once every track before it has finished, from the
        ///     values the control has then. Timing starts fresh (150 ms, <see cref="Easings.CubicOut"/>).
        /// </summary>
        public Animation Then()
        {
            var step = new Step();
            _steps.Add(step);
            _current = step.AddTrack();
            return this;
        }

        /// <summary><see cref="Then()"/> with a pause of <paramref name="delay"/> before the next step moves.</summary>
        public Animation Then(TimeSpan delay)
        {
            return Then().Delay(delay);
        }

        /// <summary>
        ///     Adds a track to the current step that runs in parallel with the others, with its own
        ///     duration, easing, spring and delay; the step ends when its slowest track ends.
        /// </summary>
        public Animation With()
        {
            _current = _steps[_steps.Count - 1].AddTrack();
            return this;
        }

        public Animation OnCompleted(Action completed)
        {
            _completed += completed;
            return this;
        }

        public Animation Start()
        {
            if (_isRunning || !HasWork())
                return this;

            _isFinished = false;
            _isRunning = true;
            _stepIndex = 0;
            _stepStarted = false;
            _stepStartedAt = ScreenSystem.TotalTime;
            foreach (var step in _steps)
                step.Reset();

            _control.StartAnimation(this);
            Update();
            return this;
        }

        /// <summary>Starts again from the control's current values, cancelling a run in progress.</summary>
        public Animation Restart()
        {
            if (_isRunning)
                Cancel();
            return Start();
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
            if (other._stepIndex < other._steps.Count)
                _steps[0].InheritVelocity(other._steps[other._stepIndex]);
        }

        internal bool RemoveConflictingTweens(Animation other)
        {
            var remaining = false;
            foreach (var step in _steps)
            {
                if (step.RemoveTweensAnimatedBy(other))
                    remaining = true;
            }

            if (remaining)
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

            var now = ScreenSystem.TotalTime;
            while (true)
            {
                var step = _steps[_stepIndex];
                if (!_stepStarted)
                {
                    _stepStarted = true;
                    step.Begin(_stepIndex > 0 ? _steps[_stepIndex - 1] : null);
                }

                if (!step.Update(_control, now, _stepStartedAt, out var finishedAt))
                    return false;

                _stepIndex++;
                if (_stepIndex >= _steps.Count)
                {
                    _stepIndex = _steps.Count - 1;
                    Complete();
                    return true;
                }

                // The next step starts when this one ended, so leftover frame time is not lost.
                _stepStarted = false;
                _stepStartedAt = finishedAt;
            }
        }

        private bool HasWork()
        {
            foreach (var step in _steps)
            {
                if (step.HasWork)
                    return true;
            }

            return false;
        }

        private bool Animates(AnimationProperty property)
        {
            foreach (var step in _steps)
            {
                if (step.Animates(property))
                    return true;
            }

            return false;
        }

        private void Complete()
        {
            if (_isFinished)
                return;

            // Remaining steps in order, so each property ends at its last target.
            for (var i = _stepIndex; i < _steps.Count; i++)
                _steps[i].ApplyEnd(_control);

            _isRunning = false;
            _isFinished = true;
            _control.RemoveAnimation(this);
            _completed?.Invoke();
        }

        /// <summary>Tracks that run in parallel; the step ends with its slowest track.</summary>
        private sealed class Step
        {
            private readonly List<Track> _tracks = new List<Track>(1);

            public Track AddTrack()
            {
                var track = new Track();
                _tracks.Add(track);
                return track;
            }

            public bool HasWork
            {
                get
                {
                    foreach (var track in _tracks)
                    {
                        if (track.Tweens.Count > 0 || track.Delay > TimeSpan.Zero)
                            return true;
                    }

                    return false;
                }
            }

            public void Begin(Step? previous)
            {
                foreach (var track in _tracks)
                {
                    track.Begin();
                    if (previous != null)
                        track.InheritVelocity(previous);
                }
            }

            public bool Update(Control control, TimeSpan now, TimeSpan startedAt, out TimeSpan finishedAt)
            {
                var done = true;
                finishedAt = startedAt;
                foreach (var track in _tracks)
                {
                    if (!track.Update(control, now, startedAt, out var trackFinishedAt))
                        done = false;
                    else if (trackFinishedAt > finishedAt)
                        finishedAt = trackFinishedAt;
                }

                return done;
            }

            public void ApplyEnd(Control control)
            {
                foreach (var track in _tracks)
                    track.ApplyEnd(control);
            }

            public void Reset()
            {
                foreach (var track in _tracks)
                    track.Reset();
            }

            public void InheritVelocity(Step previous)
            {
                foreach (var track in _tracks)
                    track.InheritVelocity(previous);
            }

            public bool Animates(AnimationProperty property)
            {
                foreach (var track in _tracks)
                {
                    if (track.Animates(property))
                        return true;
                }

                return false;
            }

            public bool RemoveTweensAnimatedBy(Animation other)
            {
                var remaining = false;
                foreach (var track in _tracks)
                {
                    var tweens = track.Tweens;
                    for (var i = tweens.Count - 1; i >= 0; i--)
                    {
                        if (other.Animates(tweens[i].Property))
                            tweens.RemoveAt(i);
                    }

                    if (tweens.Count > 0)
                        remaining = true;
                }

                return remaining;
            }

            internal List<Track> Tracks => _tracks;
        }

        /// <summary>Tweens sharing one timing: duration + easing, or a spring; optionally delayed.</summary>
        private sealed class Track
        {
            public readonly List<IAnimationTween> Tweens = new List<IAnimationTween>();
            public TimeSpan Duration = TimeSpan.FromMilliseconds(150);
            public Easing Easing = Easings.CubicOut;
            public TimeSpan Delay;
            public bool UseSpring;
            public SpringSpec SpringX;
            public SpringSpec SpringY;
            private bool _captured;
            private bool _done;
            private TimeSpan _finishedAt;

            public void Replace(IAnimationTween tween)
            {
                for (var i = Tweens.Count - 1; i >= 0; i--)
                {
                    if (Tweens[i].Property == tween.Property)
                        Tweens.RemoveAt(i);
                }

                Tweens.Add(tween);
            }

            public void Begin()
            {
                _captured = false;
                _done = false;
            }

            /// <summary>Before a run: nothing captured or finished, no inherited velocity.</summary>
            public void Reset()
            {
                Begin();
                foreach (var tween in Tweens)
                    tween.ResetVelocity();
            }

            public bool Animates(AnimationProperty property)
            {
                foreach (var tween in Tweens)
                {
                    if (tween.Property == property)
                        return true;
                }

                return false;
            }

            public void InheritVelocity(Step previous)
            {
                if (!UseSpring)
                    return;

                foreach (var tween in Tweens)
                {
                    foreach (var track in previous.Tracks)
                    {
                        foreach (var earlier in track.Tweens)
                        {
                            if (earlier.Property == tween.Property)
                                tween.InheritVelocity(earlier);
                        }
                    }
                }
            }

            public bool Update(Control control, TimeSpan now, TimeSpan stepStartedAt, out TimeSpan finishedAt)
            {
                var movesAt = stepStartedAt + Delay;
                if (_done)
                {
                    finishedAt = _finishedAt;
                    return true;
                }

                finishedAt = now;
                if (now < movesAt)
                    return false;

                // A pure pause (Delay or Then(delay) without tweens) ends when it would start moving.
                if (Tweens.Count == 0)
                {
                    _done = true;
                    finishedAt = _finishedAt = movesAt;
                    return true;
                }

                if (!_captured)
                {
                    _captured = true;
                    foreach (var tween in Tweens)
                        tween.CaptureStart(control);
                }

                if (UseSpring)
                {
                    var seconds = (now - movesAt).TotalSeconds;
                    var settled = true;
                    foreach (var tween in Tweens)
                    {
                        if (!tween.ApplySpring(control, SpringX, SpringY, seconds))
                            settled = false;
                    }

                    if (!settled && seconds < MaxSpringSeconds)
                        return false;

                    ApplyEnd(control);
                    _finishedAt = now;
                    return true;
                }

                var progress = Duration == TimeSpan.Zero
                    ? 1
                    : (now - movesAt).TotalMilliseconds / Duration.TotalMilliseconds;
                progress = Math.Max(0, Math.Min(1, progress));
                var eased = Math.Max(0, Math.Min(1, Easing(progress)));
                var easedPerSecond = Duration == TimeSpan.Zero ? 0 : EasedSlope(progress) / Duration.TotalSeconds;
                foreach (var tween in Tweens)
                    tween.Apply(control, eased, easedPerSecond);

                if (progress < 1)
                    return false;

                ApplyEnd(control);
                finishedAt = _finishedAt = movesAt + Duration;
                return true;
            }

            public void ApplyEnd(Control control)
            {
                if (_done)
                    return;
                _done = true;
                foreach (var tween in Tweens)
                    tween.ApplyEnd(control);
            }

            // Slope of the easing curve, for the velocity a spring inherits from an eased tween.
            private double EasedSlope(double progress)
            {
                const double h = 0.001;
                var from = Math.Max(0, progress - h);
                var to = Math.Min(1, progress + h);
                return (Easing(to) - Easing(from)) / (to - from);
            }
        }

        // A spring that never comes to rest (no damping) still ends after this long.
        private const double MaxSpringSeconds = 30;

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

            void ResetVelocity();
            void InheritVelocity(IAnimationTween previous);

            /// <summary>Sets the target; the velocity stays, for a spring of a following step.</summary>
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

            public void ResetVelocity()
            {
                _initialVelocity = Vector2.Zero;
                _velocity = Vector2.Zero;
            }

            public void InheritVelocity(IAnimationTween previous)
            {
                if (previous is Vector2AnimationTween tween)
                    _initialVelocity = tween._velocity;
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

            public void ResetVelocity()
            {
                _initialVelocity = 0;
                _velocity = 0;
            }

            public void InheritVelocity(IAnimationTween previous)
            {
                if (previous is DoubleAnimationTween tween)
                    _initialVelocity = tween._velocity;
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

            public void ResetVelocity()
            {
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
