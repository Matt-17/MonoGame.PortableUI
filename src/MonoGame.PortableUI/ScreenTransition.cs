using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;

namespace MonoGame.PortableUI
{
    /// <summary>
    ///     Built-in visual transition used when a screen is pushed or popped. A popped screen plays its push transition
    ///     backwards. For your own, derive from <see cref="ScreenTransitionAnimation"/>.
    /// </summary>
    public enum ScreenTransition
    {
        None,
        /// <summary>The new screen fades in over the old one.</summary>
        Fade,
        /// <summary>The new screen slides in from the right edge and covers the old one.</summary>
        SlideFromRight,
        /// <summary>The new screen slides up from the bottom edge and covers the old one (sheets).</summary>
        SlideFromBottom,
        /// <summary>The new screen slides in from the left edge and covers the old one.</summary>
        SlideFromLeft,
        /// <summary>The new screen slides down from the top edge and covers the old one.</summary>
        SlideFromTop,
        /// <summary>The new screen pushes in from the right while the old one moves left more slowly (parallax), like a navigation stack.</summary>
        Push,
        /// <summary>The new screen grows in from slightly smaller while fading in; the old one recedes a little.</summary>
        Zoom,
        /// <summary>The old screen slides off to the left and reveals the new one lying under it.</summary>
        Reveal,
        /// <summary>A card flip: the old screen narrows to its vertical centre line, the new one widens out of it.</summary>
        Flip
    }

    /// <summary>
    ///     How one screen is drawn during a transition: applied to the screen's root and its background. Render-only,
    ///     like <see cref="Controls.Control.Scale"/> and friends: layout and hit testing are not affected.
    /// </summary>
    public sealed class ScreenVisual
    {
        public float Opacity { get; set; } = 1;
        public Vector2 Translation { get; set; }
        public Vector2 Scale { get; set; } = Vector2.One;
        /// <summary>Radians, clockwise. A rotated screen is drawn through a texture of itself.</summary>
        public float Rotation { get; set; }
        /// <summary>Pivot of Scale and Rotation relative to the screen (default centre).</summary>
        public Vector2 Origin { get; set; } = new Vector2(0.5f, 0.5f);

        /// <summary>True when nothing is changed (the screen is drawn as usual).</summary>
        public bool IsIdentity => Opacity == 1 && Translation == Vector2.Zero && Scale == Vector2.One && Rotation == 0;

        public void Reset()
        {
            Opacity = 1;
            Translation = Vector2.Zero;
            Scale = Vector2.One;
            Rotation = 0;
            Origin = new Vector2(0.5f, 0.5f);
        }
    }

    /// <summary>
    ///     One frame of a screen transition, handed to <see cref="ScreenTransitionAnimation.Apply"/>. Both visuals are
    ///     reset before each call, so an animation only sets what it changes.
    /// </summary>
    public sealed class ScreenTransitionFrame
    {
        internal ScreenTransitionFrame()
        {
        }

        /// <summary>Size of the screen in layout units (for slide distances).</summary>
        public Vector2 ScreenSize { get; internal set; }

        /// <summary>
        ///     The screen higher in the stack: the one being pushed, or the one being popped (the transition then runs
        ///     backwards).
        /// </summary>
        public ScreenVisual Top { get; } = new ScreenVisual();

        /// <summary>The screen under it (the one being covered, or revealed again); unused when there is none.</summary>
        public ScreenVisual Below { get; } = new ScreenVisual();

        /// <summary>True while pushing (progress runs 0 → 1), false while popping (1 → 0).</summary>
        public bool IsPush { get; internal set; }
    }

    /// <summary>
    ///     A screen transition: called every frame with the eased progress, it places the screen on top and the one
    ///     below it. Progress 0 shows only the screen below, 1 only the top screen; a push runs 0 → 1, popping the
    ///     screen runs 1 → 0, so one animation serves both directions.
    ///     <code>public sealed class Spin : ScreenTransitionAnimation
    /// {
    ///     public override void Apply(float progress, ScreenTransitionFrame frame)
    ///     {
    ///         frame.Top.Rotation = (1 - progress) * MathHelper.Pi;
    ///         frame.Top.Scale = new Vector2(progress);
    ///     }
    /// }
    /// engine.NavigateToScreen(next, new Spin { Duration = TimeSpan.FromMilliseconds(400) });</code>
    ///     Instances are shared freely (no per-navigation state).
    /// </summary>
    public abstract class ScreenTransitionAnimation
    {
        /// <summary>Length of the transition; null uses <see cref="ScreenEngine.TransitionDuration"/>.</summary>
        public TimeSpan? Duration { get; init; }

