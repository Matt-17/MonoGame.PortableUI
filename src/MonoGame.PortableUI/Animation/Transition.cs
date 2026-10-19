using System;

namespace MonoGame.PortableUI.Animation
{
    /// <summary>The properties a <see cref="Transition"/> can animate implicitly (<c>control.Transitions</c>).</summary>
    [Flags]
    public enum TransitionProperty
    {
        None = 0,
        Scale = 1,
        Translation = 2,
        Opacity = 4,
        /// <summary><c>BackgroundBrush</c>, including the theme's per-state backgrounds (hover, pressed).</summary>
        Background = 8,
        /// <summary><c>BorderBrush</c>, including the theme's per-state borders.</summary>
        BorderBrush = 16,
        /// <summary><c>TextBlock.TextColor</c>.</summary>
        TextColor = 32,
        /// <summary><c>Control.Rotation</c>.</summary>
        Rotation = 64,
        /// <summary><c>Control.Skew</c>.</summary>
        Skew = 128,
        /// <summary>The render transform: Scale, Translation, Opacity, Rotation and Skew.</summary>
        Transform = Scale | Translation | Opacity | Rotation | Skew,
        /// <summary>Background, border and text colour.</summary>
        Colors = Background | BorderBrush | TextColor,
        All = Transform | Colors
    }

    /// <summary>
    ///     How a property moves to a new value when it is set, like a CSS <c>transition</c>: a duration
    ///     and an easing, or a <see cref="SpringSpec"/>. A spring that receives a new target mid-flight
    ///     keeps its velocity, so rapid changes (hover in, hover out) flow into each other. Immutable and
    ///     shareable between controls.
    /// </summary>
    public sealed class Transition
    {
        public Transition(TimeSpan duration, Easing? easing = null, TimeSpan delay = default)
        {
            Duration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
            Easing = easing ?? Easings.CubicOut;
            Delay = delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
        }

        public Transition(SpringSpec spring, TimeSpan delay = default)
            : this(spring, spring, delay)
        {
        }

        /// <summary>A spring transition with separate springs for the X and Y axes of Scale,
        /// Translation and Skew (out of phase reads as organic); opacity and colours follow <paramref name="springX"/>.</summary>
        public Transition(SpringSpec springX, SpringSpec springY, TimeSpan delay = default)
        {
            if (!(springX.Stiffness > 0) || !(springY.Stiffness > 0))
                throw new ArgumentException("Use a SpringSpec preset or constructor; default(SpringSpec) has no stiffness.");
            IsSpring = true;
            SpringX = springX;
            SpringY = springY;
            Easing = Easings.Linear;
            Delay = delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
        }

        public TimeSpan Duration { get; }
        public Easing Easing { get; }
        public TimeSpan Delay { get; }
        public bool IsSpring { get; }
        public SpringSpec SpringX { get; }
        public SpringSpec SpringY { get; }

        public static implicit operator Transition(TimeSpan duration) => new Transition(duration);
        public static implicit operator Transition(SpringSpec spring) => new Transition(spring);
    }

    /// <summary>
    ///     A control's implicit transitions: once a property has one, setting the property animates it
    ///     from its current look to the new value. <c>control.Scale</c> and friends return the new value
    ///     (the target) right away; drawing shows the moving value. An explicit <c>Animate()</c> of a
    ///     property takes over from a running transition.
    /// </summary>
    public sealed class TransitionCollection
    {
        private readonly Transition?[] _transitions = new Transition?[8];

        internal TransitionCollection(Action changed)
        {
            Changed = changed;
        }

        internal Action Changed { get; }

        /// <summary>The transition of <paramref name="property"/> (a single flag), or null.</summary>
        public Transition? this[TransitionProperty property]
        {
            get
            {
                var index = IndexOf(property);
                return index < 0 ? null : _transitions[index];
            }
        }

        /// <summary>Sets <paramref name="transition"/> for every property flag in <paramref name="properties"/>.</summary>
        public TransitionCollection Add(TransitionProperty properties, Transition transition)
        {
            if (transition == null)
                throw new ArgumentNullException(nameof(transition));
            Set(properties, transition);
            return this;
        }

        public TransitionCollection Add(TransitionProperty properties, TimeSpan duration, Easing? easing = null)
            => Add(properties, new Transition(duration, easing));

        public TransitionCollection Add(TransitionProperty properties, SpringSpec spring)
            => Add(properties, new Transition(spring));

        /// <summary>Removes the transitions of <paramref name="properties"/>; running ones jump to their target.</summary>
        public TransitionCollection Remove(TransitionProperty properties)
        {
            Set(properties, null);
            return this;
        }

        public TransitionCollection Clear() => Remove(TransitionProperty.All);

        private void Set(TransitionProperty properties, Transition? transition)
        {
            for (var i = 0; i < _transitions.Length; i++)
            {
                if (((int)properties & (1 << i)) != 0)
                    _transitions[i] = transition;
            }

            Changed();
        }

        private static int IndexOf(TransitionProperty property) => property switch
        {
            TransitionProperty.Scale => 0,
            TransitionProperty.Translation => 1,
            TransitionProperty.Opacity => 2,
            TransitionProperty.Background => 3,
            TransitionProperty.BorderBrush => 4,
            TransitionProperty.TextColor => 5,
            TransitionProperty.Rotation => 6,
            TransitionProperty.Skew => 7,
            _ => -1
        };
    }
}
