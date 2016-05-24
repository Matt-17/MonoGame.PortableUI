using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class ScrollViewer : ContentControl
    {
        private Point? _mouse;
        private PointF _offset;
        public Orientation ScrollOrientation { get; set; }
        public ScrollViewer()
        {
            MouseLeftDown += ScrollViewer_MouseLeftDown;
            MouseMove += ScrollViewer_MouseMove;
            MouseLeftUp += ScrollViewer_MouseLeftUp;
        }

        private void ScrollViewer_MouseLeftUp(object sender, MouseButtonEventHandlerArgs args)
        {
            _mouse = null;
        }

        private void ScrollViewer_MouseMove(object sender, MouseMoveEventHandlerArgs args)
        {
            if (_mouse != null)
            {
                var point = args.AbsolutePoint;
                var diff = point - _mouse.Value;
                if (ScrollOrientation == Orientation.Horizontal)
                    _offset.X += diff.X;
                else
                    _offset.Y += diff.Y;
                _mouse = point;
            }
        }

        private void ScrollViewer_MouseLeftDown(object sender, MouseButtonEventHandlerArgs args)
        {
            _mouse = args.AbsolutePoint;
        }

        public override Size MeasureLayout()
        {
            return base.MeasureLayout();
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
        }

        protected internal override void OnAfterDraw(SpriteBatch spriteBatch, Rect renderedBoundingRect)
        {
            base.OnAfterDraw(spriteBatch, renderedBoundingRect);
            Content?.Draw(spriteBatch, Content.BoundingRect + _offset);
        }
    }
}