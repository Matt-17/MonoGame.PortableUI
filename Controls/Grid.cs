using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

        private static readonly Dictionary<UIControl, GridPosition> ControlGridPositionDictionary = new Dictionary<UIControl, GridPosition>();

        private static GridPosition GetGridPosition(UIControl control)
        {
            GridPosition gridPosition = new GridPosition();
            if (ControlGridPositionDictionary.ContainsKey(control))
                gridPosition = ControlGridPositionDictionary[control];
            return gridPosition;
        }

        public static void SetRow(UIControl control, int row)
        {
            var gridPosition = GetGridPosition(control);
            gridPosition.Row = row;
            ControlGridPositionDictionary[control] = gridPosition;
        }


        public static void SetColumn(UIControl control, int column)
        {
            var gridPosition = GetGridPosition(control);
            gridPosition.Column = column;
            ControlGridPositionDictionary[control] = gridPosition;
        }
        public static void SetRowSpan(UIControl control, int rowSpan)
        {
            var gridPosition = GetGridPosition(control);
            gridPosition.RowSpan = rowSpan;
            ControlGridPositionDictionary[control] = gridPosition;
        }
        public static void SetColumnSpan(UIControl control, int columnSpan)
        {
            var gridPosition = GetGridPosition(control);
            gridPosition.ColumnSpan = columnSpan;
            ControlGridPositionDictionary[control] = gridPosition;
        }

        public static int GetRow(UIControl control)
        {
            return GetGridPosition(control).Row;
        }
        public static int GetColumn(UIControl control)
        {
            return GetGridPosition(control).Column;
        }
        public static int GetRowSpan(UIControl control)
        {
            return GetGridPosition(control).RowSpan;
        }
        public static int GetColumnSpan(UIControl control)
        {
            return GetGridPosition(control).ColumnSpan;
        }

        public Grid(Game game) : base(game)
        {
        }

        public void AddChild(UIControl child, int row = 0, int column = 0, int rowSpan = 0, int columnSpan = 0)
        {
            base.AddChild(child);
            SetRow(child, row);
            SetColumn(child, column);
            SetRowSpan(child, rowSpan);
            SetColumnSpan(child, columnSpan);
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rectangle rect)
        {
            spriteBatch.Draw(BackgroundTexture, rect, BackgroundColor);

            var singleWidth = (int) (Width/ColumnDefinitions.Count);
            var singleHeight = (int) (Height/RowDefinitions.Count);

            foreach (var child in Children)
            {
                
                    var row = Math.Min(GetRow(child), RowDefinitions.Count);
                    var column = Math.Min(GetColumn(child), ColumnDefinitions.Count);
                    var rowSpan = Math.Max(GetRowSpan(child), 1);
                    var columnSpan = Math.Max(GetColumnSpan(child), 1);

                    var newRect = new Rectangle(column * singleWidth + rect.X, row * singleHeight + rect.Y, singleWidth * columnSpan, singleHeight * rowSpan);

                    child.OnDraw(spriteBatch, newRect);
            }
        }

        protected internal override void OnUpdate(TimeSpan elapsed, Rectangle rect)
        {
            base.OnUpdate(elapsed, rect);
            var singleWidth = (int)(Width / ColumnDefinitions.Count);
            var singleHeight = (int)(Height / RowDefinitions.Count);
            foreach (var child in Children)
            {
                var row = Math.Min( GetRow(child), RowDefinitions.Count);
                var column = Math.Min(GetColumn(child), ColumnDefinitions.Count);
                var rowSpan = Math.Max(GetRowSpan(child), 1);
                var columnSpan = Math.Max(GetColumnSpan(child), 1);

                var newRect = new Rectangle(column * singleWidth + rect.X, row * singleHeight + rect.Y, singleWidth * columnSpan, singleHeight* rowSpan);
                child.OnUpdate(elapsed, newRect);
            }
        }
    }

    public class ColumnDefinition
    {
        public GridLength Width { get; set; }
    }

    public class RowDefinition
    {
        public GridLength Height { get; set; }
    }

    public struct GridLength
    {
        public GridLengthUnit Unit { get; set; }  

        public float Pixels { get; set; }
    }

    public enum GridLengthUnit
    {
        Auto, Absolute, Relative
    }
}