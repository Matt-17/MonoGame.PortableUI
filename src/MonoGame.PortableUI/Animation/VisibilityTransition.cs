using System;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Animation
{
    /// <summary>
    ///     How a control appears and disappears (<c>control.VisibilityTransition</c>): when <c>IsVisible</c> or
    ///     <c>IsGone</c> hides it, it first plays the exit into the hidden pose (<see cref="Opacity"/>,
    ///     <see cref="Scale"/>, <see cref="Offset"/>) and only then leaves the screen (and, for <c>IsGone</c>, the
    ///     layout); when it is shown again it plays the enter from the hidden pose. The pose is applied on top of
    ///     the control's own Scale, Translation and Opacity, so it combines with animations. Showing during an exit
    ///     reverses it from where it is. Immutable and shareable between controls.
    ///     <code>toast.VisibilityTransition = VisibilityTransition.SlideUp();
    /// toast.IsGone = false; // slides in
    /// toast.IsGone = true;  // slides out, then collapses</code>
    /// </summary>
    public sealed class VisibilityTransition
    {
        private static readonly Transition DefaultExit = new Transition(TimeSpan.FromMilliseconds(160), Easings.QuadIn);

        /// <param name="enter">How it moves in from the hidden pose.</param>
        /// <param name="exit">How it moves out; default: <paramref name="enter"/>, or a quick 160 ms ease-in when
        /// <paramref name="enter"/> is a spring (a spring's settling tail would only delay the removal).</param>
        public VisibilityTransition(Transition enter, Transition? exit = null)
        {
            Enter = enter ?? throw new ArgumentNullException(nameof(enter));
            Exit = exit ?? (enter.IsSpring ? DefaultExit : enter);
        }

        public Transition Enter { get; }
        public Transition Exit { get; }

        /// <summary>Opacity factor of the hidden pose (default 0).</summary>
        public float Opacity { get; init; }

        /// <summary>Scale factor of the hidden pose about the control's <c>TransformOrigin</c> (default 1).</summary>
        public Vector2 Scale { get; init; } = Vector2.One;

        /// <summary>Offset of the hidden pose in layout units (default none).</summary>
        public Vector2 Offset { get; init; }

        /// <summary>A plain fade (200 ms in, 150 ms out).</summary>
        public static VisibilityTransition Fade { get; } = new VisibilityTransition(
            new Transition(TimeSpan.FromMilliseconds(200), Easings.QuadOut),
            new Transition(TimeSpan.FromMilliseconds(150), Easings.QuadIn));

        /// <summary>Fades in while growing from 80 % on a bouncy spring; shrinks and fades out.</summary>
        public static VisibilityTransition Pop { get; } = new VisibilityTransition(SpringSpec.Bouncy)
        {
            Scale = new Vector2(0.8f)
        };

        /// <summary>Fades in while growing from 95 % - menus, dialogs.</summary>
        public static VisibilityTransition Zoom { get; } = new VisibilityTransition(
            new Transition(TimeSpan.FromMilliseconds(220), Easings.CubicOut),
            new Transition(TimeSpan.FromMilliseconds(150), Easings.QuadIn))
        {
            Scale = new Vector2(0.95f)
        };

        /// <summary>Slides up into place from <paramref name="distance"/> below while fading in (toasts, sheets).</summary>
        public static VisibilityTransition SlideUp(float distance = 24) => Slide(new Vector2(0, distance));

        /// <summary>Slides down into place from <paramref name="distance"/> above (dropdowns, banners).</summary>
        public static VisibilityTransition SlideDown(float distance = 24) => Slide(new Vector2(0, -distance));

        /// <summary>Slides in from <paramref name="distance"/> to the right.</summary>
        public static VisibilityTransition SlideLeft(float distance = 24) => Slide(new Vector2(distance, 0));

        /// <summary>Slides in from <paramref name="distance"/> to the left.</summary>
        public static VisibilityTransition SlideRight(float distance = 24) => Slide(new Vector2(-distance, 0));

        private static VisibilityTransition Slide(Vector2 offset) => new VisibilityTransition(SpringSpec.Snappy)
        {
            Offset = offset
        };
    }
}
