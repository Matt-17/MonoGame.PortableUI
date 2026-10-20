using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI
{
    // Two-finger gestures: the topmost control under both fingers that handles Pinch gets the whole gesture.
    public abstract partial class Screen
    {
        private Control? _pinchTarget;
        private float _pinchDistance;
        private float _pinchAngle;
        private PointF _pinchCenter;
        // After a pinch, the finger still down is no tap or scroll until it lifts.
        private bool _pinchHolds;

        /// <summary>True while a pinch (or a finger left over from one) owns the touches: single-finger routing waits.</summary>
        private bool RoutePinch(TouchCollection touches, Control content)
        {
            var down = 0;
            for (var i = 0; i < touches.Count; i++)
            {
                if (touches[i].State != TouchLocationState.Released)
                    down++;
            }

            if (down >= 2)
            {
                var a = TransformPointerPosition((PointF)touches[0].Position.ToPoint());
                var b = TransformPointerPosition((PointF)touches[1].Position.ToPoint());
                var center = new PointF((a.X + b.X) / 2, (a.Y + b.Y) / 2);
                var distance = Math.Max(1, MathF.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y)));
                var angle = MathF.Atan2(b.Y - a.Y, b.X - a.X);
                if (_pinchTarget == null)
                {
                    _pinchTarget = FindPinchTarget(content, center);
                    if (_pinchTarget == null)
                        return false;
                    // What the first finger started is no tap and no scroll any more.
                    var cancel = new TouchEventArgs(center);
                    VisualTreeHelper.CancelDescendantTouches(content, cancel);
                    foreach (var previewed in _previewed)
                        previewed.OnPreviewWithdrawn();
                    TouchScrollOwner = null;
                    _pinchHolds = true;
                    _pinchDistance = distance;
                    _pinchAngle = angle;
                    _pinchCenter = center;
                    _pinchTarget.RaisePinchStarted(new PinchEventArgs(center, 1, 0));
                    return true;
                }

                var turn = angle - _pinchAngle;
                if (turn > MathF.PI) turn -= MathHelper.TwoPi;
                if (turn < -MathF.PI) turn += MathHelper.TwoPi;
                if (distance != _pinchDistance || turn != 0 || center != _pinchCenter)
                    _pinchTarget.RaisePinch(new PinchEventArgs(center, distance / _pinchDistance, turn));
                _pinchDistance = distance;
                _pinchAngle = angle;
                _pinchCenter = center;
                return true;
            }

            if (_pinchTarget != null)
            {
                _pinchTarget.RaisePinchEnded(new PinchEventArgs(_pinchCenter, 1, 0));
                _pinchTarget = null;
            }
            if (_pinchHolds && down == 0)
                _pinchHolds = false;
            return _pinchHolds;
        }

        // Topmost first, like input routing: the deepest control under the point that listens for pinches.
        private static Control? FindPinchTarget(Control control, PointF point)
        {
            if (control.IsGone || !control.IsVisible || !control.IsEnabled || !control.IsHitTestVisible)
                return null;
            var inside = control.ClippingRect.Contains(point);
            if (!inside && control.ClipsDescendants)
                return null;
            for (var i = control.VisualChildCount - 1; i >= 0; i--)
            {
                if (FindPinchTarget(control.GetVisualChild(i), point) is { } found)
                    return found;
            }
            return inside && control.HandlesPinch ? control : null;
        }
    }
}
