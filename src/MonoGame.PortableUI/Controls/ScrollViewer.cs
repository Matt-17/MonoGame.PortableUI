using System.Diagnostics;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    public class ScrollViewer : ContentControl
    {
        private PointF? _mouse;
        private PointF _offset;
        public Orientation ScrollOrientation { get; set; }
        public ScrollViewer()
        {
            TouchDown += ScrollViewerTouchDown;
            TouchMove += ScrollViewerTouchMove;
            TouchUp += ScrollViewerTouchUp;
            ScrollWheelChanged += ScrollViewerScrollWheelChanged;
        }

        private void ScrollViewerScrollWheelChanged(object sender, ScrollWheelChangedEventArgs args)
        {
            var d = args.Delta / 10f;
            UpdatePosition(new PointF(d, d));
        }

        private void UpdatePosition(PointF p)
        {
            var boundingRect = Content.BoundingRect;
            boundingRect.Offset -= _offset;
            if (ScrollOrientation == Orientation.Horizontal)
            {
                var x = _offset.X + p.X;
                x = MathHelper.Clamp(x, -boundingRect.Width, 0);
                _offset.X = x;
            }
            else
            {
                var y = _offset.Y + p.Y;
                y = MathHelper.Clamp(y, -boundingRect.Height, 0);
                _offset.Y = y;
            }

            Debug.WriteLine($"{_offset}");

            boundingRect.Offset += _offset;
            if (ScrollOrientation == Orientation.Horizontal)
                boundingRect.Width = Size.Infinity;
            else
                boundingRect.Height = Size.Infinity;
            Content.UpdateLayout(boundingRect);
        }

        public override void UpdateLayout(Rect rect)
        {
        //    var boundingRect = Content.BoundingRect;
            //boundingRect.Offset -= _offset;
            base.UpdateLayout(rect);
            //boundingRect.Offset += _offset;
         //   Content.UpdateLayout(rect - _offset);

        }

        private void ScrollViewerTouchUp(object sender, TouchEventArgs args)
        {
            _mouse = null;
        }

        private void ScrollViewerTouchMove(object sender, TouchEventArgs args)
        {
            if (_mouse != null)
            {
                UpdatePosition(args.Position - _mouse.Value);
                _mouse = args.Position;
            }
        }

        private void ScrollViewerTouchDown(object sender, TouchEventArgs args)
        {
            _mouse = args.Position;
        }
    }
}