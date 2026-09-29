using System;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>How a <see cref="FlyOut"/>'s content relates to its anchor point.</summary>
    public enum FlyOutPlacement
    {
        /// <summary>The content's bottom-left corner sits at the anchor (opens upward).</summary>
        Above,
        /// <summary>The content's top-left corner sits at the anchor (opens downward, e.g. dropdowns).</summary>
        Below,
        /// <summary>Opens to the right of the anchor, top-aligned.</summary>
        Right,
        /// <summary>Opens to the left of the anchor, top-aligned.</summary>
        Left,
        /// <summary>Below when there is room, otherwise above.</summary>
        Auto
    }

    public class FlyOut : ContentControl, IDisposable
    {
        private readonly Rect _anchor;
        private readonly FlyOutPlacement _placement;
        private bool _isOpen;

        // Popup content (dropdown lists, menus) must never draw outside the flyout bounds.
        protected internal override bool ClipsDescendants => true;

        public event EventHandler? Showing;
        public event EventHandler? Shown;
        public event EventHandler? Dismissing;
        public event EventHandler? Dismissed;

        public FlyOut(PointF position, bool removeOnRelease, FlyOutPlacement placement = FlyOutPlacement.Above)
            : this(new Rect(position.X, position.Y, 0, 0), removeOnRelease, placement)
        {
        }

        /// <summary>Opens next to <paramref name="anchor"/> (e.g. a button's bounds) and flips to the
        /// opposite side when the preferred one does not fit on screen.</summary>
        public FlyOut(Rect anchor, bool removeOnRelease, FlyOutPlacement placement = FlyOutPlacement.Below)
        {
            _anchor = anchor;
            _placement = placement;
            // A container, not a stop: keyboard focus goes to the menu items / list inside it.
            IsFocusable = false;
            MouseEventHandler onMouseDown = (sender, args) => Screen?.ClearFlyOut();
            TouchEventHandler onTouchDown = (sender, args) => Screen?.ClearFlyOut();
            if (removeOnRelease)
            {
                MouseUp += onMouseDown;
                TouchUp += onTouchDown;
            }
            else
            {
                MouseDown += onMouseDown;
                TouchDown += onTouchDown;
            }
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            var size = Content?.Measure() ?? Size.Empty;
            var screen = Screen?.ScreenRect ?? rect;
            var pos = PlaceRect(_anchor, size, _placement, screen);
            pos = Screen.ClampPopupRect(pos, screen, 0);
            Content?.UpdateLayout(pos);
        }

        /// <summary>Preferred popup rect for a placement, flipped to the opposite side when it would
        /// leave <paramref name="screen"/> and the other side has room.</summary>
        internal static Rect PlaceRect(Rect anchor, Size size, FlyOutPlacement placement, Rect screen)
        {
            Rect Below() => new Rect(anchor.Left, anchor.Bottom, size.Width, size.Height);
            Rect Above() => new Rect(anchor.Left, anchor.Top - size.Height, size.Width, size.Height);
            Rect Right() => new Rect(anchor.Right, anchor.Top, size.Width, size.Height);
            Rect Left() => new Rect(anchor.Left - size.Width, anchor.Top, size.Width, size.Height);
            bool FitsVertically(Rect r) => screen.Height <= 0 || (r.Top >= screen.Top && r.Bottom <= screen.Bottom);
            bool FitsHorizontally(Rect r) => screen.Width <= 0 || (r.Left >= screen.Left && r.Right <= screen.Right);

            switch (placement)
            {
                case FlyOutPlacement.Above:
                    return FitsVertically(Above()) || !FitsVertically(Below()) ? Above() : Below();
                case FlyOutPlacement.Right:
                    return FitsHorizontally(Right()) || !FitsHorizontally(Left()) ? Right() : Left();
                case FlyOutPlacement.Left:
                    return FitsHorizontally(Left()) || !FitsHorizontally(Right()) ? Left() : Right();
                default:
                    return FitsVertically(Below()) || !FitsVertically(Above()) ? Below() : Above();
            }
        }

        public void Dispose()
        {
            Content = null;
        }

        internal void NotifyShowing()
        {
            if (_isOpen)
                return;
            Showing?.Invoke(this, EventArgs.Empty);
        }

        internal void NotifyShown()
        {
            if (_isOpen)
                return;
            _isOpen = true;
            Shown?.Invoke(this, EventArgs.Empty);
        }

        internal void NotifyDismissing()
        {
            if (!_isOpen)
                return;
            Dismissing?.Invoke(this, EventArgs.Empty);
        }

        internal void NotifyDismissed()
        {
            if (!_isOpen)
                return;
            _isOpen = false;
            Dismissed?.Invoke(this, EventArgs.Empty);
        }
    }
}
