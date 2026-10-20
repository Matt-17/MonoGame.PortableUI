using System;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    ///     Material's press feedback: a circle grows from where the control was pressed until it covers the
    ///     control (clipped to its corner radius), stays while pressed and fades out on release.
    /// </summary>
    public sealed class RippleStyle
    {
        /// <summary>Ripple color, straight alpha (Material: black 12 % on light surfaces, white 24 % on filled buttons).</summary>
        public Color Color { get; set; } = new Color(0, 0, 0, 31);

        /// <summary>Time to grow from the press point to the farthest corner.</summary>
        public TimeSpan ExpandDuration { get; set; } = TimeSpan.FromMilliseconds(225);

        /// <summary>
        ///     Width of the circle's soft edge as a fraction of its radius (Android 12+ ripples fade out toward
        ///     the edge); 0 = a crisp, anti-aliased edge.
        /// </summary>
        public float EdgeSoftness { get; set; } = 0.3f;

        /// <summary>
        ///     Radius of an unbounded ripple: the circle around a check box, radio button or switch knob
        ///     (Material: 20, a 40 dp circle). Bounded ripples (buttons, rows) fill their control instead.
        /// </summary>
        public float Radius { get; set; } = 20;

        /// <summary>Color while the control is on (checked box, selected radio, switch on); null keeps <see cref="Color"/>.</summary>
        public Color? CheckedColor { get; set; }

        /// <summary>Fade-out after the release (once the circle has grown).</summary>
        public TimeSpan FadeDuration { get; set; } = TimeSpan.FromMilliseconds(150);
    }
}
