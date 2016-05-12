using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
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

        public List<RowDefinition> RowDefinitions { get; set; }
        public List<ColumnDefinition> ColumnDefinitions { get; set; }

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

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            spriteBatch.Draw(ScreenEngine.Pixel, rect, BackgroundColor);
            base.OnDraw(spriteBatch, rect);
        }

        private Rect GetRect(Rect rect, Control child)
        {
            var coloumnCount = ColumnDefinitions?.Count ?? 1;
            var rowCount = RowDefinitions?.Count ?? 1;

            var rowHeights = GetRowHeights();
            var columnWidths = GetColumnWidths();

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

        //protected internal override void OnUpdate(TimeSpan elapsed, Rectangle rect)
        //{
        //    base.OnUpdate(elapsed, rect);
        //    foreach (var child in Children)
        //    {
        //        child.OnUpdate(elapsed, GetRect(rect, child));
        //    }
        //}

        private List<int> GetRowHeights()
        {
            // floats
            var autoRows = 0f;
            var starRows = 0f;
            var absoluteRows = 0f;
            var rowDefinitions = RowDefinitions ?? new List<RowDefinition> { new RowDefinition() };
            foreach (var gridLength in rowDefinitions.Select(row => row.Height))
            {
                switch (gridLength.Unit)
                {
                    case GridLengthUnit.Auto:
                        // Ignore now
                        autoRows += 0;
                        break;
                    case GridLengthUnit.Absolute:
                        absoluteRows += gridLength.Value;
                        break;
                    case GridLengthUnit.Relative:
                        starRows += gridLength.Value;
                        break;
                }
            }

            var starLeftover = Math.Max(0, Height - absoluteRows - autoRows);
            var starSingleValue = starLeftover / starRows;
            var result = new List<int>();
            foreach (var gridLength in rowDefinitions.Select(row => row.Height))
            {
                switch (gridLength.Unit)
                {
                    case GridLengthUnit.Auto:
                        // TODO 
                        result.Add(0);
                        break;
                    case GridLengthUnit.Absolute:
                        result.Add((int)gridLength.Value);
                        break;
                    case GridLengthUnit.Relative:
                        result.Add((int)(gridLength.Value * starSingleValue));
                        break;
                }
            }
            var f = (int)Height - result.Sum();
            if (f > 0)
                result[result.Count - 1] += f;
            return result;
        }

        public override void UpdateLayout(Rect availableBoundingRect)
        {
            base.UpdateLayout(availableBoundingRect);
            foreach (var child in Children )
            {
                child.UpdateLayout(GetRect(availableBoundingRect, child));
            }
        }

        public override Size MeasureLayout(Size availableSize)
        {
            return base.MeasureLayout(availableSize);
        }

        private List<int> GetColumnWidths()
        {
            // floats
            var autoColumns = 0f;
            var starColumns = 0f;
            var absoluteColumns = 0f;
            var columnDefinitions = ColumnDefinitions ?? new List<ColumnDefinition> { new ColumnDefinition() };
            foreach (var gridLength in columnDefinitions.Select(columns => columns.Width))
            {
                switch (gridLength.Unit)
                {
                    case GridLengthUnit.Auto:
                        // Ignore now
                        autoColumns += 0;
                        break;
                    case GridLengthUnit.Absolute:
                        absoluteColumns += gridLength.Value;
                        break;
                    case GridLengthUnit.Relative:
                        starColumns += gridLength.Value;
                        break;
                }
            }

            var starLeftover = Math.Max(0, Width - absoluteColumns - autoColumns);
            var starSingleValue = starLeftover / starColumns;
            var result = new List<int>();
            foreach (var gridLength in columnDefinitions.Select(column => column.Width))
            {
                switch (gridLength.Unit)
                {
                    case GridLengthUnit.Auto:
                        // TODO 
                        result.Add(0);
                        break;
                    case GridLengthUnit.Absolute:
                        result.Add((int)gridLength.Value);
                        break;
                    case GridLengthUnit.Relative:
                        result.Add((int)(gridLength.Value * starSingleValue));
                        break;
                }
            }
            var f = (int)Width - result.Sum();
            if (f > 0)
                result[result.Count - 1] += f;
            return result;
        }
    }
}