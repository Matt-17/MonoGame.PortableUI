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
        // The pointer lives in UI space, where the picture is a plain rectangle: moving up along
        // the left edge runs straight along the edge, and the CRT curve only bends how it is shown.
        private PointF _uiPosition;

        public SurfacePointerCapture(UISurface surface, VirtualInputSource input)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
            _input = input ?? throw new ArgumentNullException(nameof(input));
        }

        /// <summary>True while the pointer belongs to the surface.</summary>
        public bool IsCaptured { get; private set; }

        /// <summary>The captured pointer as shown on the (curved) picture, in surface units.</summary>
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
            _uiPosition = ClampToUi(_surface.MapDisplayToUi(position));
            Position = _surface.MapUiToDisplay(_uiPosition);
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

            _uiPosition = ClampToUi(new PointF(_uiPosition.X + mouseDelta.X * Sensitivity, _uiPosition.Y + mouseDelta.Y * Sensitivity));
            Position = _surface.MapUiToDisplay(_uiPosition);
            _input.SetPointer(Position, leftDown, rightDown, middleDown);
            return true;
        }

        /// <summary>Keeps the pointer inside the UI rectangle (= on the picture once shown).</summary>
        private PointF ClampToUi(PointF point)
        {
            var rect = _surface.Engine.ScreenRect;
            return new PointF(
                MathHelper.Clamp(point.X, rect.Left, rect.Right - 1),
                MathHelper.Clamp(point.Y, rect.Top, rect.Bottom - 1));
        }
    }
}
