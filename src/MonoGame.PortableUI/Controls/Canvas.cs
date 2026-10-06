using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     Places children at coordinates: <see cref="SetLeft"/>/<see cref="SetTop"/> (or
    ///     <see cref="SetRight"/>/<see cref="SetBottom"/> from the far edges) in design pixels from the
    ///     canvas's content box, each child at its own desired size. <see cref="SetAnchor"/> picks which point
    ///     of the child sits on that position - (0.5, 0.5) centres a marker on a map point or a label on a
    ///     projected 3D position.
    ///     <para>
    ///         As in WPF the canvas does not grow with its children (set Width/Height or stretch it) and does
    ///         not clip them. Moving a child is cheap: while nothing inside changed size, a new position only
    ///         shifts the arranged child (like scrolling) instead of running a layout pass, so markers can
    ///         follow objects every frame.
    ///     </para>
    /// </summary>
    public class Canvas : Panel
    {
        private sealed class CanvasPosition
        {
            public float Left = float.NaN;
            public float Top = float.NaN;
            public float Right = float.NaN;
            public float Bottom = float.NaN;
            public Vector2 Anchor;
            // Where the child's slot was arranged last (top-left of its margin box).
            public PointF ArrangedOrigin;
        }

        private Rect _content;
        private bool _arrangeValid;

        private static CanvasPosition Slot(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);
            return (CanvasPosition)(control.CanvasPositionSlot ??= new CanvasPosition());
        }

        /// <summary>Distance of the child's anchor from the content box's left edge (NaN = unset).</summary>
        public static void SetLeft(Control control, float left)
        {
            Slot(control).Left = left;
            Moved(control);
        }

        /// <summary>Distance of the child's anchor from the content box's top edge (NaN = unset).</summary>
        public static void SetTop(Control control, float top)
        {
            Slot(control).Top = top;
            Moved(control);
        }

        /// <summary>Distance of the child's right edge from the content box's right edge; used when Left is unset.</summary>
        public static void SetRight(Control control, float right)
        {
            Slot(control).Right = right;
            Moved(control);
        }

        /// <summary>Distance of the child's bottom edge from the content box's bottom edge; used when Top is unset.</summary>
        public static void SetBottom(Control control, float bottom)
        {
            Slot(control).Bottom = bottom;
            Moved(control);
        }

        /// <summary>Left and top in one call (one move instead of two), e.g. every frame for a tracked marker.</summary>
        public static void SetPosition(Control control, Vector2 position)
        {
            var slot = Slot(control);
            slot.Left = position.X;
            slot.Top = position.Y;
            Moved(control);
        }

        /// <summary>
        ///     Which point of the child sits on its Left/Top position, as a fraction of its size:
        ///     (0, 0) top-left (default), (0.5, 0.5) centre, (0.5, 1) bottom centre (a pin).
        /// </summary>
        public static void SetAnchor(Control control, Vector2 anchor)
        {
            Slot(control).Anchor = anchor;
            Moved(control);
        }

        public static float GetLeft(Control control) => (control.CanvasPositionSlot as CanvasPosition)?.Left ?? float.NaN;
        public static float GetTop(Control control) => (control.CanvasPositionSlot as CanvasPosition)?.Top ?? float.NaN;
        public static float GetRight(Control control) => (control.CanvasPositionSlot as CanvasPosition)?.Right ?? float.NaN;
        public static float GetBottom(Control control) => (control.CanvasPositionSlot as CanvasPosition)?.Bottom ?? float.NaN;
        public static Vector2 GetAnchor(Control control) => (control.CanvasPositionSlot as CanvasPosition)?.Anchor ?? Vector2.Zero;

        /// <summary>Adds <paramref name="child"/> with its anchor at (<paramref name="left"/>, <paramref name="top"/>).</summary>
        public void AddChild(Control child, float left, float top)
        {
            var position = Slot(child);
            position.Left = left;
            position.Top = top;
            Children.Add(child);
        }

        private static void Moved(Control control)
        {
            if (control.Parent is Canvas canvas)
                canvas.OnChildMoved(control);
        }

        private void OnChildMoved(Control child)
        {
            if (!_arrangeValid || child.IsGone)
            {
                InvalidateLayout(true);
                return;
            }

            // Fast path: the child's size and the canvas's box are unchanged, so moving it is a shift
            // of its arranged subtree - no measure, no arrange, no layout pass.
            var position = Slot(child);
            var origin = SlotOrigin(child, position, child.Measure());
            var delta = new PointF(origin.X - position.ArrangedOrigin.X, origin.Y - position.ArrangedOrigin.Y);
            if (delta.X == 0 && delta.Y == 0)
                return;
            child.OffsetArrangement(delta);
            position.ArrangedOrigin = origin;
            Screen?.MarkHoverStale();
            RequestRedraw();
        }

        public override void InvalidateLayout(bool boundsChanged)
        {
            // A size change inside (or of the canvas) needs a real arrange before shifting again.
            if (boundsChanged)
                _arrangeValid = false;
            base.InvalidateLayout(boundsChanged);
        }

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;
            // Children get their desired sizes; the canvas itself keeps its own size (WPF semantics).
            for (var i = 0; i < Children.Count; i++)
                Children[i].Measure();
            return base.MeasureLayout();
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            _content = ClippingRect - Padding;
            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                var position = Slot(child);
                var size = child.Measure();
                var origin = SlotOrigin(child, position, size);
                position.ArrangedOrigin = origin;
                child.UpdateLayout(new Rect(origin.X, origin.Y, size.Width, size.Height));
            }
            _arrangeValid = !IsGone;
        }

        private PointF SlotOrigin(Control child, CanvasPosition position, Size size)
        {
            float x, y;
            if (position.Left.IsFixed())
                x = _content.Left + position.Left - size.Width * position.Anchor.X;
            else if (position.Right.IsFixed())
                x = _content.Left + _content.Width - position.Right - size.Width;
            else
                x = _content.Left;

            if (position.Top.IsFixed())
                y = _content.Top + position.Top - size.Height * position.Anchor.Y;
            else if (position.Bottom.IsFixed())
                y = _content.Top + _content.Height - position.Bottom - size.Height;
            else
                y = _content.Top;
            return new PointF(x, y);
        }
    }
}
