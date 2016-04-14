using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
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
            base.AddChild(child);
            SetRow(child, row);
            SetColumn(child, column);
            SetRowSpan(child, rowSpan);
            SetColumnSpan(child, columnSpan);
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rectangle rect)
        {
            spriteBatch.Draw(ScreenEngine.Pixel, rect, BackgroundColor);
            foreach (var child in Children)
                child.OnDraw(spriteBatch, GetRect(rect, child));
        }

        private Rectangle GetRect(Rectangle rect, Control child)
        {
            var coloumnCount = ColumnDefinitions?.Count ?? 1;
            var rowCount = RowDefinitions?.Count ?? 1;
            var singleWidth = (int)(Width / coloumnCount);
            var singleHeight = (int)(Height / rowCount);

            var row = Math.Min(GetRow(child), rowCount);
            var column = Math.Min(GetColumn(child), coloumnCount);
            var rowSpan = Math.Max(GetRowSpan(child), 1);
            var columnSpan = Math.Max(GetColumnSpan(child), 1);

            return new Rectangle(column * singleWidth + rect.X, row * singleHeight + rect.Y, singleWidth * columnSpan, singleHeight * rowSpan);
        }

        protected internal override void OnUpdate(TimeSpan elapsed, Rectangle rect)
        {
            base.OnUpdate(elapsed, rect);
            foreach (var child in Children)
            {
                child.OnUpdate(elapsed, GetRect(rect, child));
            }
        }

        private List<float> GetRowHeights()
        {
            // floats
            var autoRows = 0f;
            var starRows = 0f;
            var absoluteRows = 0f;
            foreach (var row in RowDefinitions)
            {
                switch (row.Height.Unit)
                {
                    case GridLengthUnit.Auto:
                        // Ignore now
                        autoRows += 0;
                        break;
                    case GridLengthUnit.Absolute:
                        absoluteRows += row.Height.Value;
                        break;
                    case GridLengthUnit.Relative:
                        starRows += row.Height.Value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var starLeftover = Math.Max(0, Height - absoluteRows - autoRows);
            var starSingleValue = starLeftover / starRows;
            var result = new List<float>();
            foreach (var row in RowDefinitions)
            {
                switch (row.Height.Unit)
                {
                    case GridLengthUnit.Auto:
                        // TODO 
                        result.Add(0);
                        break;
                    case GridLengthUnit.Absolute:
                        result.Add(row.Height.Value);
                        break;
                    case GridLengthUnit.Relative:
                        result.Add(row.Height.Value * starSingleValue);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return result;
        }
    }
}