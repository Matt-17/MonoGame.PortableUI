using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class ScrollViewer : ContentControl
    {
        private const float MinimumScrollBarHitThickness = 12;
        private PointF? _touchPosition;
        private PointF _lastTouchDelta;
        private bool _isScrollBarDragging;
        private bool _isScrollBarThumbHovering;
        private bool _hasHorizontalScrollBar;
        private bool _hasVerticalScrollBar;
        private float _scrollBarDragPointerOffset;

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
        public Brush? ScrollBarGutterBrush { get; set; }
        public Brush ScrollBarBrush { get; set; }
        public Brush? ScrollBarHoverBrush { get; set; }
        public Brush? ScrollBarPressedBrush { get; set; }

        public ScrollViewer()
        {
            ShowScrollBars = true;
            EnableFling = true;
            EnableRubberBanding = true;
            FlingMultiplier = 6;
            RubberBandLimit = 48;
            ScrollBarThickness = 8;
            ScrollBarGutterBrush = new SolidColorBrush(new Color(245, 245, 245));
            ScrollBarBrush = new SolidColorBrush(new Color(0, 0, 0, 120));
            ScrollBarHoverBrush = new SolidColorBrush(new Color(0, 0, 0, 160));
            ScrollBarPressedBrush = new SolidColorBrush(new Color(0, 0, 0, 190));
            TouchDown += ScrollViewerTouchDown;
            TouchMove += ScrollViewerTouchMove;
            TouchUp += ScrollViewerTouchUp;
            ScrollWheelChanged += ScrollViewerScrollWheelChanged;
            MouseEnter += ScrollViewerMouseEnter;
            MouseLeave += ScrollViewerMouseLeave;
            MouseDown += ScrollViewerMouseDown;
            MouseMove += ScrollViewerMouseMove;
            MouseUp += ScrollViewerMouseUp;
        }

        private void ScrollViewerScrollWheelChanged(object? sender, ScrollWheelChangedEventArgs args)
        {
            var delta = -args.Delta / 4f;
            if (ScrollOrientation == Orientation.Horizontal)
                ScrollBy(new PointF(delta, 0), false);
            else
                ScrollBy(new PointF(0, delta), false);
            SynchronizeHoverAfterScroll(args.Position);
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

        public void BringIntoView(Control control)
        {
            var viewportRect = ContentViewportRect;
            var targetRect = control.BoundingRect;

            if (ScrollOrientation == Orientation.Horizontal)
            {
                var offsetX = Offset.X;
                if (targetRect.Left < viewportRect.Left)
                    offsetX += targetRect.Left - viewportRect.Left;
                else if (targetRect.Right > viewportRect.Right)
                    offsetX += targetRect.Right - viewportRect.Right;

                ScrollTo(new PointF(offsetX, 0));
                return;
            }

            var offsetY = Offset.Y;
            if (targetRect.Top < viewportRect.Top)
                offsetY += targetRect.Top - viewportRect.Top;
            else if (targetRect.Bottom > viewportRect.Bottom)
                offsetY += targetRect.Bottom - viewportRect.Bottom;

            ScrollTo(new PointF(0, offsetY));
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
        }

        protected internal override void OnDrawOverlay(SpriteBatch spriteBatch, Rect rect)
        {
            DrawScrollBars(spriteBatch, rect - Padding);
            base.OnDrawOverlay(spriteBatch, rect);
        }

        protected internal override bool CapturesInputBeforeDescendants(BaseEventArgs args)
        {
            return args is MouseEventArgs mouseArgs
                && (_isScrollBarDragging || IsScrollBarThumbHit(mouseArgs.Position));
        }

        private void ScrollViewerTouchUp(object? sender, TouchEventArgs args)
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

        private void ScrollViewerTouchMove(object? sender, TouchEventArgs args)
        {
            if (_touchPosition != null)
            {
                _lastTouchDelta = args.Position - _touchPosition.Value;
                ScrollBy(new PointF(-_lastTouchDelta.X, -_lastTouchDelta.Y), EnableRubberBanding);
                _touchPosition = args.Position;
            }
        }

        private void ScrollViewerTouchDown(object? sender, TouchEventArgs args)
        {
            _touchPosition = args.Position;
            _lastTouchDelta = new PointF();
        }

        private void ScrollViewerMouseEnter(object? sender, MouseEventArgs args)
        {
            UpdateScrollBarThumbHover(args.Position);
        }

        private void ScrollViewerMouseLeave(object? sender, MouseEventArgs args)
        {
            if (!_isScrollBarDragging)
                SetScrollBarThumbHovering(false);
        }

        private void ScrollViewerMouseDown(object? sender, MouseEventArgs args)
        {
            if (!args.Buttons.Contains(MouseButton.Left) || !TryGetScrollBarThumbRect(out var thumbRect))
                return;

            var hitRect = GetScrollBarThumbHitRect(thumbRect);
            if (!hitRect.Contains(args.Position))
                return;

            _isScrollBarDragging = true;
            _scrollBarDragPointerOffset = ScrollOrientation == Orientation.Horizontal
                ? args.Position.X - thumbRect.Left
                : args.Position.Y - thumbRect.Top;
            SetScrollBarThumbHovering(true);
            Screen?.CaptureMouse(this);
            args.Handled = true;
        }

        private void ScrollViewerMouseMove(object? sender, MouseEventArgs args)
        {
            if (!_isScrollBarDragging)
            {
                UpdateScrollBarThumbHover(args.Position);
                return;
            }

            if (!args.Buttons.Contains(MouseButton.Left))
            {
                EndScrollBarDrag(args.Position);
                args.Handled = true;
                return;
            }

            DragScrollBarTo(args.Position);
            args.Handled = true;
        }

        private void ScrollViewerMouseUp(object? sender, MouseEventArgs args)
        {
            if (!_isScrollBarDragging)
                return;

            DragScrollBarTo(args.Position);
            EndScrollBarDrag(args.Position);
            args.Handled = true;
        }

        private float MaxHorizontalOffset => MathHelper.Max(0, Extent.Width - Viewport.Width);

        private float MaxVerticalOffset => MathHelper.Max(0, Extent.Height - Viewport.Height);

        private Rect ViewportRect => BoundingRect - Margin - Padding;

        private Rect ContentViewportRect => GetContentViewportRect(ViewportRect, _hasVerticalScrollBar, _hasHorizontalScrollBar);

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
            if (Content == null)
            {
                Viewport = new Size(MathHelper.Max(0, viewportRect.Width), MathHelper.Max(0, viewportRect.Height));
                Extent = Size.Empty;
                Offset = new PointF();
                _hasHorizontalScrollBar = false;
                _hasVerticalScrollBar = false;
                return;
            }

            var measuredContent = Content.MeasureLayout();
            _hasVerticalScrollBar = CanShowScrollBars
                && ScrollOrientation == Orientation.Vertical
                && measuredContent.Height > viewportRect.Height;
            _hasHorizontalScrollBar = CanShowScrollBars
                && ScrollOrientation == Orientation.Horizontal
                && measuredContent.Width > viewportRect.Width;

            var contentViewportRect = GetContentViewportRect(viewportRect, _hasVerticalScrollBar, _hasHorizontalScrollBar);
            Viewport = new Size(MathHelper.Max(0, contentViewportRect.Width), MathHelper.Max(0, contentViewportRect.Height));
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

            var viewportRect = ContentViewportRect;
            var contentRect = new Rect(
                viewportRect.Left - Offset.X,
                viewportRect.Top - Offset.Y,
                ScrollOrientation == Orientation.Horizontal ? Extent.Width : viewportRect.Width,
                ScrollOrientation == Orientation.Vertical ? Extent.Height : viewportRect.Height);

            Content.UpdateLayout(contentRect);
        }

        private void SynchronizeHoverAfterScroll(PointF position)
        {
            if (Content == null)
                return;

            var args = new MouseEventArgs(position, new List<MouseButton>());
            foreach (var control in VisualTreeHelper.GetVisualTreeAsList(Content, false))
            {
                var containsPosition = control.BoundingRect.Contains(position);
                if (containsPosition && !control.IsMouseHovering)
                    control.OnMouseEnter(args);
                else if (!containsPosition && control.IsMouseHovering)
                    control.OnMouseLeave(args);
            }
        }

        private void DrawScrollBars(SpriteBatch spriteBatch, Rect viewportRect)
        {
            if (!CanShowScrollBars)
                return;

            if (ScrollBarGutterBrush != null)
            {
                if (TryGetVerticalScrollGutterRect(viewportRect, out var verticalGutterRect))
                    ScrollBarGutterBrush.Draw(spriteBatch, verticalGutterRect, RenderOpacity);

                if (TryGetHorizontalScrollGutterRect(viewportRect, out var horizontalGutterRect))
                    ScrollBarGutterBrush.Draw(spriteBatch, horizontalGutterRect, RenderOpacity);
            }

            var scrollBarBrush = CurrentScrollBarBrush;
            if (scrollBarBrush == null)
                return;

            if (TryGetVerticalScrollThumbRect(viewportRect, out var verticalThumbRect))
                scrollBarBrush.Draw(spriteBatch, verticalThumbRect, RenderOpacity);

            if (TryGetHorizontalScrollThumbRect(viewportRect, out var horizontalThumbRect))
                scrollBarBrush.Draw(spriteBatch, horizontalThumbRect, RenderOpacity);
        }

        private bool CanShowScrollBars => ShowScrollBars && ScrollBarBrush != null && ScrollBarThickness > 0;

        internal Brush? CurrentScrollBarBrush
        {
            get
            {
                if (_isScrollBarDragging)
                    return ScrollBarPressedBrush ?? ScrollBarHoverBrush ?? ScrollBarBrush;
                if (_isScrollBarThumbHovering)
                    return ScrollBarHoverBrush ?? ScrollBarBrush;
                return ScrollBarBrush;
            }
        }

        private bool TryGetVerticalScrollGutterRect(Rect viewportRect, out Rect gutterRect)
        {
            gutterRect = Rect.Empty;
            if (!_hasVerticalScrollBar || !CanShowScrollBars)
                return false;

            gutterRect = new Rect(viewportRect.Right - ScrollBarThickness, viewportRect.Top, ScrollBarThickness, viewportRect.Height);
            return true;
        }

        private bool TryGetHorizontalScrollGutterRect(Rect viewportRect, out Rect gutterRect)
        {
            gutterRect = Rect.Empty;
            if (!_hasHorizontalScrollBar || !CanShowScrollBars)
                return false;

            gutterRect = new Rect(viewportRect.Left, viewportRect.Bottom - ScrollBarThickness, viewportRect.Width, ScrollBarThickness);
            return true;
        }

        private bool TryGetScrollBarThumbRect(out Rect thumbRect)
        {
            var viewportRect = ViewportRect;
            if (ScrollOrientation == Orientation.Horizontal)
                return TryGetHorizontalScrollThumbRect(viewportRect, out thumbRect);
            return TryGetVerticalScrollThumbRect(viewportRect, out thumbRect);
        }

        private bool TryGetVerticalScrollThumbRect(Rect viewportRect, out Rect thumbRect)
        {
            thumbRect = Rect.Empty;
            if (!_hasVerticalScrollBar || !CanShowScrollBars || Viewport.Height <= 0)
                return false;

            var thumbHeight = MathHelper.Max(18, Viewport.Height * Viewport.Height / Extent.Height);
            var travel = Viewport.Height - thumbHeight;
            var top = viewportRect.Top + (MaxVerticalOffset == 0 ? 0 : Offset.Y / MaxVerticalOffset * travel);
            thumbRect = new Rect(viewportRect.Right - ScrollBarThickness, top, ScrollBarThickness, thumbHeight);
            return true;
        }

        private bool TryGetHorizontalScrollThumbRect(Rect viewportRect, out Rect thumbRect)
        {
            thumbRect = Rect.Empty;
            if (!_hasHorizontalScrollBar || !CanShowScrollBars || Viewport.Width <= 0)
                return false;

            var thumbWidth = MathHelper.Max(18, Viewport.Width * Viewport.Width / Extent.Width);
            var travel = Viewport.Width - thumbWidth;
            var left = viewportRect.Left + (MaxHorizontalOffset == 0 ? 0 : Offset.X / MaxHorizontalOffset * travel);
            thumbRect = new Rect(left, viewportRect.Bottom - ScrollBarThickness, thumbWidth, ScrollBarThickness);
            return true;
        }

        private bool IsScrollBarThumbHit(PointF position)
        {
            if (!TryGetScrollBarThumbRect(out var thumbRect))
                return false;

            return GetScrollBarThumbHitRect(thumbRect).Contains(position);
        }

        private Rect GetScrollBarThumbHitRect(Rect thumbRect)
        {
            var viewportRect = ViewportRect;
            var hitThickness = MathHelper.Max(ScrollBarThickness, MinimumScrollBarHitThickness);
            if (ScrollOrientation == Orientation.Horizontal)
            {
                var top = MathHelper.Max(viewportRect.Top, viewportRect.Bottom - hitThickness);
                return new Rect(thumbRect.Left, top, thumbRect.Width, viewportRect.Bottom - top);
            }

            var left = MathHelper.Max(viewportRect.Left, viewportRect.Right - hitThickness);
            return new Rect(left, thumbRect.Top, viewportRect.Right - left, thumbRect.Height);
        }

        private void DragScrollBarTo(PointF position)
        {
            var viewportRect = ViewportRect;
            if (ScrollOrientation == Orientation.Horizontal)
            {
                if (!TryGetHorizontalScrollThumbRect(viewportRect, out var thumbRect))
                    return;

                var travel = viewportRect.Width - thumbRect.Width;
                var left = Clamp(position.X - _scrollBarDragPointerOffset, viewportRect.Left, viewportRect.Left + travel);
                var offset = travel <= 0 || MaxHorizontalOffset <= 0 ? 0 : (left - viewportRect.Left) / travel * MaxHorizontalOffset;
                ScrollTo(new PointF(offset, 0));
                return;
            }

            if (!TryGetVerticalScrollThumbRect(viewportRect, out var verticalThumbRect))
                return;

            var verticalTravel = viewportRect.Height - verticalThumbRect.Height;
            var top = Clamp(position.Y - _scrollBarDragPointerOffset, viewportRect.Top, viewportRect.Top + verticalTravel);
            var verticalOffset = verticalTravel <= 0 || MaxVerticalOffset <= 0 ? 0 : (top - viewportRect.Top) / verticalTravel * MaxVerticalOffset;
            ScrollTo(new PointF(0, verticalOffset));
        }

        private void EndScrollBarDrag(PointF position)
        {
            _isScrollBarDragging = false;
            Screen?.ReleaseMouse(this);
            UpdateScrollBarThumbHover(position);
        }

        private void UpdateScrollBarThumbHover(PointF position)
        {
            SetScrollBarThumbHovering(IsScrollBarThumbHit(position));
        }

        private void SetScrollBarThumbHovering(bool isHovering)
        {
            if (_isScrollBarThumbHovering == isHovering)
                return;

            _isScrollBarThumbHovering = isHovering;
            InvalidateLayout(false);
        }

        private Rect GetContentViewportRect(Rect viewportRect, bool hasVerticalScrollBar, bool hasHorizontalScrollBar)
        {
            if (hasVerticalScrollBar)
                viewportRect.Width = MathHelper.Max(0, viewportRect.Width - ScrollBarThickness);
            if (hasHorizontalScrollBar)
                viewportRect.Height = MathHelper.Max(0, viewportRect.Height - ScrollBarThickness);
            return viewportRect;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (max < min)
                max = min;
            return MathHelper.Clamp(value, min, max);
        }
    }
}
