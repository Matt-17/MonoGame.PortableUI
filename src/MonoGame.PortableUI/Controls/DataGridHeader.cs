using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    /// The non-scrolling header row of a <see cref="DataGrid"/>. Renders a label per column with a
    /// sort glyph, sorts on click, and resizes columns by dragging the splitter on a column's edge.
    /// </summary>
    internal sealed class HeaderControl : Control
    {
        private const float SplitterHitWidth = 6;
        private const float GlyphWidth = 9;
        private const float GlyphHeight = 6;
        private const float GlyphGap = 5;

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
                label.Text = column.Header;
            }
        }

        public override Size MeasureLayout()
        {
            if (IsGone || !_owner.ShowColumnHeaders)
                return Size.Empty;
            return ApplyConstraints(new Size(_owner.InnerWidth, _owner.HeaderHeight)) + Margin;
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
            var sortIndex = SortColumnIndex();
            for (var i = 0; i < _labels.Count && i < _owner.Columns.Count; i++)
            {
                var width = _owner.Columns[i].ActualWidth;
                var left = BoundingRect.Left + _owner.ColumnOffset(i);
                // Reserve room for the sort glyph on the active column so it never overlaps the text.
                var reserve = i == sortIndex ? GlyphWidth + GlyphGap : 0;
                _labels[i].UpdateLayout(new Rect(left + pad, BoundingRect.Top, Math.Max(0, width - 2 * pad - reserve), BoundingRect.Height));
            }
        }

        public override IEnumerable<Control> GetDescendants()
        {
            return _labels;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            if (!_owner.ShowColumnHeaders)
                return;

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

            DrawSortGlyph(spriteBatch, rect);
        }

        private void DrawSortGlyph(SpriteBatch spriteBatch, Rect rect)
        {
            var sortIndex = SortColumnIndex();
            if (sortIndex < 0)
                return;

            const float pad = DataGrid.CellHorizontalPadding;
            var columnRight = rect.Left + _owner.ColumnOffset(sortIndex) + _owner.Columns[sortIndex].ActualWidth;
            var glyphRect = new Rect(
                columnRight - pad - GlyphWidth,
                rect.Top + (rect.Height - GlyphHeight) / 2,
                GlyphWidth,
                GlyphHeight);
            var color = Brush.ApplyOpacity(_owner.HeaderTextColor, RenderOpacity);
            var texture = TriangleGlyph.Get(spriteBatch.GraphicsDevice, pointingUp: _owner.SortAscending);
            spriteBatch.Draw(texture, glyphRect, color);
        }

        private int SortColumnIndex()
        {
            if (_owner.SortColumn == null)
                return -1;
            for (var i = 0; i < _owner.Columns.Count; i++)
            {
                if (ReferenceEquals(_owner.Columns[i], _owner.SortColumn))
                    return i;
            }

            return -1;
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
