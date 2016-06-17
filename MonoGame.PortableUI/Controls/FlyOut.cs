using System;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    public class FlyOut : ContentControl, IDisposable
    {
        private readonly PointF _position;

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
            Content.UpdateLayout(pos);
        }

        public void Dispose()
        {
            Content = null;
        }
    }
}