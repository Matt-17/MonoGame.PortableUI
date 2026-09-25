using System;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     Lays children out in rows (or columns) and starts a new row when the next child would not
    ///     fit — e.g. an inventory or tag list whose item count varies. <see cref="ItemWidth"/> and
    ///     <see cref="ItemHeight"/> give every child the same slot; NaN (default) uses each child's
    ///     own size.
    /// </summary>
    /// <remarks>
    ///     Measure has no available size here, so the wrap width is the fixed <see cref="Control.Width"/>,
    ///     else <see cref="Control.MaxWidth"/>, else the width the panel was last arranged at. When the
    ///     arranged width changes the line count, the panel invalidates itself and the next pass
    ///     measures the correct height.
    /// </remarks>
    public class WrapPanel : Panel
    {
        private Orientation _orientation = Orientation.Horizontal;
        private float _itemWidth = float.NaN;
        private float _itemHeight = float.NaN;
        private float _horizontalSpacing;
        private float _verticalSpacing;
        private float _lastArrangedExtent = float.NaN;
        private float _lastMeasuredCross = float.NaN;

        /// <summary>Horizontal fills rows left to right and wraps downwards; Vertical fills columns.</summary>
        public Orientation Orientation
        {
            get => _orientation;
            set { _orientation = value; InvalidateLayout(true); }
        }

        public float ItemWidth
        {
            get => _itemWidth;
            set { _itemWidth = value; InvalidateLayout(true); }
        }

        public float ItemHeight
        {
            get => _itemHeight;
            set { _itemHeight = value; InvalidateLayout(true); }
        }

        /// <summary>Gap between neighbouring items in a row (Horizontal) or between columns (Vertical).</summary>
        public float HorizontalSpacing
        {
            get => _horizontalSpacing;
            set { _horizontalSpacing = value; InvalidateLayout(true); }
        }

        /// <summary>Gap between rows (Horizontal) or between items in a column (Vertical).</summary>
        public float VerticalSpacing
        {
            get => _verticalSpacing;
            set { _verticalSpacing = value; InvalidateLayout(true); }
        }

        public override Size MeasureLayout()
        {
            if (IsGone || (Width.IsFixed() && Height.IsFixed()))
                return base.MeasureLayout();

            var horizontal = Orientation == Orientation.Horizontal;
            var limit = horizontal
                ? (Width.IsFixed() ? Width : MaxWidth.IsFixed() ? MaxWidth : _lastArrangedExtent + Padding.Horizontal) - Padding.Horizontal
                : (Height.IsFixed() ? Height : MaxHeight.IsFixed() ? MaxHeight : _lastArrangedExtent + Padding.Vertical) - Padding.Vertical;
            if (!limit.IsFixed() || limit <= 0)
                limit = float.PositiveInfinity;

            var (main, cross) = Flow(limit, arrange: false, 0, 0);
            _lastMeasuredCross = cross;
            var contentWidth = horizontal ? main : cross;
            var contentHeight = horizontal ? cross : main;
            var size = new Size(
                Width.IsFixed() ? Width : contentWidth + Padding.Horizontal,
                Height.IsFixed() ? Height : contentHeight + Padding.Vertical);
            return ApplyConstraints(size) + Margin;
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            var content = BoundingRect - Margin - Padding;
            var horizontal = Orientation == Orientation.Horizontal;
            var extent = horizontal ? content.Width : content.Height;
            if (!extent.IsFixed() || extent <= 0)
                extent = float.PositiveInfinity;

            var (_, cross) = Flow(extent, arrange: true, content.Left, content.Top);

            // The wrap width is only known now; if it changed the line count, measure again.
            var changed = !_lastArrangedExtent.Equals(extent);
            _lastArrangedExtent = extent;
            if (changed && !cross.Equals(_lastMeasuredCross) && !Width.IsFixed() && !Height.IsFixed())
                InvalidateLayout(true);
        }

        /// <summary>
        ///     Walks the children in order, breaking lines at <paramref name="limit"/> along the main
        ///     axis. Returns the widest line (main axis) and the total of line thicknesses (cross axis);
        ///     with <paramref name="arrange"/> it also places the children.
        /// </summary>
        private (float Main, float Cross) Flow(float limit, bool arrange, float originX, float originY)
        {
            var horizontal = Orientation == Orientation.Horizontal;
            var mainSpacing = horizontal ? HorizontalSpacing : VerticalSpacing;
            var crossSpacing = horizontal ? VerticalSpacing : HorizontalSpacing;

            float widestLine = 0;
            float crossTotal = 0;
            float lineMain = 0;
            float lineCross = 0;
            var lineStart = 0;
            var lineItems = 0;

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (child.IsGone)
                    continue;

                var (itemMain, itemCross) = SlotSize(child, horizontal);
                var needed = lineItems == 0 ? itemMain : lineMain + mainSpacing + itemMain;
                if (lineItems > 0 && needed > limit + 0.01f)
                {
                    if (arrange)
                        ArrangeLine(lineStart, i, originX, originY, crossTotal, lineCross, horizontal, mainSpacing);
                    widestLine = Math.Max(widestLine, lineMain);
                    crossTotal += lineCross + crossSpacing;
                    lineStart = i;
                    lineItems = 0;
                    lineMain = 0;
                    lineCross = 0;
                    needed = itemMain;
                }

                lineMain = needed;
                lineCross = Math.Max(lineCross, itemCross);
                lineItems++;
            }

            if (lineItems > 0)
            {
                if (arrange)
                    ArrangeLine(lineStart, Children.Count, originX, originY, crossTotal, lineCross, horizontal, mainSpacing);
                widestLine = Math.Max(widestLine, lineMain);
                crossTotal += lineCross;
            }

            return (widestLine, crossTotal);
        }

        private void ArrangeLine(int start, int end, float originX, float originY, float crossOffset, float lineCross, bool horizontal, float mainSpacing)
        {
            float main = 0;
            var first = true;
            for (var i = start; i < end; i++)
            {
                var child = Children[i];
                if (child.IsGone)
                    continue;

                if (!first)
                    main += mainSpacing;
                first = false;
                var (itemMain, _) = SlotSize(child, horizontal);
                var slot = horizontal
                    ? new Rect(originX + main, originY + crossOffset, itemMain, lineCross)
                    : new Rect(originX + crossOffset, originY + main, lineCross, itemMain);
                child.UpdateLayout(slot);
                main += itemMain;
            }
        }

        private (float Main, float Cross) SlotSize(Control child, bool horizontal)
        {
            var desired = child.Measure();
            var width = ItemWidth.IsFixed() ? ItemWidth : desired.Width;
            var height = ItemHeight.IsFixed() ? ItemHeight : desired.Height;
            return horizontal ? (width, height) : (height, width);
        }
    }
}
