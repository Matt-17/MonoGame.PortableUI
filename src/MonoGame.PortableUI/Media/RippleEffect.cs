using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    ///     The state of one control's ripple: started when the control turns pressed, released when it
    ///     stops being pressed, drawn growing and then fading. Bounded ripples fill the control from the
    ///     press point; unbounded ones (check boxes, radio buttons, switches) are a circle around a center.
    /// </summary>
    internal sealed class RippleEffect
    {
        // The origin as a fraction of the control, when the ripple started and was released.
        private Vector2 _origin;
        private TimeSpan _start;
        private TimeSpan? _release;
        private bool _active;
        private bool _pressed;

        /// <summary>
        ///     Follows the control's pressed look; returns true when it flipped (the control redraws).
        ///     <paramref name="bounds"/> and <paramref name="press"/> place the origin (layout units).
        /// </summary>
        public bool Track(bool pressed, Rect bounds, PointF press)
        {
            if (pressed == _pressed)
                return false;
            _pressed = pressed;
            if (pressed)
            {
                _origin = bounds.Width > 0 && bounds.Height > 0 && bounds.Contains(press)
                    ? new Vector2((press.X - bounds.Left) / bounds.Width, (press.Y - bounds.Top) / bounds.Height)
                    : new Vector2(0.5f, 0.5f);
                _start = ScreenSystem.TotalTime;
                _release = null;
                _active = true;
            }
            else if (_active)
                _release = ScreenSystem.TotalTime;
            return true;
        }

        /// <summary>Grows from the press point until it covers <paramref name="clip"/>, clipped to its corners.</summary>
        public void DrawBounded(SpriteBatch spriteBatch, Rect clip, CornerRadius radius, RippleStyle style, Color color, float opacity)
        {
            if (!Progress(style, out var eased, out var fade))
                return;
            var center = new Vector2(clip.Left + clip.Width * _origin.X, clip.Top + clip.Height * _origin.Y);
            // Material's decelerating growth up to the farthest corner.
            var reach = MathF.Sqrt(MathF.Pow(Math.Max(center.X - clip.Left, clip.Right - center.X), 2)
                + MathF.Pow(Math.Max(center.Y - clip.Top, clip.Bottom - center.Y), 2));
            // The soft edge lies outside the reach at full size, so the whole control ends up covered.
            reach /= 1 - Math.Clamp(style.EdgeSoftness, 0, 0.9f);
            RippleRenderer.Draw(spriteBatch, clip, radius, center, reach * (0.1f + 0.9f * eased), style.EdgeSoftness, Brush.ApplyOpacity(color, opacity * fade));
            ScreenEngine.RequestAnimationFrame();
        }

        /// <summary>A circle of <paramref name="radius"/> (render pixels) growing around <paramref name="center"/>, unclipped.</summary>
        public void DrawUnbounded(SpriteBatch spriteBatch, Vector2 center, float radius, RippleStyle style, Color color, float opacity)
        {
            if (!Progress(style, out var eased, out var fade))
                return;
            var area = new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2);
            RippleRenderer.Draw(spriteBatch, area, new CornerRadius(0), center, radius * (0.3f + 0.7f * eased), style.EdgeSoftness, Brush.ApplyOpacity(color, opacity * fade));
            ScreenEngine.RequestAnimationFrame();
        }

        /// <summary>An unbounded ripple reaches past its control: widens the control's ink overflow by its radius.</summary>
        public static Thickness Overflow(Thickness own, RippleStyle? style)
        {
            if (style == null)
                return own;
            var r = style.Radius;
            return new Thickness(Math.Max(own.Left, r), Math.Max(own.Top, r), Math.Max(own.Right, r), Math.Max(own.Bottom, r));
        }

        // How far the ripple has grown (eased) and how much of it is left (fade); false once it is gone.
        private bool Progress(RippleStyle style, out float eased, out float fade)
        {
            eased = 0;
            fade = 1;
            if (!_active)
                return false;
            var now = ScreenSystem.TotalTime;
            var grow = (float)Math.Min(1, (now - _start).TotalMilliseconds / Math.Max(1, style.ExpandDuration.TotalMilliseconds));
            if (_release is { } released)
            {
                // Fades once it has grown: a quick tap still shows the full circle.
                var fadeStart = released > _start + style.ExpandDuration ? released : _start + style.ExpandDuration;
                var faded = (now - fadeStart).TotalMilliseconds / Math.Max(1, style.FadeDuration.TotalMilliseconds);
                if (faded >= 1)
                {
                    _active = false;
                    return false;
                }
                fade = (float)Math.Clamp(1 - faded, 0, 1);
            }
            eased = 1 - (1 - grow) * (1 - grow);
            return true;
        }
    }
}
