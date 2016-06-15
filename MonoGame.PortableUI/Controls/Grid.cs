using System;
using System.Collections.Generic;
using System.Linq;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class Grid : Panel
    {
        private struct GridPosition
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

        private static readonly Dictionary<Control, GridPosition> ControlGridPositionDictionary = new Dictionary<Control, GridPosition>();

        private static GridPosition GetGridPosition(Control control)
        {
            GridPosition gridPosition = new GridPosition();
            if (ControlGridPositionDictionary.ContainsKey(control))
                gridPosition = ControlGridPositionDictionary[control];
            return gridPosition;
        }

        public static void SetRow(Control control, int row)
        {
            var gridPosition = GetGridPosition(control);
            gridPosition.Row = row;
            ControlGridPositionDictionary[control] = gridPosition;
        }

        public static void SetColumn(Control control, int column)
        {
            var gridPosition = GetGridPosition(control);
            gridPosition.Column = column;
            ControlGridPositionDictionary[control] = gridPosition;
        }
        public static void SetRowSpan(Control control, int rowSpan)
        {
            var gridPosition = GetGridPosition(control);
            gridPosition.RowSpan = rowSpan;
            ControlGridPositionDictionary[control] = gridPosition;
        }
        public static void SetColumnSpan(Control control, int columnSpan)
        {
            var gridPosition = GetGridPosition(control);
            gridPosition.ColumnSpan = columnSpan;
            ControlGridPositionDictionary[control] = gridPosition;
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

        public void AddChild(Control child, int row = 0, int column = 0, int rowSpan = 0, int columnSpan = 0)
        {
            Children.Add(child);
            SetRow(child, row);
            SetColumn(child, column);
            SetRowSpan(child, rowSpan);
            SetColumnSpan(child, columnSpan);
        }

        private Rect GetRect(Rect rect, Control child)
        {
            var coloumnCount = ColumnDefinitions.Count > 0 ? ColumnDefinitions.Count : 1;
            var rowCount = RowDefinitions.Count > 0 ? RowDefinitions.Count : 1;

            var rowHeights = GetRowHeights(rect);
            var columnWidths = GetColumnWidths(rect);

            var row = Math.Min(GetRow(child), rowCount);
            var column = Math.Min(GetColumn(child), coloumnCount);
            var rowSpan = Math.Max(GetRowSpan(child), 1);
            var columnSpan = Math.Max(GetColumnSpan(child), 1);

            var rectangle = new Rect(
                columnWidths.Take(column).Sum() + rect.Left,
                rowHeights.Take(row).Sum() + rect.Top,
                columnWidths.Skip(column).Take(columnSpan).Sum(),
                rowHeights.Skip(row).Take(rowSpan).Sum()
            );
            return rectangle;
        }

        private List<int> GetRowHeights(Rect rect)
        {
            var autoRows = 0f;
            var starRows = 0f;
            var absoluteRows = 0f;
            var rowDefinitions = RowDefinitions;
            foreach (var gridLength in rowDefinitions.Select((row, i) => new {row.Height, Index = i }))
            {
                switch (gridLength.Height.Unit)
                {
                    case GridLengthUnit.Auto:
                        var max = 0f;
                        foreach (var child in Children)
                        {
                            var row = GetRow(child);
                            if (row == gridLength.Index)
                            {
                                var size = child.MeasureLayout();
                                if (size.Height > max)
                                    max = size.Height;
                            }
                        }
                        // Ignore now
                        autoRows += max;
                        break;
                    case GridLengthUnit.Absolute:
                        absoluteRows += gridLength.Height.Value;
                        break;
                    case GridLengthUnit.Relative:
                        starRows += gridLength.Height.Value;
                        break;
                }
            }

            var starLeftover = Math.Max(0, rect.Height - absoluteRows - autoRows);

            if (!starLeftover.IsFixed())
                starLeftover = 0;
            var starSingleValue = starLeftover / starRows;
            var result = new List<int>();
            foreach (var gridLength in rowDefinitions.Select((row, i) => new { row.Height, Index = i }))
            {
                switch (gridLength.Height.Unit)
                {
                    case GridLengthUnit.Auto:
                        var max = 0f;
                        foreach (var child in Children)
                        {
                            var row = GetRow(child);
                            if (row == gridLength.Index)
                            {
                                var size = child.MeasureLayout();
                                if (size.Height > max)
                                    max = size.Height;
                            }
                        }
                        // Ignore now
                        result.Add((int) max);
                        break;
                    case GridLengthUnit.Absolute:
                        result.Add((int)gridLength.Height.Value);
                        break;
                    case GridLengthUnit.Relative:
                        result.Add((int)(gridLength.Height.Value * starSingleValue));
                        break;
                }
            }
            var f = (int)rect.Height - result.Sum();
            if (f > 0)
                result[result.Count - 1] += f;
            return result;
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            foreach (var child in Children)
            {
                child.UpdateLayout(GetRect(BoundingRect - Margin, child));
            }
        }

        public override Size MeasureLayout()
        {
            return base.MeasureLayout();
        }

        private List<int> GetColumnWidths(Rect rect)
        {
            // floats
            var autoColumns = 0f;
            var starColumns = 0f;
            var absoluteColumns = 0f;
            var columnDefinitions = ColumnDefinitions;
            foreach (var gridLength in columnDefinitions.Select((column, i) => new { column.Width, Index = i }))
            {
                switch (gridLength.Width.Unit)
                {
                    case GridLengthUnit.Auto:  
                        var max = 0f;
                        foreach (var child in Children)
                        {
                            var row = GetColumn(child);
                            if (row == gridLength.Index)
                            {
                                var size = child.MeasureLayout();
                                if (size.Width > max)
                                    max = size.Width;
                            }
                        }
                        autoColumns += max;
                        break;
                    case GridLengthUnit.Absolute:
                        absoluteColumns += gridLength.Width.Value;
                        break;
                    case GridLengthUnit.Relative:
                        starColumns += gridLength.Width.Value;
                        break;
                }
            }

            var starLeftover = Math.Max(0, rect.Width - absoluteColumns - autoColumns);
            if (!starLeftover.IsFixed())
                starLeftover = 0;
            var starSingleValue = starLeftover / starColumns;
            var result = new List<int>();
            foreach (var gridLength in columnDefinitions.Select((column, i) => new { column.Width, Index = i }))
            {
                switch (gridLength.Width.Unit)
                {
                    case GridLengthUnit.Auto:
                        var max = 0f;
                        foreach (var child in Children)
                        {
                            var row = GetColumn(child);
                            if (row == gridLength.Index)
                            {
                                var size = child.MeasureLayout();
                                if (size.Width > max)
                                    max = size.Width;
                            }
                        }
                        // Ignore now
                        result.Add((int)max);
                        break;
                    case GridLengthUnit.Absolute:
                        result.Add((int)gridLength.Width.Value);
                        break;
                    case GridLengthUnit.Relative:
                        result.Add((int)(gridLength.Width.Value * starSingleValue));
                        break;
                }
            }
            var f = (int)rect.Width - result.Sum();
            if (f > 0)
                result[result.Count - 1] += f;
            return result;
        }
    }
}