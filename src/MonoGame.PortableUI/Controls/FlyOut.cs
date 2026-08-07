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
        Below
    }

    public class FlyOut : ContentControl, IDisposable
    {
        private readonly PointF _position;
        private readonly FlyOutPlacement _placement;
        private bool _isOpen;

        // Popup content (dropdown lists, menus) must never draw outside the flyout bounds.
        protected internal override bool ClipsDescendants => true;

        public event EventHandler? Showing;
        public event EventHandler? Shown;
        public event EventHandler? Dismissing;
        public event EventHandler? Dismissed;

        public FlyOut(PointF position, bool removeOnRelease, FlyOutPlacement placement = FlyOutPlacement.Above)
        {
            _position = position;
            _placement = placement;
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
            var size = Content?.MeasureLayout() ?? Size.Empty;
            var pos = new Rect(_position, size);
            if (_placement == FlyOutPlacement.Above)
                pos.Top -= size.Height;
            pos = Screen.ClampPopupRect(pos, Screen?.ScreenRect ?? rect, 0);
            Content?.UpdateLayout(pos);
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
