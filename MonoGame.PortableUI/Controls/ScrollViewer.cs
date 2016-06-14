using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    public class ScrollViewer : ContentControl
    {
        private Point? _mouse;
        private PointF _offset;
        public Orientation ScrollOrientation { get; set; }
        public ScrollViewer()
        {
            TouchDown += ScrollViewer_MouseLeftDown;
            TouchDown += ScrollViewer_MouseMove;
            TouchDown += ScrollViewer_MouseLeftUp;
            ScrollWheelChanged += ScrollViewer_ScrollWheelChanged;
        }

        private void ScrollViewer_ScrollWheelChanged(object sender, ScrollWheelChangedEventHandlerArgs args)
        {
            var d = args.Delta/50;
            if (ScrollOrientation == Orientation.Horizontal)
                _offset.X += d;
            else
                _offset.Y += d;

            var boundingRect = Content.BoundingRect;
            boundingRect.Offset = _offset;
            if (ScrollOrientation == Orientation.Horizontal)
                boundingRect.Width = Size.Infinity;
            else
                boundingRect.Height = Size.Infinity;
            Content.UpdateLayout(boundingRect);
        }

        private void ScrollViewer_MouseLeftUp(object sender, TouchEventHandlerArgs args)
        {
            _mouse = null;
        }

        private void ScrollViewer_MouseMove(object sender, TouchEventHandlerArgs args)
        {
            if (_mouse != null)
            {
                var point = args.Position;
                var diff = point - _mouse.Value;
                if (ScrollOrientation == Orientation.Horizontal)
                    _offset.X += diff.X;
                else
                    _offset.Y += diff.Y;
                var boundingRect = Content.BoundingRect;
                boundingRect.Offset = _offset;
                if (ScrollOrientation == Orientation.Horizontal)
                    boundingRect.Width = Size.Infinity;
                else
                    boundingRect.Height = Size.Infinity;
                Content.UpdateLayout(boundingRect);
                _mouse = point;
            }
        }

        private void ScrollViewer_MouseLeftDown(object sender, TouchEventHandlerArgs args)
        {
            _mouse = args.Position;
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            _offset = Content.BoundingRect.Offset;
        }
    }
}