using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI
{
    /// <summary>
    ///     "Click in, Escape out" pointer capture for an in-world screen (<see cref="UISurface"/>):
    ///     while captured, relative mouse movement drives a pointer that belongs to the surface —
    ///     it stays put when the camera or the monitor moves and is confined to the visible picture
    ///     (inside the CRT curve, see <see cref="UISurface.IsPointOnDisplay"/>). Platform free: the
    ///     host feeds raw deltas and buttons each frame and keeps the system pointer hidden/centred.
    /// </summary>
    public sealed class SurfacePointerCapture
    {
        private readonly UISurface _surface;
        private readonly VirtualInputSource _input;
        private bool _releaseKeyWasDown;

        public SurfacePointerCapture(UISurface surface, VirtualInputSource input)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
            _input = input ?? throw new ArgumentNullException(nameof(input));
        }

        /// <summary>True while the pointer belongs to the surface.</summary>
        public bool IsCaptured { get; private set; }

        /// <summary>The captured pointer, in surface units.</summary>
        public PointF Position { get; private set; }

        /// <summary>Surface units per pixel of mouse movement (1 = one pixel moves one surface pixel).</summary>
        public float Sensitivity { get; set; } = 1f;

        /// <summary>The key that hands the pointer back; it is not passed on to the surface.</summary>
        public Keys ReleaseKey { get; set; } = Keys.Escape;

        public event EventHandler? Captured;

        /// <summary>Raised after release; <see cref="Position"/> is where the pointer left the surface.</summary>
        public event EventHandler? Released;

        /// <summary>Takes the pointer at <paramref name="position"/> (surface units, e.g. where the user clicked).</summary>
        public void Capture(PointF position)
        {
            Position = Clamp(position, position);
            IsCaptured = true;
            // The release key may still be held from before; it must be pressed anew.
            _releaseKeyWasDown = true;
            _surface.ShowSoftwareCursor = true;
            _input.SetPointer(Position);
            Captured?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Hands the pointer back to the system.</summary>
        public void Release()
        {
            if (!IsCaptured)
                return;
            IsCaptured = false;
            _surface.ShowSoftwareCursor = false;
            // Park the surface pointer outside so nothing stays hovered or pressed.
            _input.SetPointer(new PointF(-100, -100));
            Released?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Feeds one frame of mouse input. Returns false when not captured (or just released), so
        ///     the host routes the frame as usual. <paramref name="keyboard"/> is checked for the release key.
        /// </summary>
        public bool Update(Vector2 mouseDelta, bool leftDown, bool rightDown, bool middleDown, KeyboardState keyboard)
        {
            if (!IsCaptured)
                return false;

            var releaseDown = keyboard.IsKeyDown(ReleaseKey);
            var releasePressed = releaseDown && !_releaseKeyWasDown;
            _releaseKeyWasDown = releaseDown;
            if (releasePressed)
            {
                Release();
                return false;
            }

            var target = new PointF(Position.X + mouseDelta.X * Sensitivity, Position.Y + mouseDelta.Y * Sensitivity);
            Position = Clamp(target, Position);
            _input.SetPointer(Position, leftDown, rightDown, middleDown);
            return true;
        }

        /// <summary>
        ///     Keeps the pointer on the picture: if <paramref name="target"/> is off it, slide along each
        ///     axis, then fall back to the furthest point on the way from <paramref name="from"/>.
        /// </summary>
        private PointF Clamp(PointF target, PointF from)
        {
            if (_surface.IsPointOnDisplay(target))
                return target;
            var alongX = new PointF(target.X, from.Y);
            if (_surface.IsPointOnDisplay(alongX))
                return alongX;
            var alongY = new PointF(from.X, target.Y);
            if (_surface.IsPointOnDisplay(alongY))
                return alongY;
            if (!_surface.IsPointOnDisplay(from))
                return Centre();

            // Binary search between the last valid point and the target.
            var low = 0f;
            var high = 1f;
            for (var i = 0; i < 12; i++)
            {
                var mid = (low + high) / 2;
                var probe = new PointF(from.X + (target.X - from.X) * mid, from.Y + (target.Y - from.Y) * mid);
                if (_surface.IsPointOnDisplay(probe))
                    low = mid;
                else
                    high = mid;
            }
            return new PointF(from.X + (target.X - from.X) * low, from.Y + (target.Y - from.Y) * low);
        }

        private PointF Centre() => new(_surface.Engine.ScreenRect.Width / 2, _surface.Engine.ScreenRect.Height / 2);
    }
}
