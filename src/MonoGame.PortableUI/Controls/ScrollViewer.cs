using System;
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
        private Control? _arrangedContent;
        private Rect _arrangedContentRect;
        private bool _contentArrangeValid;
        private PointF _touchStartPosition;
        private bool _isTouchPanning;

        /// <summary>Finger travel (design px) after which a touch counts as a pan and pressed children stop clicking.</summary>
        private const float TouchPanThreshold = 8f;
        private bool _isScrollBarDragging;
        private bool _isScrollBarThumbHovering;
        private bool _hasHorizontalScrollBar;
        private bool _hasVerticalScrollBar;
        private float _scrollBarDragPointerOffset;
        private readonly List<Control> _visualTreeScratch = new List<Control>();

        // Hover-sync fires on every wheel tick; the handlers never mutate the button list, so
        // one shared empty instance avoids a per-scroll allocation.
        private static readonly List<MouseButton> EmptyMouseButtons = new List<MouseButton>();

        private ScrollDirections _directions = ScrollDirections.Vertical;
        private bool _dragHorizontalThumb;

        /// <summary>Single-axis shorthand for <see cref="ScrollDirections"/> (kept for existing code).</summary>
        public Orientation ScrollOrientation
        {
            get => _directions == ScrollDirections.Horizontal ? Orientation.Horizontal : Orientation.Vertical;
            set => ScrollDirections = value == Orientation.Horizontal ? ScrollDirections.Horizontal : ScrollDirections.Vertical;
        }

        /// <summary>Which axes scroll; <see cref="Controls.ScrollDirections.Both"/> pans maps, large images, wide grids.</summary>
        public ScrollDirections ScrollDirections
        {
            get => _directions;
            set
            {
                if (_directions == value)
                    return;
                _directions = value;
                InvalidateLayout(true);
            }
        }

        private bool CanScrollX => _directions != ScrollDirections.Vertical;

        private bool CanScrollY => _directions != ScrollDirections.Horizontal;

        protected internal override bool ClipsDescendants => true;

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
            var theme = PortableTheme.ResolveCurrent();

            ShowScrollBars = true;
            EnableFling = true;
            EnableRubberBanding = true;
            FlingMultiplier = 6;
            RubberBandLimit = 48;
            ScrollBarThickness = theme.ScrollBarThickness;
            ScrollBarGutterBrush = theme.ScrollBarGutterBrush;
            ScrollBarBrush = theme.ScrollBarBrush;
            ScrollBarHoverBrush = theme.ScrollBarHoverBrush;
            ScrollBarPressedBrush = theme.ScrollBarPressedBrush;
            TouchDown += ScrollViewerTouchDown;
            TouchMove += ScrollViewerTouchMove;
            TouchUp += ScrollViewerTouchUp;
            TouchCancel += ScrollViewerTouchCancel;
            ScrollWheelChanged += ScrollViewerScrollWheelChanged;
            MouseEnter += ScrollViewerMouseEnter;
            MouseLeave += ScrollViewerMouseLeave;
            MouseDown += ScrollViewerMouseDown;
            MouseMove += ScrollViewerMouseMove;
            MouseUp += ScrollViewerMouseUp;
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

            if (ScrollBarThickness.Equals(oldTheme.ScrollBarThickness))
                ScrollBarThickness = newTheme.ScrollBarThickness;
            if (ReferenceEquals(ScrollBarGutterBrush, oldTheme.ScrollBarGutterBrush))
                ScrollBarGutterBrush = newTheme.ScrollBarGutterBrush;
            if (ReferenceEquals(ScrollBarBrush, oldTheme.ScrollBarBrush))
                ScrollBarBrush = newTheme.ScrollBarBrush;
            if (ReferenceEquals(ScrollBarHoverBrush, oldTheme.ScrollBarHoverBrush))
                ScrollBarHoverBrush = newTheme.ScrollBarHoverBrush;
            if (ReferenceEquals(ScrollBarPressedBrush, oldTheme.ScrollBarPressedBrush))
                ScrollBarPressedBrush = newTheme.ScrollBarPressedBrush;
        }

        private void ScrollViewerScrollWheelChanged(object? sender, ScrollWheelChangedEventArgs args)
        {
            var delta = -args.Delta / 4f;
            var before = Offset;
            // The vertical wheel scrolls Y when possible; horizontal wheel / Shift+wheel, or a
            // horizontal-only viewer, scroll X.
            var horizontal = args.IsHorizontal ? CanScrollX : !CanScrollY;
            if (horizontal)
                ScrollBy(new PointF(delta, 0), false);
            else
                ScrollBy(new PointF(0, delta), false);
            // Consume the tick only when it scrolled; at the limits it bubbles to an outer viewer.
            if (Offset != before)
                args.Handled = true;
            SynchronizeHoverAfterScroll(args.Position);
        }

        public void ScrollTo(PointF offset)
        {
            Offset = new PointF(
                CanScrollX ? Clamp(offset.X, 0, MaxHorizontalOffset) : 0,
                CanScrollY ? Clamp(offset.Y, 0, MaxVerticalOffset) : 0);

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

            var offsetX = Offset.X;
            if (targetRect.Left < viewportRect.Left)
                offsetX += targetRect.Left - viewportRect.Left;
            else if (targetRect.Right > viewportRect.Right)
                offsetX += targetRect.Right - viewportRect.Right;

            var offsetY = Offset.Y;
            if (targetRect.Top < viewportRect.Top)
                offsetY += targetRect.Top - viewportRect.Top;
            else if (targetRect.Bottom > viewportRect.Bottom)
                offsetY += targetRect.Bottom - viewportRect.Bottom;

            ScrollTo(new PointF(offsetX, offsetY));
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            UpdateViewportAndExtent();
            ClampOffset();
            // An arrange from the parent always lays the content out in full.
            _contentArrangeValid = false;
            UpdateContentLayout();
        }

        protected internal override void OnDrawOverlay(SpriteBatch spriteBatch, Rect rect)
        {
            DrawScrollBars(spriteBatch, rect - Padding);
            base.OnDrawOverlay(spriteBatch, rect);
        }

        protected internal override bool CapturesInputBeforeDescendants(BaseEventArgs args)
        {
            return args switch
            {
                MouseEventArgs mouseArgs => _isScrollBarDragging || IsScrollBarThumbHit(mouseArgs.Position),
                TouchEventArgs touchArgs => _isScrollBarDragging || IsScrollBarThumbHit(touchArgs.Position),
                _ => false,
            };
        }

        private void ScrollViewerTouchUp(object? sender, TouchEventArgs args)
        {
            if (_isScrollBarDragging)
            {
                DragScrollBarTo(args.Position);
                EndScrollBarDrag(args.Position);
                args.Handled = true;
                return;
            }

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
            if (_isScrollBarDragging)
            {
                DragScrollBarTo(args.Position);
                args.Handled = true;
                return;
            }

            if (_touchPosition != null)
            {
                if (!_isTouchPanning && Distance(args.Position, _touchStartPosition) > TouchPanThreshold)
                {
                    // The finger is panning, not tapping: the pressed child must not click on release.
                    _isTouchPanning = true;
                    VisualTreeHelper.CancelDescendantTouches(this, args);
                }

                _lastTouchDelta = args.Position - _touchPosition.Value;
                var before = Offset;
                ScrollBy(new PointF(-_lastTouchDelta.X, -_lastTouchDelta.Y), EnableRubberBanding);
                _touchPosition = args.Position;
                // Same as the wheel: an outer viewer only pans what this one could not.
                if (Offset != before)
                    args.Handled = true;
            }
        }

        // Touch has no capture: the finger leaving the viewer ends the gesture. Without this the
        // pan state, an over-scrolled offset or a thumb drag would survive into the next touch.
        private void ScrollViewerTouchCancel(object? sender, TouchEventArgs args)
        {
            if (_isScrollBarDragging)
                EndScrollBarDrag(args.Position);

            _touchPosition = null;
            _isTouchPanning = false;
            _lastTouchDelta = new PointF();
            ClampOffset();
            UpdateContentLayout();
        }

        private void ScrollViewerTouchDown(object? sender, TouchEventArgs args)
        {
            // A touch that starts on the scrollbar thumb drags the thumb; anywhere else pans the content.
            if (TryHitScrollBarThumb(args.Position, out var thumbRect, out var horizontal))
            {
                BeginThumbDrag(args.Position, thumbRect, horizontal);
                args.Handled = true;
                return;
            }

            BeginTouchPan(args.Position);
        }

        // Clickable children handle TouchDown before it bubbles here, so the pan starts in the
        // tunneling pre-pass instead; the bubbling handler above only covers direct calls.
        internal override void OnPreviewTouchDown(TouchEventArgs args)
        {
            if (TryHitScrollBarThumb(args.Position, out _, out _))
                return;
            BeginTouchPan(args.Position);
        }

        private void BeginTouchPan(PointF position)
        {
            _touchPosition = position;
            _touchStartPosition = position;
            _isTouchPanning = false;
            _lastTouchDelta = new PointF();
        }

        private static float Distance(PointF a, PointF b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
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
            if (!args.Buttons.Contains(MouseButton.Left) || !TryHitScrollBarThumb(args.Position, out var thumbRect, out var horizontal))
                return;

            BeginThumbDrag(args.Position, thumbRect, horizontal);
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

            Offset = new PointF(
                CanScrollX ? Clamp(Offset.X + delta.X, minOffset, maxHorizontal) : 0,
                CanScrollY ? Clamp(Offset.Y + delta.Y, minOffset, maxVertical) : 0);

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

            var measuredContent = Content.Measure();
            _hasVerticalScrollBar = CanShowScrollBars && CanScrollY && measuredContent.Height > viewportRect.Height;
            _hasHorizontalScrollBar = CanShowScrollBars && CanScrollX && measuredContent.Width > viewportRect.Width;
            // One bar takes room from the other axis, which can make that one overflow too.
            if (_hasVerticalScrollBar && !_hasHorizontalScrollBar && CanShowScrollBars && CanScrollX)
                _hasHorizontalScrollBar = measuredContent.Width > viewportRect.Width - ScrollBarThickness;
            if (_hasHorizontalScrollBar && !_hasVerticalScrollBar && CanShowScrollBars && CanScrollY)
                _hasVerticalScrollBar = measuredContent.Height > viewportRect.Height - ScrollBarThickness;

            var contentViewportRect = GetContentViewportRect(viewportRect, _hasVerticalScrollBar, _hasHorizontalScrollBar);
            Viewport = new Size(MathHelper.Max(0, contentViewportRect.Width), MathHelper.Max(0, contentViewportRect.Height));
            Extent = new Size(
                MathHelper.Max(Viewport.Width, measuredContent.Width),
                MathHelper.Max(Viewport.Height, measuredContent.Height));
        }

        private void ClampOffset()
        {
            Offset = new PointF(
                CanScrollX ? Clamp(Offset.X, 0, MaxHorizontalOffset) : 0,
                CanScrollY ? Clamp(Offset.Y, 0, MaxVerticalOffset) : 0);
        }

        private void UpdateContentLayout()
        {
            if (Content == null)
                return;

            var viewportRect = ContentViewportRect;
            var contentRect = new Rect(
                viewportRect.Left - Offset.X,
                viewportRect.Top - Offset.Y,
                CanScrollX ? Extent.Width : viewportRect.Width,
                CanScrollY ? Extent.Height : viewportRect.Height);

            // Scrolling only moves the content: when nothing inside invalidated since the last
            // full arrange and the slot size is unchanged, shift the arranged rects instead of
            // re-running measure and arrange for every item (touch pans do this each frame).
            if (_contentArrangeValid && ReferenceEquals(_arrangedContent, Content)
                && contentRect.Width.Equals(_arrangedContentRect.Width)
                && contentRect.Height.Equals(_arrangedContentRect.Height))
            {
                var delta = new PointF(contentRect.Left - _arrangedContentRect.Left, contentRect.Top - _arrangedContentRect.Top);
                if (delta.X != 0 || delta.Y != 0)
                    Content.OffsetArrangement(delta);
            }
            else
            {
                Content.UpdateLayout(contentRect);
                _arrangedContent = Content;
                _contentArrangeValid = true;
            }

            _arrangedContentRect = contentRect;
        }

        public override void InvalidateLayout(bool boundsChanged)
        {
            if (boundsChanged)
                _contentArrangeValid = false;
            base.InvalidateLayout(boundsChanged);
        }

        private void SynchronizeHoverAfterScroll(PointF position)
        {
            if (Content == null)
                return;

            var args = new MouseEventArgs(position, EmptyMouseButtons);
            _visualTreeScratch.Clear();
            VisualTreeHelper.AppendVisualTree(Content, _visualTreeScratch, false);
            foreach (var control in _visualTreeScratch)
            {
                var containsPosition = control.ClippingRect.Contains(position);
                if (containsPosition && !control.IsMouseHovering)
                    control.OnMouseEnter(args);
                else if (!containsPosition && control.IsMouseHovering)
                    control.OnMouseLeave(args);
            }
            _visualTreeScratch.Clear();
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

            gutterRect = new Rect(viewportRect.Right - ScrollBarThickness, viewportRect.Top, ScrollBarThickness, Viewport.Height);
            return true;
        }

        private bool TryGetHorizontalScrollGutterRect(Rect viewportRect, out Rect gutterRect)
        {
            gutterRect = Rect.Empty;
            if (!_hasHorizontalScrollBar || !CanShowScrollBars)
                return false;

            gutterRect = new Rect(viewportRect.Left, viewportRect.Bottom - ScrollBarThickness, Viewport.Width, ScrollBarThickness);
            return true;
        }

        /// <summary>Which thumb (if any) is under <paramref name="position"/>.</summary>
        private bool TryHitScrollBarThumb(PointF position, out Rect thumbRect, out bool horizontal)
        {
            var viewportRect = ViewportRect;
            if (TryGetVerticalScrollThumbRect(viewportRect, out thumbRect) && GetScrollBarThumbHitRect(thumbRect, false).Contains(position))
            {
                horizontal = false;
                return true;
            }

            if (TryGetHorizontalScrollThumbRect(viewportRect, out thumbRect) && GetScrollBarThumbHitRect(thumbRect, true).Contains(position))
            {
                horizontal = true;
                return true;
            }

            horizontal = false;
            return false;
        }

        private void BeginThumbDrag(PointF position, Rect thumbRect, bool horizontal)
        {
            _isScrollBarDragging = true;
            _dragHorizontalThumb = horizontal;
            _scrollBarDragPointerOffset = horizontal ? position.X - thumbRect.Left : position.Y - thumbRect.Top;
            SetScrollBarThumbHovering(true);
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

        private bool IsScrollBarThumbHit(PointF position) => TryHitScrollBarThumb(position, out _, out _);

        private Rect GetScrollBarThumbHitRect(Rect thumbRect, bool horizontal)
        {
            var viewportRect = ViewportRect;
            var hitThickness = MathHelper.Max(ScrollBarThickness, MinimumScrollBarHitThickness);
            if (horizontal)
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
            if (_dragHorizontalThumb)
            {
                if (!TryGetHorizontalScrollThumbRect(viewportRect, out var thumbRect))
                    return;

                var travel = Viewport.Width - thumbRect.Width;
                var left = Clamp(position.X - _scrollBarDragPointerOffset, viewportRect.Left, viewportRect.Left + travel);
                var offset = travel <= 0 || MaxHorizontalOffset <= 0 ? 0 : (left - viewportRect.Left) / travel * MaxHorizontalOffset;
                ScrollTo(new PointF(offset, Offset.Y));
                return;
            }

            if (!TryGetVerticalScrollThumbRect(viewportRect, out var verticalThumbRect))
                return;

            var verticalTravel = Viewport.Height - verticalThumbRect.Height;
            var top = Clamp(position.Y - _scrollBarDragPointerOffset, viewportRect.Top, viewportRect.Top + verticalTravel);
            var verticalOffset = verticalTravel <= 0 || MaxVerticalOffset <= 0 ? 0 : (top - viewportRect.Top) / verticalTravel * MaxVerticalOffset;
            ScrollTo(new PointF(Offset.X, verticalOffset));
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
