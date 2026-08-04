using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    /// The non-scrolling header row of a <see cref="DataGrid"/>. Renders a label per column with a
    /// sort glyph, sorts on click, and resizes columns by dragging the splitter on a column's edge.
    /// </summary>
    internal sealed class HeaderControl : Control
    {
        private const float SplitterHitWidth = 6;

        private readonly DataGrid _owner;
        private readonly List<TextBlock> _labels = new List<TextBlock>();

        private int _pressColumnIndex = -1;
        private int _resizeColumnIndex = -1;
        private float _resizeStartX;
        private float _resizeStartWidth;

        public HeaderControl(DataGrid owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            HorizontalAlignment = HorizontalAlignment.Stretch;
            ShowFocusVisual = false;
            MouseDown += HeaderMouseDown;
            MouseMove += HeaderMouseMove;
            MouseUp += HeaderMouseUp;
        }

        public void RebuildLabels()
        {
            while (_labels.Count > _owner.Columns.Count)
                _labels.RemoveAt(_labels.Count - 1);
            while (_labels.Count < _owner.Columns.Count)
                _labels.Add(new TextBlock { Parent = this });

            for (var i = 0; i < _owner.Columns.Count; i++)
            {
                var column = _owner.Columns[i];
                var label = _labels[i];
                label.Parent = this;
                label.TextColor = _owner.HeaderTextColor;
                label.TextAlignment = column.CellAlignment;
                // ASCII-only sort glyph: the default font only builds characters 32-126, so avoid
                // unicode arrows which would throw when measured.
                var glyph = ReferenceEquals(_owner.SortColumn, column) ? (_owner.SortAscending ? "  ^" : "  v") : "";
                label.Text = column.Header + glyph;
            }
        }

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;
            return ApplyConstraints(new Size(_owner.TotalColumnsWidth, _owner.HeaderHeight)) + Margin;
        }

        public override void UpdateLayout(Rect rect)
        {
            if (IsGone)
            {
                BoundingRect = Rect.Empty;
                return;
            }

            if (_labels.Count != _owner.Columns.Count)
                RebuildLabels();

            base.UpdateLayout(rect);

            const float pad = DataGrid.CellHorizontalPadding;
            for (var i = 0; i < _labels.Count && i < _owner.Columns.Count; i++)
            {
                var width = _owner.Columns[i].ActualWidth;
                var left = BoundingRect.Left + _owner.ColumnOffset(i);
                _labels[i].UpdateLayout(new Rect(left + pad, BoundingRect.Top, Math.Max(0, width - 2 * pad), BoundingRect.Height));
            }
        }

        public override IEnumerable<Control> GetDescendants()
        {
            return _labels;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            DataGrid.FillRect(spriteBatch, _owner.HeaderBackgroundBrush, rect, RenderOpacity);

            if (_owner.ShowGridLines)
            {
                DataGrid.FillRect(spriteBatch, _owner.GridLinesBrush, new Rect(rect.Left, rect.Bottom - 1, rect.Width, 1), RenderOpacity);
                for (var i = 0; i < _owner.Columns.Count - 1; i++)
                {
                    var x = rect.Left + _owner.ColumnOffset(i) + _owner.Columns[i].ActualWidth;
                    DataGrid.FillRect(spriteBatch, _owner.GridLinesBrush, new Rect(x, rect.Top, 1, rect.Height), RenderOpacity);
                }
            }
        }

        private void HeaderMouseDown(object? sender, MouseEventArgs args)
        {
            if (!args.Buttons.Contains(MouseButton.Left))
                return;

            var localX = args.Position.X - BoundingRect.Left;

            if (TryGetResizeColumn(localX, out var resizeIndex))
            {
                _resizeColumnIndex = resizeIndex;
                _resizeStartX = args.Position.X;
                _resizeStartWidth = _owner.Columns[resizeIndex].ActualWidth;
                CaptureMouse();
                args.Handled = true;
                return;
            }

            _pressColumnIndex = ColumnAtX(localX);
            args.Handled = true;
        }

        private void HeaderMouseMove(object? sender, MouseEventArgs args)
        {
            if (_resizeColumnIndex < 0)
                return;

            var delta = args.Position.X - _resizeStartX;
            var column = _owner.Columns[_resizeColumnIndex];
            column.ResizeOverride = Math.Max(column.MinWidth, _resizeStartWidth + delta);
            _owner.InvalidateLayout(true);
            args.Handled = true;
        }

        private void HeaderMouseUp(object? sender, MouseEventArgs args)
        {
            if (_resizeColumnIndex >= 0)
            {
                _resizeColumnIndex = -1;
                ReleaseMouse();
                args.Handled = true;
                return;
            }

            if (_pressColumnIndex >= 0)
            {
                var localX = args.Position.X - BoundingRect.Left;
                var releaseColumn = ColumnAtX(localX);
                if (releaseColumn == _pressColumnIndex && releaseColumn >= 0 && releaseColumn < _owner.Columns.Count)
                    _owner.SortByColumn(_owner.Columns[releaseColumn]);
            }

            _pressColumnIndex = -1;
            args.Handled = true;
        }

        private bool TryGetResizeColumn(float localX, out int columnIndex)
        {
            var edge = 0f;
            for (var i = 0; i < _owner.Columns.Count; i++)
            {
                edge += _owner.Columns[i].ActualWidth;
                if (_owner.Columns[i].CanResize && Math.Abs(localX - edge) <= SplitterHitWidth)
                {
                    columnIndex = i;
                    return true;
                }
            }

            columnIndex = -1;
            return false;
        }

        private int ColumnAtX(float localX)
        {
            var edge = 0f;
            for (var i = 0; i < _owner.Columns.Count; i++)
            {
                edge += _owner.Columns[i].ActualWidth;
                if (localX < edge)
                    return i;
            }

            return _owner.Columns.Count - 1;
        }
    }
}
