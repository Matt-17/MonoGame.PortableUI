using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class ScrollViewer : ContentControl
    {
        private PointF? _touchPosition;
        private PointF _lastTouchDelta;

        public Orientation ScrollOrientation { get; set; }

        public Size Viewport { get; private set; }
        public Size Extent { get; private set; }
        public PointF Offset { get; private set; }

        public bool ShowScrollBars { get; set; }
        public bool EnableFling { get; set; }
        public bool EnableRubberBanding { get; set; }
        public float FlingMultiplier { get; set; }
        public float RubberBandLimit { get; set; }
        public float ScrollBarThickness { get; set; }
        public Brush ScrollBarBrush { get; set; }

        public ScrollViewer()
        {
            ShowScrollBars = true;
            EnableFling = true;
            EnableRubberBanding = true;
            FlingMultiplier = 6;
            RubberBandLimit = 48;
            ScrollBarThickness = 4;
            ScrollBarBrush = new SolidColorBrush(new Color(0, 0, 0, 120));
            TouchDown += ScrollViewerTouchDown;
            TouchMove += ScrollViewerTouchMove;
            TouchUp += ScrollViewerTouchUp;
            ScrollWheelChanged += ScrollViewerScrollWheelChanged;
        }

        private void ScrollViewerScrollWheelChanged(object sender, ScrollWheelChangedEventArgs args)
        {
            var delta = -args.Delta / 4f;
            if (ScrollOrientation == Orientation.Horizontal)
                ScrollBy(new PointF(delta, 0), false);
            else
                ScrollBy(new PointF(0, delta), false);
        }

        public void ScrollTo(PointF offset)
        {
            if (ScrollOrientation == Orientation.Horizontal)
            {
                Offset = new PointF(Clamp(offset.X, 0, MaxHorizontalOffset), 0);
            }
            else
            {
                Offset = new PointF(0, Clamp(offset.Y, 0, MaxVerticalOffset));
            }

            UpdateContentLayout();
        }

        public void ScrollBy(PointF delta)
        {
            ScrollBy(delta, false);
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            UpdateViewportAndExtent();
            ClampOffset();
            UpdateContentLayout();
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            DrawScrollBars(spriteBatch, rect - Margin - Padding);
        }

        private void ScrollViewerTouchUp(object sender, TouchEventArgs args)
        {
            _touchPosition = null;
            if (EnableFling)
            {
                ScrollBy(new PointF(-_lastTouchDelta.X * FlingMultiplier, -_lastTouchDelta.Y * FlingMultiplier), false);
            }
            else
            {
                ClampOffset();
                UpdateContentLayout();
            }
        }

        private void ScrollViewerTouchMove(object sender, TouchEventArgs args)
        {
            if (_touchPosition != null)
            {
                _lastTouchDelta = args.Position - _touchPosition.Value;
                ScrollBy(new PointF(-_lastTouchDelta.X, -_lastTouchDelta.Y), EnableRubberBanding);
                _touchPosition = args.Position;
            }
        }

        private void ScrollViewerTouchDown(object sender, TouchEventArgs args)
        {
            _touchPosition = args.Position;
            _lastTouchDelta = new PointF();
        }

        private float MaxHorizontalOffset => MathHelper.Max(0, Extent.Width - Viewport.Width);

        private float MaxVerticalOffset => MathHelper.Max(0, Extent.Height - Viewport.Height);

        private void ScrollBy(PointF delta, bool allowOverscroll)
        {
            var minOffset = allowOverscroll ? -RubberBandLimit : 0;
            var maxHorizontal = MaxHorizontalOffset + (allowOverscroll ? RubberBandLimit : 0);
            var maxVertical = MaxVerticalOffset + (allowOverscroll ? RubberBandLimit : 0);

            if (ScrollOrientation == Orientation.Horizontal)
                Offset = new PointF(Clamp(Offset.X + delta.X, minOffset, maxHorizontal), 0);
            else
                Offset = new PointF(0, Clamp(Offset.Y + delta.Y, minOffset, maxVertical));

            UpdateContentLayout();
        }

        private void UpdateViewportAndExtent()
        {
            var viewportRect = BoundingRect - Margin - Padding;
            Viewport = new Size(MathHelper.Max(0, viewportRect.Width), MathHelper.Max(0, viewportRect.Height));

            if (Content == null)
            {
                Extent = Size.Empty;
                Offset = new PointF();
                return;
            }

            var measuredContent = Content.MeasureLayout();
            Extent = new Size(
                MathHelper.Max(Viewport.Width, measuredContent.Width),
                MathHelper.Max(Viewport.Height, measuredContent.Height));
        }

        private void ClampOffset()
        {
            if (ScrollOrientation == Orientation.Horizontal)
                Offset = new PointF(Clamp(Offset.X, 0, MaxHorizontalOffset), 0);
            else
                Offset = new PointF(0, Clamp(Offset.Y, 0, MaxVerticalOffset));
        }

        private void UpdateContentLayout()
        {
            if (Content == null)
                return;

            var viewportRect = BoundingRect - Margin - Padding;
            var contentRect = new Rect(
                viewportRect.Left - Offset.X,
                viewportRect.Top - Offset.Y,
                ScrollOrientation == Orientation.Horizontal ? Extent.Width : viewportRect.Width,
                ScrollOrientation == Orientation.Vertical ? Extent.Height : viewportRect.Height);

            Content.UpdateLayout(contentRect);
        }

        private void DrawScrollBars(SpriteBatch spriteBatch, Rect viewportRect)
        {
            if (!ShowScrollBars || ScrollBarBrush == null)
                return;

            if (ScrollOrientation == Orientation.Vertical && Extent.Height > Viewport.Height && Viewport.Height > 0)
            {
                var thumbHeight = MathHelper.Max(18, Viewport.Height * Viewport.Height / Extent.Height);
                var travel = Viewport.Height - thumbHeight;
                var top = viewportRect.Top + (MaxVerticalOffset == 0 ? 0 : Offset.Y / MaxVerticalOffset * travel);
                ScrollBarBrush.Draw(spriteBatch, new Rect(viewportRect.Right - ScrollBarThickness, top, ScrollBarThickness, thumbHeight));
            }

            if (ScrollOrientation == Orientation.Horizontal && Extent.Width > Viewport.Width && Viewport.Width > 0)
            {
                var thumbWidth = MathHelper.Max(18, Viewport.Width * Viewport.Width / Extent.Width);
                var travel = Viewport.Width - thumbWidth;
                var left = viewportRect.Left + (MaxHorizontalOffset == 0 ? 0 : Offset.X / MaxHorizontalOffset * travel);
                ScrollBarBrush.Draw(spriteBatch, new Rect(left, viewportRect.Bottom - ScrollBarThickness, thumbWidth, ScrollBarThickness));
            }
        }

        private static float Clamp(float value, float min, float max)
        {
            if (max < min)
                max = min;
            return MathHelper.Clamp(value, min, max);
        }
    }
}
