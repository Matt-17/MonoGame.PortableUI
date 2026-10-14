using System;
using System.Collections.Generic;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Animation
{
    public static class AnimationExtension
    {
        public static Animation Animate(this Control _control)
        {
            return new Animation(_control);
        }

        /// <summary>
        ///     Starts the animation <paramref name="build"/> returns for each control, each one
        ///     <paramref name="interval"/> later than the one before (<see cref="Animation.DelayStart"/>) - list
        ///     rows sliding in one after another. <paramref name="build"/> gets the control and returns an
        ///     animation that is not started yet, e.g. <c>c =&gt; c.Animate().FadeTo(1)</c>.
        /// </summary>
        public static IReadOnlyList<Animation> Stagger(this IEnumerable<Control> controls, TimeSpan interval, Func<Control, Animation> build)
        {
            if (controls == null)
                throw new ArgumentNullException(nameof(controls));
            if (build == null)
                throw new ArgumentNullException(nameof(build));

            var animations = new List<Animation>();
            var index = 0;
            foreach (var control in controls)
            {
                var animation = build(control);
                animation.DelayStart(TimeSpan.FromTicks(interval.Ticks * index++)).Start();
                animations.Add(animation);
            }

            return animations;
        }
    }
}
