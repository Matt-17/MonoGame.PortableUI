using System;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     Arranges children in a grid of equally sized cells, filled row by row — inventories, level
    ///     select, keypads. With <see cref="Columns"/> and/or <see cref="Rows"/> at 0 the missing
    ///     dimension is derived from the child count (both 0: as square as possible). Every cell is
    ///     as large as the largest child unless the panel itself is sized; collapsed (gone) children
    ///     take no cell.
    /// </summary>
    public class UniformGrid : Panel
    {
        private int _rows;
        private int _columns;
        private float _spacing;

        public int Rows
        {
            get => _rows;
            set { _rows = Math.Max(0, value); InvalidateLayout(true); }
        }

        public int Columns
        {
            get => _columns;
            set { _columns = Math.Max(0, value); InvalidateLayout(true); }
        }

        /// <summary>Gap between neighbouring cells, horizontally and vertically.</summary>
        public float Spacing
        {
            get => _spacing;
            set { _spacing = Math.Max(0, value); InvalidateLayout(true); }
        }

        private int VisibleChildCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < Children.Count; i++)
                {
                    if (!Children[i].IsGone)
                        count++;
                }
                return count;
            }
        }

        internal (int Rows, int Columns) GetDimensions()
        {
            var count = VisibleChildCount;
            var columns = Columns;
            var rows = Rows;
            if (columns == 0 && rows == 0)
            {
                columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(count)));
                rows = Math.Max(1, (int)Math.Ceiling(count / (double)columns));
            }
            else if (columns == 0)
            {
                columns = Math.Max(1, (int)Math.Ceiling(count / (double)rows));
            }
            else if (rows == 0)
            {
                rows = Math.Max(1, (int)Math.Ceiling(count / (double)columns));
            }
            return (rows, columns);
        }

        public override Size MeasureLayout()
        {
            if (IsGone || (Width.IsFixed() && Height.IsFixed()))
                return base.MeasureLayout();

            float cellWidth = 0;
            float cellHeight = 0;
            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (child.IsGone)
                    continue;
                var desired = child.Measure();
                cellWidth = Math.Max(cellWidth, desired.Width);
                cellHeight = Math.Max(cellHeight, desired.Height);
            }

            var (rows, columns) = GetDimensions();
            var size = new Size(
                Width.IsFixed() ? Width : columns * cellWidth + (columns - 1) * Spacing + Padding.Horizontal,
                Height.IsFixed() ? Height : rows * cellHeight + (rows - 1) * Spacing + Padding.Vertical);
            return ApplyConstraints(size) + Margin;
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            var content = BoundingRect - Margin - Padding;
            var (rows, columns) = GetDimensions();
            var cellWidth = Math.Max(0, (content.Width - (columns - 1) * Spacing) / columns);
            var cellHeight = Math.Max(0, (content.Height - (rows - 1) * Spacing) / rows);

            var cell = 0;
            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (child.IsGone)
                {
                    child.UpdateLayout(Rect.Empty);
                    continue;
                }

                var row = cell / columns;
                var column = cell % columns;
                cell++;
                child.UpdateLayout(new Rect(
                    content.Left + column * (cellWidth + Spacing),
                    content.Top + row * (cellHeight + Spacing),
                    cellWidth,
                    cellHeight));
            }
        }
    }
}
