using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Animation
{
    /// <summary>
    ///     How a moving part of a control - a <see cref="ToggleSwitch.Knob"/>, a selection indicator - travels from one
    ///     state to the next. Built-ins: <see cref="None"/> (jumps), <see cref="Ease"/>, <see cref="Spring"/>,
    ///     <see cref="Drop"/>/<see cref="KnobDrop"/> (a soft body: the tail lags, the front swells, it wobbles out) and
    ///     <see cref="Lift"/> (grows on the way, like a lever coming closer, and settles back). Combine them with
    ///     <c>with</c> (<c>PartMotion.Drop with { LiftScale = 0.12f }</c>); derive and override <see cref="Update"/> for
    ///     an effect of your own. The travel is <see cref="TravelSpring"/>, else <see cref="Duration"/> along
    ///     <see cref="Easing"/>, else a jump. Themes set the defaults; a control's own value overrides them.
    /// </summary>
    public record PartMotion
    {
        /// <summary>No animation: the part jumps to its new place.</summary>
        public static readonly PartMotion None = new();

        /// <summary>A short eased slide.</summary>
        public static readonly PartMotion Ease = new() { Duration = 0.18f, Easing = Easings.Standard };

        /// <summary>A constant-speed slide (the classic switch).</summary>
        public static readonly PartMotion Linear = new() { Duration = 0.16f, Easing = Easings.Linear };

        /// <summary>A spring with a slight overshoot.</summary>
        public static readonly PartMotion Spring = new() { TravelSpring = SpringSpec.FromResponse(0.3, 0.75) };

        /// <summary>Springs and grows by a fifth half way, settling back as it lands.</summary>
        public static readonly PartMotion Lift = Spring with { LiftScale = 0.2f };

        /// <summary>A drop for parts that travel far (a selection indicator): it stretches behind, swells in front and
        /// wobbles out on landing.</summary>
        public static readonly PartMotion Drop = new()
        {
            TravelSpring = SpringSpec.FromResponse(0.38, 0.72),
            Deformation = new Deformation(12, 12) { Trail = 0.8f, Taper = 0.45f, Spring = SpringSpec.FromResponse(0.4, 0.5) }
        };

        /// <summary>A drop for a small part on a short way (a switch knob): it stretches to about twice its width.</summary>
        public static readonly PartMotion KnobDrop = new()
        {
            TravelSpring = SpringSpec.FromResponse(0.3, 0.75),
            Deformation = new Deformation(12, 10) { Trail = 18, Taper = 5, Spring = SpringSpec.FromResponse(0.35, 0.35) }
        };

        /// <summary>Seconds of an eased travel (used without <see cref="TravelSpring"/>; 0 with neither = a jump).</summary>
        public float Duration { get; init; }

        /// <summary>Curve of an eased travel; null = linear.</summary>
        public Easing? Easing { get; init; }

        /// <summary>Spring of the travel (it may overshoot and settle); wins over <see cref="Duration"/>.</summary>
        public SpringSpec? TravelSpring { get; init; }

        /// <summary>A template the part gets a copy of (<see cref="Animation.Deformation.Clone"/>); it follows the part's
        /// motion. Null = the part keeps its shape.</summary>
        public Deformation? Deformation { get; init; }

        /// <summary>How much larger the part is drawn half way (0.2 = 20 %), back to its size on landing. 0 leaves the
        /// part's <see cref="Control.Scale"/> alone.</summary>
        public float LiftScale { get; init; }

        /// <summary>
        ///     Called every frame the part moves and once when it lands: <paramref name="progress"/> runs from 0 at the
        ///     start of this move to 1 at its end (a spring may overshoot). The base applies <see cref="LiftScale"/>.
        ///     Override to add an effect of your own, e.g. on the part's <see cref="Control.Scale"/>, <see cref="Control.Opacity"/>
        ///     or <see cref="Control.Rotation"/> (without a <see cref="Control.Transitions"/> entry for them they apply at once).
        ///     The part's <see cref="Control.Translation"/> belongs to the control that moves it.
        /// </summary>
        protected internal virtual void Update(Control part, float progress)
        {
            if (LiftScale != 0)
                part.SetScaleShown(Vector2.One * (1 + LiftScale * (float)Math.Sin(Math.PI * Math.Clamp(progress, 0, 1))));
        }

        /// <summary>Puts a copy of this motion's deformation on <paramref name="part"/> unless the app gave it one of its
        /// own: <paramref name="given"/> remembers the copy handed out last.</summary>
        internal void AttachTo(Control part, ref Deformation? given)
        {
            if (part.Deformation != null && !ReferenceEquals(part.Deformation, given))
                return;
            part.Deformation = given = Deformation?.Clone();
        }
    }

    /// <summary>The progress of a part's move under a <see cref="PartMotion"/>, 0 at the start of the move and 1 at its end.</summary>
    internal sealed class PartMoveProgress
    {
        private double _velocity; // progress per second
        private float _elapsed;
        private float _duration;

        public float Progress { get; private set; } = 1;
        public bool IsMoving { get; private set; }

        /// <summary>A new move from where the part is now. <paramref name="distanceRatio"/>: this move's length over the
        /// longest one (an eased move takes that share of the duration). <paramref name="velocityScale"/> carries the
        /// speed of a move still running into this one (a spring keeps its momentum): the previous move's length over
        /// this one's, negative when it went the other way.</summary>
        public void Start(PartMotion motion, float distanceRatio, float velocityScale)
        {
            _velocity = IsMoving && float.IsFinite(velocityScale) ? _velocity * velocityScale : 0;
            Progress = 0;
            _elapsed = 0;
            _duration = motion.Duration * Math.Clamp(distanceRatio, 0, 1);
            IsMoving = motion.TravelSpring != null || _duration > 0;
            if (!IsMoving)
                Progress = 1;
        }

        public void Stop()
        {
            Progress = 1;
            _velocity = 0;
            IsMoving = false;
        }

        public void Step(PartMotion motion, float seconds)
        {
            if (!IsMoving || seconds <= 0)
                return;
            if (motion.TravelSpring is { } spring)
            {
                spring.Evaluate(Progress - 1, _velocity, seconds, out var displacement, out var velocity);
                if (Math.Abs(displacement) < 0.001 && Math.Abs(velocity) < 0.01)
                    Stop();
                else
                {
                    Progress = 1 + (float)displacement;
                    _velocity = velocity;
                }
                return;
            }

            _elapsed += seconds;
            if (_duration <= 0 || _elapsed >= _duration)
            {
                Stop();
                return;
            }

            var t = _elapsed / _duration;
            Progress = motion.Easing is { } easing ? (float)easing(t) : t;
        }
    }
}
