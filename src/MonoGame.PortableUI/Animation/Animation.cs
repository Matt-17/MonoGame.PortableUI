using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    ///     <see cref="Repeat"/>, <see cref="Loop"/> and <see cref="Yoyo"/> replay the whole sequence.
    ///     An animation can be started again after it finished (<see cref="Restart"/>), so a hover or press
    ///     effect is built once.
    /// </summary>
    public class Animation
    {
        private readonly Control _control;
        private readonly List<Step> _steps = new List<Step>();
        private Track _current;
        private Action? _completed;
        private TaskCompletionSource<bool>? _completion;
        private TimeSpan _startDelay;
        private int _iterations = 1;
        private bool _yoyo;
        private int _iteration;
        private bool _reversed;
        private int _stepIndex;
        private bool _stepStarted;
        private TimeSpan _stepStartedAt;
        private TimeSpan _iterationStartedAt;
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
                control => control.ShownScale,
                (control, value) => control.SetScaleShown(value),
                scaleTo,
                ScaleRestDelta));
            return this;
        }

        public Animation TranslateTo(Vector2 translationTo)
        {
            _current.Replace(new Vector2AnimationTween(
                AnimationProperty.Translation,
                control => control.ShownTranslation,
                (control, value) => control.SetTranslationShown(value),
                translationTo,
                TranslationRestDelta));
            return this;
        }

        public Animation FadeTo(double opacityTo)
        {
            _current.Replace(new DoubleAnimationTween(
                AnimationProperty.Opacity,
                control => control.ShownOpacity,
                (control, value) => control.SetOpacityShown(value),
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
                        textBlock.SetTextColorShown(value);
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

        /// <summary>Waits <paramref name="delay"/> after <see cref="Start"/> before anything moves (once,
        /// not per repetition). Values are captured when the motion begins.</summary>
        public Animation DelayStart(TimeSpan delay)
        {
            _startDelay = delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
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

        /// <summary>
        ///     Plays the whole sequence <paramref name="times"/> times in total. Without <see cref="Yoyo"/>,
        ///     every repetition jumps back to the values the first one started from (like CSS).
        /// </summary>
        public Animation Repeat(int times)
        {
            if (times < 1)
                throw new ArgumentOutOfRangeException(nameof(times), "An animation plays at least once.");
            _iterations = times;
            return this;
        }

        /// <summary>Repeats until <see cref="Cancel"/>; never completes on its own.</summary>
        public Animation Loop()
        {
            _iterations = 0;
            return this;
        }

        /// <summary>
        ///     Every second repetition plays the sequence backwards in time (steps in reverse order, each
        ///     eased track along its own curve reversed, springs back to where they started), so a loop
        ///     swings to and fro without a jump. Use with <see cref="Repeat"/> or <see cref="Loop"/>.
        /// </summary>
        public Animation Yoyo()
        {
            _yoyo = true;
            return this;
        }

        public Animation OnCompleted(Action completed)
        {
            _completed += completed;
            return this;
        }

        public Animation Start()
        {
            if (_isRunning)
                return this;
            if (!HasWork())
            {
                SignalCompletion(true);
                return this;
            }

            _isFinished = false;
            _isRunning = true;
            _iteration = 0;
            _reversed = false;
            _stepIndex = 0;
            _stepStarted = false;
            _stepStartedAt = _iterationStartedAt = ScreenSystem.TotalTime + _startDelay;
            foreach (var step in _steps)
                step.Reset();

            _control.StartAnimation(this);
            Update();
            return this;
        }

        /// <summary>
        ///     <see cref="Start"/> and a task that completes with true when the animation completes, or false
        ///     when it is cancelled or replaced by another animation of all its properties. The continuation
        ///     of an <c>await</c> runs on the game thread, in the update that ended the animation.
        /// </summary>
        public Task<bool> StartAsync()
        {
            var completion = _completion ??= new TaskCompletionSource<bool>();
            Start();
            return completion.Task;
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
            SignalCompletion(false);
        }

        /// <summary>Takes over the current velocity of <paramref name="other"/>'s tweens of the same
        /// properties, so a spring continues an interrupted motion instead of starting from rest.</summary>
        internal void InheritVelocity(Animation other)
        {
            if (other._stepIndex < other._steps.Count)
                _steps[0].InheritVelocity(other.StepAt(other._stepIndex));
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
            SignalCompletion(false);
        }

        internal bool Update()
        {
            if (!_isRunning)
                return _isFinished;

            var now = ScreenSystem.TotalTime;
            if (now < _stepStartedAt && _stepIndex == 0 && _iteration == 0)
                return false; // DelayStart

            while (true)
            {
                var step = StepAt(_stepIndex);
                if (!_stepStarted)
                {
                    _stepStarted = true;
                    step.Begin(_stepIndex > 0 && !_reversed ? StepAt(_stepIndex - 1) : null);
                }

                if (!step.Update(_control, now, _stepStartedAt, _reversed, out var finishedAt))
                    return false;

                // The next step starts when this one ended, so leftover frame time is not lost.
                _stepStarted = false;
                _stepStartedAt = finishedAt;
                _stepIndex++;
                if (_stepIndex < _steps.Count)
                    continue;

                _iteration++;
                if (_iterations != 0 && _iteration >= _iterations)
                {
                    _stepIndex = _steps.Count - 1;
                    Complete();
                    return true;
                }

                _stepIndex = 0;
                if (_yoyo)
                {
                    _reversed = !_reversed;
                }
                else
                {
                    // Jump back to where the first run started, latest steps first so the earliest wins.
                    for (var i = _steps.Count - 1; i >= 0; i--)
                        _steps[i].ApplyOrigin(_control);
                }

                // A sequence that takes no time would loop forever within this update: one per update.
                if (finishedAt <= _iterationStartedAt)
                {
                    _stepStartedAt = _iterationStartedAt = now;
                    return false;
                }

                _iterationStartedAt = finishedAt;
            }
        }

        private Step StepAt(int index) => _reversed ? _steps[_steps.Count - 1 - index] : _steps[index];

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

            // Remaining steps in playing order, so each property ends at its last target.
            for (var i = _stepIndex; i < _steps.Count; i++)
                StepAt(i).ApplyEnd(_control, _reversed);

            _isRunning = false;
            _isFinished = true;
            _control.RemoveAnimation(this);
            _completed?.Invoke();
            SignalCompletion(true);
        }

        private void SignalCompletion(bool completed)
        {
            var completion = _completion;
            if (completion == null)
                return;
            _completion = null;
            completion.TrySetResult(completed);
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

            public bool Update(Control control, TimeSpan now, TimeSpan startedAt, bool reversed, out TimeSpan finishedAt)
            {
                var done = true;
                finishedAt = startedAt;
                foreach (var track in _tracks)
                {
                    if (!track.Update(control, now, startedAt, reversed, out var trackFinishedAt))
                        done = false;
                    else if (trackFinishedAt > finishedAt)
                        finishedAt = trackFinishedAt;
                }

                return done;
            }

            public void ApplyEnd(Control control, bool reversed)
            {
                foreach (var track in _tracks)
                    track.ApplyEnd(control, reversed);
            }

            public void ApplyOrigin(Control control)
            {
                foreach (var track in _tracks)
                    track.ApplyOrigin(control);
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
            private bool _hasOrigin;
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
                _hasOrigin = false;
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

            public bool Update(Control control, TimeSpan now, TimeSpan stepStartedAt, bool reversed, out TimeSpan finishedAt)
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

                // Played backwards, a track returns to the start it captured going forwards.
                if (!_captured && !reversed)
                {
                    _captured = true;
                    foreach (var tween in Tweens)
                        tween.CaptureStart(control, !_hasOrigin);
                    _hasOrigin = true;
                }

                if (UseSpring)
                {
                    var seconds = (now - movesAt).TotalSeconds;
                    var settled = true;
                    foreach (var tween in Tweens)
                    {
                        if (!tween.ApplySpring(control, SpringX, SpringY, seconds, reversed))
                            settled = false;
                    }

                    if (!settled && seconds < MaxSpringSeconds)
                        return false;

                    ApplyEnd(control, reversed);
                    _finishedAt = now;
                    return true;
                }

                var progress = Duration == TimeSpan.Zero
                    ? 1
                    : (now - movesAt).TotalMilliseconds / Duration.TotalMilliseconds;
                progress = Math.Max(0, Math.Min(1, progress));
                // Backwards in time: the forward curve read from its end.
                var curveAt = reversed ? 1 - progress : progress;
                var eased = Math.Max(0, Math.Min(1, Easing(curveAt)));
                var easedPerSecond = Duration == TimeSpan.Zero ? 0 : EasedSlope(curveAt) / Duration.TotalSeconds;
                if (reversed)
                    easedPerSecond = -easedPerSecond;
                foreach (var tween in Tweens)
                    tween.Apply(control, eased, easedPerSecond);

                if (progress < 1)
                    return false;

                ApplyEnd(control, reversed);
                finishedAt = _finishedAt = movesAt + Duration;
                return true;
            }

            public void ApplyEnd(Control control, bool reversed)
            {
                if (_done)
                    return;
                _done = true;
                foreach (var tween in Tweens)
                    tween.ApplyEnd(control, reversed);
            }

            public void ApplyOrigin(Control control)
            {
                if (!_hasOrigin)
                    return;
                foreach (var tween in Tweens)
                    tween.ApplyOrigin(control);
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

            /// <summary>Takes the current value as the start; <paramref name="origin"/>: also as the value
            /// a repetition jumps back to.</summary>
            void CaptureStart(Control control, bool origin);

            /// <summary>Eased tween: <paramref name="progress"/> 0..1 along start→target and its rate per second.</summary>
            void Apply(Control control, double progress, double progressPerSecond);

            /// <summary>Spring tween after <paramref name="seconds"/> (back to the start when
            /// <paramref name="reversed"/>); true once at rest.</summary>
            bool ApplySpring(Control control, in SpringSpec springX, in SpringSpec springY, double seconds, bool reversed);

            void ResetVelocity();
            void InheritVelocity(IAnimationTween previous);

            /// <summary>Sets the target (the start when <paramref name="reversed"/>); the velocity stays,
            /// for a spring of a following step.</summary>
            void ApplyEnd(Control control, bool reversed);

            void ApplyOrigin(Control control);
        }

        private sealed class Vector2AnimationTween : IAnimationTween
        {
            private readonly Func<Control, Vector2> _getValue;
            private readonly Action<Control, Vector2> _setValue;
            private readonly Vector2 _target;
            private readonly double _restDelta;
            private Vector2 _start;
            private Vector2 _origin;
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

            public void CaptureStart(Control control, bool origin)
            {
                _start = _getValue(control);
                if (origin)
                    _origin = _start;
                _velocity = _initialVelocity;
            }

            public void Apply(Control control, double progress, double progressPerSecond)
            {
                _velocity = (_target - _start) * (float)progressPerSecond;
                _setValue(control, Vector2.Lerp(_start, _target, (float)progress));
            }

            public bool ApplySpring(Control control, in SpringSpec springX, in SpringSpec springY, double seconds, bool reversed)
            {
                var from = reversed ? _target : _start;
                var to = reversed ? _start : _target;
                var velocity0 = reversed ? Vector2.Zero : _initialVelocity;
                springX.Evaluate(from.X - to.X, velocity0.X, seconds, out var dx, out var vx);
                springY.Evaluate(from.Y - to.Y, velocity0.Y, seconds, out var dy, out var vy);
                _velocity = new Vector2((float)vx, (float)vy);
                _setValue(control, new Vector2(to.X + (float)dx, to.Y + (float)dy));
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

            public void ApplyEnd(Control control, bool reversed)
            {
                _setValue(control, reversed ? _start : _target);
            }

            public void ApplyOrigin(Control control)
            {
                _setValue(control, _origin);
            }
        }

        private sealed class DoubleAnimationTween : IAnimationTween
        {
            private readonly Func<Control, double> _getValue;
            private readonly Action<Control, double> _setValue;
            private readonly double _target;
            private readonly double _restDelta;
            private double _start;
            private double _origin;
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

            public void CaptureStart(Control control, bool origin)
            {
                _start = _getValue(control);
                if (origin)
                    _origin = _start;
                _velocity = _initialVelocity;
            }

            public void Apply(Control control, double progress, double progressPerSecond)
            {
                _velocity = (_target - _start) * progressPerSecond;
                _setValue(control, _start + (_target - _start) * progress);
            }

            public bool ApplySpring(Control control, in SpringSpec springX, in SpringSpec springY, double seconds, bool reversed)
            {
                var from = reversed ? _target : _start;
                var to = reversed ? _start : _target;
                springX.Evaluate(from - to, reversed ? 0 : _initialVelocity, seconds, out var d, out var v);
                _velocity = v;
                _setValue(control, to + d);
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

            public void ApplyEnd(Control control, bool reversed)
            {
                _setValue(control, reversed ? _start : _target);
            }

            public void ApplyOrigin(Control control)
            {
                _setValue(control, _origin);
            }
        }

        private sealed class ColorAnimationTween : IAnimationTween
        {
            private readonly Func<Control, Color> _getValue;
            private readonly Action<Control, Color> _setValue;
            private readonly Color _target;
            private Color _start;
            private Color _origin;

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

            public void CaptureStart(Control control, bool origin)
            {
                _start = _getValue(control);
                if (origin)
                    _origin = _start;
            }

            public void Apply(Control control, double progress, double progressPerSecond)
            {
                _setValue(control, Color.Lerp(_start, _target, (float)progress));
            }

            // Colours spring along the start-to-target progress (an overshoot clamps at the target).
            public bool ApplySpring(Control control, in SpringSpec springX, in SpringSpec springY, double seconds, bool reversed)
            {
                springX.Evaluate(1, 0, seconds, out var remaining, out var v);
                var progress = (float)Math.Max(0, Math.Min(1, 1 - remaining));
                _setValue(control, reversed ? Color.Lerp(_target, _start, progress) : Color.Lerp(_start, _target, progress));
                return AtRest(remaining, v, ProgressRestDelta);
            }

            public void ResetVelocity()
            {
            }

            public void InheritVelocity(IAnimationTween previous)
            {
            }

            public void ApplyEnd(Control control, bool reversed)
            {
                _setValue(control, reversed ? _start : _target);
            }

            public void ApplyOrigin(Control control)
            {
                _setValue(control, _origin);
            }
        }
    }
}