        /// <summary>Easing of the progress (default <see cref="Easings.CubicOut"/>); popping plays the same curve backwards.</summary>
        public Easing Easing { get; init; } = Easings.CubicOut;

        /// <summary>
        ///     Draw the screen below above the top one while the transition runs (a reveal: the old screen moves away
        ///     on top of the new one). Default false.
        /// </summary>
        public virtual bool BelowOnTop => false;

        public abstract void Apply(float progress, ScreenTransitionFrame frame);

        /// <summary>The built-in animation of <paramref name="transition"/>; null for <see cref="ScreenTransition.None"/>.</summary>
        public static ScreenTransitionAnimation? For(ScreenTransition transition) => transition switch
        {
            ScreenTransition.Fade => Fade,
            ScreenTransition.SlideFromRight => SlideFromRight,
            ScreenTransition.SlideFromBottom => SlideFromBottom,
            ScreenTransition.SlideFromLeft => SlideFromLeft,
            ScreenTransition.SlideFromTop => SlideFromTop,
            ScreenTransition.Push => Push,
            ScreenTransition.Zoom => Zoom,
            ScreenTransition.Reveal => Reveal,
            ScreenTransition.Flip => Flip,
            _ => null
        };

        public static readonly ScreenTransitionAnimation Fade = new FadeAnimation();
        public static readonly ScreenTransitionAnimation SlideFromRight = new SlideAnimation(new Vector2(1, 0));
        public static readonly ScreenTransitionAnimation SlideFromBottom = new SlideAnimation(new Vector2(0, 1));
        public static readonly ScreenTransitionAnimation SlideFromLeft = new SlideAnimation(new Vector2(-1, 0));
        public static readonly ScreenTransitionAnimation SlideFromTop = new SlideAnimation(new Vector2(0, -1));
        public static readonly ScreenTransitionAnimation Push = new PushAnimation();
        public static readonly ScreenTransitionAnimation Zoom = new ZoomAnimation();
        public static readonly ScreenTransitionAnimation Reveal = new RevealAnimation();
        public static readonly ScreenTransitionAnimation Flip = new FlipAnimation { Easing = Easings.Linear };

        private sealed class FadeAnimation : ScreenTransitionAnimation
        {
            public override void Apply(float progress, ScreenTransitionFrame frame) => frame.Top.Opacity = progress;
        }

        private sealed class SlideAnimation : ScreenTransitionAnimation
        {
            private readonly Vector2 _from;

            public SlideAnimation(Vector2 from) => _from = from;

            public override void Apply(float progress, ScreenTransitionFrame frame)
                => frame.Top.Translation = _from * frame.ScreenSize * (1 - progress);
        }

        private sealed class PushAnimation : ScreenTransitionAnimation
        {
            public override void Apply(float progress, ScreenTransitionFrame frame)
            {
                var width = frame.ScreenSize.X;
                frame.Top.Translation = new Vector2(width * (1 - progress), 0);
                // The old screen follows at a third of the speed and dims a little: depth.
                frame.Below.Translation = new Vector2(-width * 0.3f * progress, 0);
                frame.Below.Opacity = 1 - 0.25f * progress;
            }
        }

        private sealed class ZoomAnimation : ScreenTransitionAnimation
        {
            public override void Apply(float progress, ScreenTransitionFrame frame)
            {
                frame.Top.Opacity = progress;
                frame.Top.Scale = new Vector2(0.88f + 0.12f * progress);
                frame.Below.Scale = new Vector2(1 + 0.06f * progress);
            }
        }

        private sealed class RevealAnimation : ScreenTransitionAnimation
        {
            public override bool BelowOnTop => true;

            public override void Apply(float progress, ScreenTransitionFrame frame)
                => frame.Below.Translation = new Vector2(-frame.ScreenSize.X * progress, 0);
        }

        private sealed class FlipAnimation : ScreenTransitionAnimation
        {
            public override void Apply(float progress, ScreenTransitionFrame frame)
            {
                // First half: the old screen turns away (narrows to its centre line); second half: the new one turns in.
                if (progress < 0.5f)
                {
                    frame.Top.Opacity = 0;
                    frame.Below.Scale = new Vector2((float)Math.Max(0.001, 1 - Easings.QuadIn(2 * progress)), 1);
                }
                else
                {
                    frame.Below.Opacity = 0;
                    frame.Top.Scale = new Vector2((float)Math.Max(0.001, Easings.QuadOut(2 * progress - 1)), 1);
                }
            }
        }
    }
}
