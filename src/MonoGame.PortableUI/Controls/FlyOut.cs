using System;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    public class FlyOut : ContentControl, IDisposable
    {
        private readonly PointF _position;
        private bool _isOpen;

        public event EventHandler Showing;
        public event EventHandler Shown;
        public event EventHandler Dismissing;
        public event EventHandler Dismissed;

        public FlyOut(PointF position, bool removeOnRelease)
        {
            _position = position;
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
            var size = Content.MeasureLayout();
            var pos = new Rect(_position, size);
            pos.Top -= size.Height;
            var overflow = pos.Right - Screen.ScreenRect.Width;
            if (overflow > 0)
                pos.Left -= overflow;
            Content.UpdateLayout(pos);
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
