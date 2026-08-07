using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class Grid : Panel
    {
        private class GridPosition
        {
            public int Row { get; set; }
            public int Column { get; set; }
            public int RowSpan { get; set; }
            public int ColumnSpan { get; set; }
        }

        public Grid()
        {
            RowDefinitions = new RowDefinitionCollection();
            ColumnDefinitions = new ColumnDefinitionCollection();
        }

        public RowDefinitionCollection RowDefinitions { get; }
        public ColumnDefinitionCollection ColumnDefinitions { get; }
        private readonly Dictionary<Control, Size> _measureCache = new Dictionary<Control, Size>();

        private static readonly ConditionalWeakTable<Control, GridPosition> ControlGridPositionDictionary = new ConditionalWeakTable<Control, GridPosition>();

        private static GridPosition GetGridPosition(Control control)
        {
            return ControlGridPositionDictionary.GetValue(control, _ => new GridPosition());
        }

        public static void SetRow(Control control, int row)
        {
            var gridPosition = GetGridPosition(control);
            gridPosition.Row = row;
        }

        public static void SetColumn(Control control, int column)
        {
            var gridPosition = GetGridPosition(control);
            gridPosition.Column = column;
        }
        public static void SetRowSpan(Control control, int rowSpan)
        {
            var gridPosition = GetGridPosition(control);
            gridPosition.RowSpan = rowSpan;
        }
        public static void SetColumnSpan(Control control, int columnSpan)
        {
            var gridPosition = GetGridPosition(control);
            gridPosition.ColumnSpan = columnSpan;
        }

        public static int GetRow(Control control)
        {
            return GetGridPosition(control).Row;
        }
        public static int GetColumn(Control control)
        {
            return GetGridPosition(control).Column;
        }
        public static int GetRowSpan(Control control)
        {
            return GetGridPosition(control).RowSpan;
        }
        public static int GetColumnSpan(Control control)
        {
            return GetGridPosition(control).ColumnSpan;
        }

        public void AddChild(Control child, int row = 0, int column = 0, int rowSpan = 1, int columnSpan = 1)
        {
            Children.Add(child);
            SetRow(child, row);
            SetColumn(child, column);
            SetRowSpan(child, rowSpan);
            SetColumnSpan(child, columnSpan);
        }

        private static Rect GetRect(Rect rect, Control child, float[] rowOffsets, float[] columnOffsets)
        {
            // Offsets are prefix sums (offsets[i] = start of track i, offsets[count] = total), so a
            // child rect is two subtractions instead of the former per-child Take/Skip/Sum passes.
            var rowCount = rowOffsets.Length - 1;
            var columnCount = columnOffsets.Length - 1;

            var row = Math.Min(Math.Max(GetRow(child), 0), rowCount - 1);
            var column = Math.Min(Math.Max(GetColumn(child), 0), columnCount - 1);
            var rowSpan = Math.Min(Math.Max(GetRowSpan(child), 1), rowCount - row);
            var columnSpan = Math.Min(Math.Max(GetColumnSpan(child), 1), columnCount - column);

            return new Rect(
                rect.Left + columnOffsets[column],
                rect.Top + rowOffsets[row],
                columnOffsets[column + columnSpan] - columnOffsets[column],
                rowOffsets[row + rowSpan] - rowOffsets[row]);
        }

        private static float[] BuildOffsets(List<float> sizes)
        {
            var offsets = new float[sizes.Count + 1];
            var total = 0f;
            for (var i = 0; i < sizes.Count; i++)
            {
                offsets[i] = total;
                total += sizes[i];
            }

            offsets[sizes.Count] = total;
            return offsets;
        }

        private List<float> GetRowHeights(Rect rect)
        {
            if (RowDefinitions.Count == 0)
                return new List<float>(1) { rect.Height.IsFixed() ? rect.Height : 0 };

            var starRows = 0f;
            var absoluteRows = 0f;
            var rowDefinitions = RowDefinitions;
            var result = new List<float>(rowDefinitions.Count);
            for (var i = 0; i < rowDefinitions.Count; i++)
            {
                var height = rowDefinitions[i].Height;
                switch (height.Unit)
                {
                    case GridLengthUnit.Auto:
                        result.Add(GetAutoRowHeight(i));
                        break;
                    case GridLengthUnit.Absolute:
                        absoluteRows += height.Value;
                        result.Add(height.Value);
                        break;
                    case GridLengthUnit.Relative:
                        starRows += height.Value;
                        result.Add(0);
                        break;
                }
            }

            AddSpanningRowContributions(result);

            var autoRows = 0f;
            for (var i = 0; i < rowDefinitions.Count; i++)
            {
                if (rowDefinitions[i].Height.Unit == GridLengthUnit.Auto)
                    autoRows += result[i];
            }

            var starLeftover = Math.Max(0, rect.Height - absoluteRows - autoRows);

            if (!starLeftover.IsFixed())
                starLeftover = 0;
            var starSingleValue = starRows > 0 ? starLeftover / starRows : 0;
            var total = 0f;
            for (var i = 0; i < rowDefinitions.Count; i++)
            {
                var height = rowDefinitions[i].Height;
                if (height.Unit == GridLengthUnit.Relative)
                    result[i] = height.Value * starSingleValue;
                total += result[i];
            }
            // Only star (Relative) rows should absorb leftover space; Absolute/Auto rows keep their
            // own size and any remainder below them stays empty, matching Grid star-sizing semantics.
            var f = rect.Height.IsFixed() ? rect.Height - total : 0;
            if (f > 0 && starRows > 0)
            {
                for (var i = rowDefinitions.Count - 1; i >= 0; i--)
                {
                    if (rowDefinitions[i].Height.Unit != GridLengthUnit.Relative)
                        continue;
                    result[i] += f;
                    break;
                }
            }
            return result;
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            var layoutRect = BoundingRect - Margin - Padding;
            var rowOffsets = BuildOffsets(GetRowHeights(layoutRect));
            var columnOffsets = BuildOffsets(GetColumnWidths(layoutRect));
            foreach (var child in Children)
            {
                child.UpdateLayout(GetRect(layoutRect, child, rowOffsets, columnOffsets));
            }
            _measureCache.Clear();
        }

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            // Refresh cached child measurements; UpdateLayout reuses them for the track passes.
            _measureCache.Clear();
            var width = Width.IsFixed() ? Width : MeasureContentWidth() + Padding.Horizontal;
            var height = Height.IsFixed() ? Height : MeasureContentHeight() + Padding.Vertical;
            return ApplyConstraints(new Size(width, height)) + Margin;
        }

        private float MeasureContentWidth()
        {
            if (ColumnDefinitions.Count == 0)
            {
                var max = 0f;
                foreach (var child in Children)
                    max = Math.Max(max, MeasureChild(child).Width);
                return max;
            }

            var total = 0f;
            for (var i = 0; i < ColumnDefinitions.Count; i++)
            {
                var definition = ColumnDefinitions[i];
                total += definition.Width.Unit == GridLengthUnit.Absolute
                    ? definition.Width.Value
                    : GetAutoColumnWidth(i);
            }
            return total;
        }

        private float MeasureContentHeight()
        {
            if (RowDefinitions.Count == 0)
            {
                var max = 0f;
                foreach (var child in Children)
                    max = Math.Max(max, MeasureChild(child).Height);
                return max;
            }

            var total = 0f;
            for (var i = 0; i < RowDefinitions.Count; i++)
            {
                var definition = RowDefinitions[i];
                total += definition.Height.Unit == GridLengthUnit.Absolute
                    ? definition.Height.Value
                    : GetAutoRowHeight(i);
            }
            return total;
        }

        private List<float> GetColumnWidths(Rect rect)
        {
            if (ColumnDefinitions.Count == 0)
                return new List<float>(1) { rect.Width.IsFixed() ? rect.Width : 0 };

            var starColumns = 0f;
            var absoluteColumns = 0f;
            var columnDefinitions = ColumnDefinitions;
            var result = new List<float>(columnDefinitions.Count);
            for (var i = 0; i < columnDefinitions.Count; i++)
            {
                var width = columnDefinitions[i].Width;
                switch (width.Unit)
                {
                    case GridLengthUnit.Auto:
                        result.Add(GetAutoColumnWidth(i));
                        break;
                    case GridLengthUnit.Absolute:
                        absoluteColumns += width.Value;
                        result.Add(width.Value);
                        break;
                    case GridLengthUnit.Relative:
                        starColumns += width.Value;
                        result.Add(0);
                        break;
                }
            }

            AddSpanningColumnContributions(result);

            var autoColumns = 0f;
            for (var i = 0; i < columnDefinitions.Count; i++)
            {
                if (columnDefinitions[i].Width.Unit == GridLengthUnit.Auto)
                    autoColumns += result[i];
            }

            var starLeftover = Math.Max(0, rect.Width - absoluteColumns - autoColumns);
            if (!starLeftover.IsFixed())
                starLeftover = 0;
            var starSingleValue = starColumns > 0 ? starLeftover / starColumns : 0;
            var total = 0f;
            for (var i = 0; i < columnDefinitions.Count; i++)
            {
                var width = columnDefinitions[i].Width;
                if (width.Unit == GridLengthUnit.Relative)
                    result[i] = width.Value * starSingleValue;
                total += result[i];
            }
            // Only star (Relative) columns should absorb leftover space; Absolute/Auto columns keep
            // their own size and any remainder stays empty, matching Grid star-sizing semantics.
            var f = rect.Width.IsFixed() ? rect.Width - total : 0;
            if (f > 0 && starColumns > 0)
            {
                for (var i = columnDefinitions.Count - 1; i >= 0; i--)
                {
                    if (columnDefinitions[i].Width.Unit != GridLengthUnit.Relative)
                        continue;
                    result[i] += f;
                    break;
                }
            }
            return result;
        }

        private float GetAutoRowHeight(int index)
        {
            var max = 0f;
            foreach (var child in Children)
            {
                if (GetRow(child) != index || GetRowSpan(child) != 1)
                    continue;

                var size = MeasureChild(child);
                if (size.Height > max)
                    max = size.Height;
            }
            return max;
        }

        private float GetAutoColumnWidth(int index)
        {
            var max = 0f;
            foreach (var child in Children)
            {
                if (GetColumn(child) != index || GetColumnSpan(child) != 1)
                    continue;

                var size = MeasureChild(child);
                if (size.Width > max)
                    max = size.Width;
            }
            return max;
        }

        private void AddSpanningRowContributions(IList<float> result)
        {
            foreach (var child in Children)
            {
                var row = Math.Min(Math.Max(GetRow(child), 0), RowDefinitions.Count - 1);
                var rowSpan = Math.Min(Math.Max(GetRowSpan(child), 1), RowDefinitions.Count - row);
                if (rowSpan <= 1)
                    continue;

                // Star tracks absorb the remaining space at arrange time; only spans made of
                // auto/absolute tracks need their deficit pushed into the auto tracks.
                var containsStar = false;
                var autoCount = 0;
                var occupiedHeight = 0f;
                for (var i = row; i < row + rowSpan; i++)
                {
                    var unit = RowDefinitions[i].Height.Unit;
                    if (unit == GridLengthUnit.Relative)
                    {
                        containsStar = true;
                        break;
                    }

                    if (unit == GridLengthUnit.Auto)
                        autoCount++;
                    occupiedHeight += result[i];
                }

                if (containsStar || autoCount == 0)
                    continue;

                var deficit = MeasureChild(child).Height - occupiedHeight;
                if (deficit <= 0)
                    continue;

                var addition = deficit / autoCount;
                for (var i = row; i < row + rowSpan; i++)
                {
                    if (RowDefinitions[i].Height.Unit == GridLengthUnit.Auto)
                        result[i] += addition;
                }
            }
        }

        private void AddSpanningColumnContributions(IList<float> result)
        {
            foreach (var child in Children)
            {
                var column = Math.Min(Math.Max(GetColumn(child), 0), ColumnDefinitions.Count - 1);
                var columnSpan = Math.Min(Math.Max(GetColumnSpan(child), 1), ColumnDefinitions.Count - column);
                if (columnSpan <= 1)
                    continue;

                var containsStar = false;
                var autoCount = 0;
                var occupiedWidth = 0f;
                for (var i = column; i < column + columnSpan; i++)
                {
                    var unit = ColumnDefinitions[i].Width.Unit;
                    if (unit == GridLengthUnit.Relative)
                    {
                        containsStar = true;
                        break;
                    }

                    if (unit == GridLengthUnit.Auto)
                        autoCount++;
                    occupiedWidth += result[i];
                }

                if (containsStar || autoCount == 0)
                    continue;

                var deficit = MeasureChild(child).Width - occupiedWidth;
                if (deficit <= 0)
                    continue;

                var addition = deficit / autoCount;
                for (var i = column; i < column + columnSpan; i++)
                {
                    if (ColumnDefinitions[i].Width.Unit == GridLengthUnit.Auto)
                        result[i] += addition;
                }
            }
        }

        private Size MeasureChild(Control child)
        {
            if (_measureCache.TryGetValue(child, out var size))
                return size;

            size = child.MeasureLayout();
            _measureCache[child] = size;
            return size;
        }
    }
}
