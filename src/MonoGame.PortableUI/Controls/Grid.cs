using System;
using System.Collections.Generic;
using System.Linq;
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

        private Rect GetRect(Rect rect, Control child, IReadOnlyList<float> rowHeights, IReadOnlyList<float> columnWidths)
        {
            var columnCount = ColumnDefinitions.Count > 0 ? ColumnDefinitions.Count : 1;
            var rowCount = RowDefinitions.Count > 0 ? RowDefinitions.Count : 1;

            var row = Math.Min(Math.Max(GetRow(child), 0), rowCount - 1);
            var column = Math.Min(Math.Max(GetColumn(child), 0), columnCount - 1);
            var rowSpan = Math.Min(Math.Max(GetRowSpan(child), 1), rowCount - row);
            var columnSpan = Math.Min(Math.Max(GetColumnSpan(child), 1), columnCount - column);

            var rectangle = new Rect(
                columnWidths.Take(column).Sum() + rect.Left,
                rowHeights.Take(row).Sum() + rect.Top,
                columnWidths.Skip(column).Take(columnSpan).Sum(),
                rowHeights.Skip(row).Take(rowSpan).Sum()
            );
            return rectangle;
        }

        private List<float> GetRowHeights(Rect rect)
        {
            if (RowDefinitions.Count == 0)
                return new List<float> { rect.Height.IsFixed() ? rect.Height : 0 };

            var starRows = 0f;
            var absoluteRows = 0f;
            var rowDefinitions = RowDefinitions;
            var result = new List<float>();
            foreach (var gridLength in rowDefinitions.Select((row, i) => new { row.Height, Index = i }))
            {
                switch (gridLength.Height.Unit)
                {
                    case GridLengthUnit.Auto:
                        result.Add(GetAutoRowHeight(gridLength.Index));
                        break;
                    case GridLengthUnit.Absolute:
                        absoluteRows += gridLength.Height.Value;
                        result.Add(gridLength.Height.Value);
                        break;
                    case GridLengthUnit.Relative:
                        starRows += gridLength.Height.Value;
                        result.Add(0);
                        break;
                }
            }

            AddSpanningRowContributions(result);

            var autoRows = rowDefinitions
                .Select((row, index) => row.Height.Unit == GridLengthUnit.Auto ? result[index] : 0)
                .Sum();

            var starLeftover = Math.Max(0, rect.Height - absoluteRows - autoRows);

            if (!starLeftover.IsFixed())
                starLeftover = 0;
            var starSingleValue = starRows > 0 ? starLeftover / starRows : 0;
            foreach (var gridLength in rowDefinitions.Select((row, i) => new { row.Height, Index = i }))
            {
                if (gridLength.Height.Unit == GridLengthUnit.Relative)
                    result[gridLength.Index] = gridLength.Height.Value * starSingleValue;
            }
            // Only star (Relative) rows should absorb leftover space; Absolute/Auto rows keep their
            // own size and any remainder below them stays empty, matching Grid star-sizing semantics.
            var f = rect.Height.IsFixed() ? rect.Height - result.Sum() : 0;
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
            var rowHeights = GetRowHeights(layoutRect);
            var columnWidths = GetColumnWidths(layoutRect);
            foreach (var child in Children)
            {
                child.UpdateLayout(GetRect(layoutRect, child, rowHeights, columnWidths));
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
                return new List<float> { rect.Width.IsFixed() ? rect.Width : 0 };

            var starColumns = 0f;
            var absoluteColumns = 0f;
            var columnDefinitions = ColumnDefinitions;
            var result = new List<float>();
            foreach (var gridLength in columnDefinitions.Select((column, i) => new { column.Width, Index = i }))
            {
                switch (gridLength.Width.Unit)
                {
                    case GridLengthUnit.Auto:
                        result.Add(GetAutoColumnWidth(gridLength.Index));
                        break;
                    case GridLengthUnit.Absolute:
                        absoluteColumns += gridLength.Width.Value;
                        result.Add(gridLength.Width.Value);
                        break;
                    case GridLengthUnit.Relative:
                        starColumns += gridLength.Width.Value;
                        result.Add(0);
                        break;
                }
            }

            AddSpanningColumnContributions(result);

            var autoColumns = columnDefinitions
                .Select((column, index) => column.Width.Unit == GridLengthUnit.Auto ? result[index] : 0)
                .Sum();

            var starLeftover = Math.Max(0, rect.Width - absoluteColumns - autoColumns);
            if (!starLeftover.IsFixed())
                starLeftover = 0;
            var starSingleValue = starColumns > 0 ? starLeftover / starColumns : 0;
            foreach (var gridLength in columnDefinitions.Select((column, i) => new { column.Width, Index = i }))
            {
                if (gridLength.Width.Unit == GridLengthUnit.Relative)
                    result[gridLength.Index] = gridLength.Width.Value * starSingleValue;
            }
            // Only star (Relative) columns should absorb leftover space; Absolute/Auto columns keep
            // their own size and any remainder stays empty, matching Grid star-sizing semantics.
            var f = rect.Width.IsFixed() ? rect.Width - result.Sum() : 0;
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
                if (SpanContainsStarTrack(RowDefinitions.Select(definition => definition.Height.Unit), row, rowSpan))
                    continue;

                var autoRows = GetAutoTrackIndices(RowDefinitions.Select(definition => definition.Height.Unit), row, rowSpan);
                if (autoRows.Count == 0)
                    continue;

                var occupiedHeight = result.Skip(row).Take(rowSpan).Sum();
                var deficit = MeasureChild(child).Height - occupiedHeight;
                if (deficit <= 0)
                    continue;

                var addition = deficit / autoRows.Count;
                foreach (var autoRow in autoRows)
                    result[autoRow] += addition;
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

                if (SpanContainsStarTrack(ColumnDefinitions.Select(definition => definition.Width.Unit), column, columnSpan))
                    continue;

                var autoColumns = GetAutoTrackIndices(ColumnDefinitions.Select(definition => definition.Width.Unit), column, columnSpan);
                if (autoColumns.Count == 0)
                    continue;

                var occupiedWidth = result.Skip(column).Take(columnSpan).Sum();
                var deficit = MeasureChild(child).Width - occupiedWidth;
                if (deficit <= 0)
                    continue;

                var addition = deficit / autoColumns.Count;
                foreach (var autoColumn in autoColumns)
                    result[autoColumn] += addition;
            }
        }

        private static bool SpanContainsStarTrack(IEnumerable<GridLengthUnit> units, int start, int span)
        {
            return units.Skip(start).Take(span).Any(unit => unit == GridLengthUnit.Relative);
        }

        private static List<int> GetAutoTrackIndices(IEnumerable<GridLengthUnit> units, int start, int span)
        {
            return units
                .Select((unit, index) => new { unit, index })
                .Where(track => track.index >= start && track.index < start + span && track.unit == GridLengthUnit.Auto)
                .Select(track => track.index)
                .ToList();
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
