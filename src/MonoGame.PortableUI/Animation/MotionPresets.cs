using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Animation
{
    /// <summary>
    ///     Ready-made organic motions built on springs and chained steps. They start at once and return the
    ///     started <see cref="Animation"/>. Each ends in the rest pose: <see cref="Pop"/> and
    ///     <see cref="Jelly"/> at <c>Scale</c> (1, 1), <see cref="Shake"/> at <c>Translation</c> (0, 0).
    ///     For an effect triggered very often, build the animation once and call <see cref="Animation.Restart"/>.
    ///     Squash impacts: <see cref="Control.Squash"/>; idle pulse: <see cref="Control.Breathe"/>.
    /// </summary>
    public static class MotionPresets
    {
        private static readonly SpringSpec JellyX = SpringSpec.FromResponse(0.42, 0.28);
        private static readonly SpringSpec JellyY = SpringSpec.FromResponse(0.5, 0.32);
        private static readonly SpringSpec ShakeSpring = SpringSpec.FromResponse(0.16, 0.18);

        /// <summary>A quick swell and a bouncy settle - emphasis for a click, a new value, a reward.</summary>
        public static Animation Pop(this Control control, float strength = 1)
        {
            return control.Animate()
                .Scale(1 + 0.14f * strength).Duration(TimeSpan.FromMilliseconds(80)).Ease(Easings.QuadOut)
                .Then()
                .Scale(1).Spring(SpringSpec.Bouncy)
                .Start();
        }

        /// <summary>
        ///     Squeezes the control wide and flat, then lets it wobble back with the two axes on different
        ///     springs, so it swings like jelly. Combine with <see cref="Control.TransformOrigin"/> (0.5, 1) to
        ///     keep it standing on its bottom edge.
        /// </summary>
        public static Animation Jelly(this Control control, float strength = 1)
        {
            return control.Animate()
                .Scale(new Vector2(1 + 0.16f * strength, 1 - 0.14f * strength))
                .Duration(TimeSpan.FromMilliseconds(70)).Ease(Easings.QuadOut)
                .Then()
                .Scale(Vector2.One).Spring(JellyX, JellyY)
                .Start();
        }

        /// <summary>A sideways shake that rings out - "no", a wrong password, a locked item.</summary>
        public static Animation Shake(this Control control, float distance = 10)
        {
            return control.Animate()
                .TranslateTo(new Vector2(distance, 0)).Duration(TimeSpan.FromMilliseconds(40)).Ease(Easings.QuadOut)
                .Then()
                .TranslateTo(Vector2.Zero).Spring(ShakeSpring)
                .Start();
        }
    }
}
