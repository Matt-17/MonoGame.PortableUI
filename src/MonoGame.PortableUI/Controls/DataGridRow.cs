using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    /// One materialized row of a <see cref="DataGrid"/>. Holds a cell control per column (a
    /// <see cref="TextBlock"/> by default, or a column's custom <see cref="DataGridColumn.CellTemplate"/>),
    /// draws the row background/gridlines, and selects itself on click.
    /// </summary>
    internal sealed class RowControl : Control
    {
        private readonly DataGrid _owner;
        private readonly List<Control> _cells = new List<Control>();
        private object? _item;
        private bool _selected;

        public RowControl(DataGrid owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            HorizontalAlignment = HorizontalAlignment.Stretch;
            ShowFocusVisual = false;
            Click += (_, _) => _owner.SelectRow(Index, false);
            DoubleClick += (_, _) => _owner.SelectRow(Index, true);
        }

        public int Index { get; private set; }

        public void SetItem(object item, int index, bool rebuildCells)
        {
            Index = index;
            var itemChanged = !ReferenceEquals(_item, item);
            _item = item;

            if (rebuildCells || _cells.Count != _owner.Columns.Count)
            {
                RebuildCells();
            }
            else if (itemChanged)
            {
                UpdateCellText();
            }
        }

        private void RebuildCells()
        {
            _cells.Clear();
            for (var i = 0; i < _owner.Columns.Count; i++)
            {
                var column = _owner.Columns[i];
                Control cell;
                if (column.CellTemplate != null && _item != null)
                {
                    cell = column.CellTemplate(_item);
                }
                else
                {
                    cell = new TextBlock
                    {
                        Text = _item != null ? column.GetText(_item) : "",
                        TextColor = _owner.RowTextColor,
                        TextAlignment = column.CellAlignment
                    };
                }

                cell.Parent = this;
                _cells.Add(cell);
            }

            ApplyVisualState(_selected);
        }

        private void UpdateCellText()
        {
            for (var i = 0; i < _cells.Count && i < _owner.Columns.Count; i++)
            {
                var column = _owner.Columns[i];
                if (_cells[i] is TextBlock textBlock && column.CellTemplate == null && _item != null)
                {
                    textBlock.Text = column.GetText(_item);
                    textBlock.TextAlignment = column.CellAlignment;
                }
            }
        }

        public void ApplyVisualState(bool selected)
        {
            _selected = selected;
            var textColor = selected ? _owner.SelectedRowTextColor : _owner.RowTextColor;
            foreach (var cell in _cells)
            {
                if (cell is TextBlock textBlock)
                    textBlock.TextColor = textColor;
            }
        }

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            var width = _owner.InnerWidth;
            return ApplyConstraints(new Size(width, _owner.RowHeight)) + Margin;
        }

        public override void UpdateLayout(Rect rect)
        {
            if (IsGone)
            {
                BoundingRect = Rect.Empty;
                return;
            }

            base.UpdateLayout(rect);

            const float pad = DataGrid.CellHorizontalPadding;
            for (var i = 0; i < _cells.Count && i < _owner.Columns.Count; i++)
            {
                var width = _owner.Columns[i].ActualWidth;
                var left = BoundingRect.Left + _owner.ColumnOffset(i);
                var cellRect = new Rect(left + pad, BoundingRect.Top, Math.Max(0, width - 2 * pad), BoundingRect.Height);
                _cells[i].UpdateLayout(cellRect);
            }
        }

        public override IEnumerable<Control> GetDescendants()
        {
            return _cells;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            var background = _selected
                ? _owner.SelectedRowBackgroundBrush
                : (Index % 2 == 1 ? _owner.AlternateRowBackgroundBrush : _owner.RowBackgroundBrush);
            DataGrid.FillRect(spriteBatch, background, rect, RenderOpacity);

            if (!_selected && IsMouseHovering)
                DataGrid.FillRect(spriteBatch, HoverOverlay, rect, RenderOpacity);

            if (_owner.ShowGridLines)
                DrawGridLines(spriteBatch, rect);
        }

        private void DrawGridLines(SpriteBatch spriteBatch, Rect rect)
        {
            // Bottom separator between rows.
            DataGrid.FillRect(spriteBatch, _owner.GridLinesBrush, new Rect(rect.Left, rect.Bottom - 1, rect.Width, 1), RenderOpacity);

            // Vertical separators on each column's trailing edge (skip the last one).
            for (var i = 0; i < _owner.Columns.Count - 1; i++)
            {
                var x = rect.Left + _owner.ColumnOffset(i) + _owner.Columns[i].ActualWidth;
                DataGrid.FillRect(spriteBatch, _owner.GridLinesBrush, new Rect(x, rect.Top, 1, rect.Height), RenderOpacity);
            }
        }

        private static readonly Media.SolidColorBrush HoverOverlay = new Media.SolidColorBrush(new Microsoft.Xna.Framework.Color(0, 0, 0, 18));
    }
}
