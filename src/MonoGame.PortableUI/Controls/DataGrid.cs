using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    /// A sortable, scrollable table control with typed columns. Rows are materialized from the plain
    /// <see cref="Items"/> list (there is no data-binding layer, matching <see cref="ListBox"/>), each
    /// column is described by a <see cref="DataGridColumn"/>. Supports click-to-sort headers,
    /// row selection with keyboard navigation, per-column resize splitters, and custom cell templates.
    ///
    /// Composition mirrors <see cref="ListBox"/>: a non-scrolling header row plus a
    /// <see cref="ScrollViewer"/> hosting a vertical stack of row controls. Column widths use the same
    /// Auto/Absolute/star semantics as <see cref="Grid"/>.
    /// </summary>
    public class DataGrid : Control
    {
        private readonly HeaderControl _header;
        private readonly StackPanel _rowsPanel;
        private readonly ScrollViewer _scrollViewer;
        private readonly Grid _bodyGrid;
        private readonly ScrollViewer _horizontalScroll;
        private readonly List<RowControl> _rows = new List<RowControl>();

        private int _selectedIndex = -1;
        private DataGridColumn? _sortColumn;
        private bool _sortAscending = true;
        private bool _columnsDirty = true;

        private float _rowHeight;
        private float _headerHeight = 32;

        public DataGrid()
        {
            var theme = PortableTheme.ResolveCurrent();

            Items = new List<object>();
            Columns = new List<DataGridColumn>();

            // The library's ScrollViewer is single-axis, so 2D scrolling is composed by nesting: an
            // outer horizontal ScrollViewer holds a vertical stack of [header, inner vertical ScrollViewer].
            // Vertical scrolling moves only the rows (header stays put); horizontal scrolling moves the
            // whole block, so the header scrolls in sync with the rows.
            _rowsPanel = new StackPanel { Orientation = Orientation.Vertical };
            _scrollViewer = new ScrollViewer
            {
                Content = _rowsPanel,
                ScrollOrientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _header = new HeaderControl(this);
            // Auto row sizes to the header height; the star row gives the vertical scroller the rest.
            _bodyGrid = new Grid();
            _bodyGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _bodyGrid.RowDefinitions.Add(new RowDefinition());
            _bodyGrid.AddChild(_header, row: 0);
            _bodyGrid.AddChild(_scrollViewer, row: 1);
            _horizontalScroll = new ScrollViewer
            {
                Parent = this,
                Content = _bodyGrid,
                ScrollOrientation = Orientation.Horizontal
            };

            _rowHeight = theme.ListBoxItemHeight;
            HeaderBackgroundBrush = theme.TabHeaderBackgroundBrush;
            HeaderTextColor = theme.TabHeaderTextColor;
            RowBackgroundBrush = theme.ListBoxItemBackgroundBrush;
            AlternateRowBackgroundBrush = theme.ListBoxItemBackgroundBrush;
            SelectedRowBackgroundBrush = theme.ListBoxSelectedItemBackgroundBrush;
            RowTextColor = theme.ListBoxItemTextColor;
            SelectedRowTextColor = theme.ListBoxSelectedItemTextColor;
            GridLinesBrush = new SolidColorBrush(new Color(0, 0, 0, 28));

            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            ShowFocusVisual = false;
            KeyPressed += DataGridKeyPressed;
        }

        /// <summary>The rows to display. Populate this then call <see cref="Refresh"/>.</summary>
        public List<object> Items { get; }

        /// <summary>The columns to display. Populate this then call <see cref="Refresh"/>.</summary>
        public List<DataGridColumn> Columns { get; }

        public Brush HeaderBackgroundBrush { get; set; }
        public Color HeaderTextColor { get; set; }
        public Brush RowBackgroundBrush { get; set; }
        public Brush AlternateRowBackgroundBrush { get; set; }
        public Brush SelectedRowBackgroundBrush { get; set; }
        public Color RowTextColor { get; set; }
        public Color SelectedRowTextColor { get; set; }
        public Brush GridLinesBrush { get; set; }
        public bool ShowGridLines { get; set; } = true;
        public bool ShowColumnHeaders { get; set; } = true;

        /// <summary>
        /// When true (default), a horizontal scrollbar appears if the columns are wider than the
        /// grid. Set false to keep columns fixed to the visible width (content is clipped instead).
        /// Grids whose columns include a star (relative) width fill the available width and never
        /// scroll horizontally.
        /// </summary>
        public bool AllowHorizontalScroll { get; set; } = true;

        /// <summary>
        /// Width the header/rows are laid out at: the viewport width normally, or the total column
        /// width when horizontal scrolling is active. Set each layout pass; read by the header/rows.
        /// </summary>
        internal float InnerWidth { get; private set; }

        public float RowHeight
        {
            get { return _rowHeight; }
            set
            {
                if (Math.Abs(_rowHeight - value) < float.Epsilon)
                    return;
                _rowHeight = Math.Max(1, value);
                InvalidateLayout(true);
            }
        }

        public float HeaderHeight
        {
            get { return _headerHeight; }
            set
            {
                if (Math.Abs(_headerHeight - value) < float.Epsilon)
                    return;
                _headerHeight = Math.Max(0, value);
                InvalidateLayout(true);
            }
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                var clamped = ClampIndex(value);
                if (_selectedIndex == clamped)
                    return;
                _selectedIndex = clamped;
                UpdateRowVisuals();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public object? SelectedItem => _selectedIndex >= 0 && _selectedIndex < Items.Count ? Items[_selectedIndex] : null;

        /// <summary>The column the grid is currently sorted by, or null.</summary>
        public DataGridColumn? SortColumn => _sortColumn;

        /// <summary>True when the current sort is ascending.</summary>
        public bool SortAscending => _sortAscending;

        public event EventHandler? SelectionChanged;
        public event EventHandler<DataGridRowInvokedEventArgs>? RowInvoked;

        /// <summary>Rebuilds all rows from <see cref="Items"/> and <see cref="Columns"/>.</summary>
        public void Refresh()
        {
            _columnsDirty = true;
            RebuildRows();
            _header.RebuildLabels();
            InvalidateLayout(true);
        }

        /// <summary>Sorts by the given column, toggling direction if it is already the sort column.</summary>
        public void SortByColumn(DataGridColumn column)
        {
            if (column == null || !column.CanSort)
                return;

            if (ReferenceEquals(_sortColumn, column))
                _sortAscending = !_sortAscending;
            else
            {
                _sortColumn = column;
                _sortAscending = true;
            }

            ApplySort();
        }

        /// <summary>Sorts by the given column in an explicit direction.</summary>
        public void SortBy(DataGridColumn column, bool ascending)
        {
            if (column == null || !column.CanSort)
                return;
            _sortColumn = column;
            _sortAscending = ascending;
            ApplySort();
        }

        private void ApplySort()
        {
            if (_sortColumn == null || Items.Count < 2)
            {
                _header.RebuildLabels();
                RebuildRows();
                InvalidateLayout(true);
                return;
            }

            var selected = SelectedItem;
            var column = _sortColumn;
            var direction = _sortAscending ? 1 : -1;

            // Stable sort: decorate with the original index and break ties on it so equal keys keep
            // their relative order across repeated sorts.
            var ordered = Items
                .Select((item, index) => (item, index))
                .OrderBy(entry => entry, Comparer<(object item, int index)>.Create((a, b) =>
                {
                    var result = CompareValues(column.GetSortValue(a.item), column.GetSortValue(b.item)) * direction;
                    return result != 0 ? result : a.index.CompareTo(b.index);
                }))
                .Select(entry => entry.item)
                .ToList();

            Items.Clear();
            Items.AddRange(ordered);

            _header.RebuildLabels();
            RebuildRows();
            if (selected != null)
                SelectItemByReference(selected);
            InvalidateLayout(true);
        }

        private static int CompareValues(IComparable? a, IComparable? b)
        {
            if (a == null && b == null)
                return 0;
            if (a == null)
                return -1;
            if (b == null)
                return 1;

            try
            {
                if (a.GetType() == b.GetType())
                    return a.CompareTo(b);
            }
            catch (ArgumentException)
            {
                // Fall through to string comparison for values that cannot be compared directly.
            }

            return string.Compare(a.ToString(), b.ToString(), StringComparison.CurrentCultureIgnoreCase);
        }

        internal void SelectRow(int index, bool invoke)
        {
            SelectedIndex = index;
            Focus();
            if (index >= 0 && index < _rows.Count)
                _scrollViewer.BringIntoView(_rows[index]);
            if (invoke)
                InvokeRow(index);
        }

        private void InvokeRow(int index)
        {
            if (index < 0 || index >= Items.Count)
                return;
            RowInvoked?.Invoke(this, new DataGridRowInvokedEventArgs(index, Items[index]));
        }

        private void SelectItemByReference(object item)
        {
            var index = Items.FindIndex(candidate => ReferenceEquals(candidate, item));
            if (index >= 0)
                SelectedIndex = index;
        }

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            EnsureRows();
            var headerRows = ShowColumnHeaders ? HeaderHeight : 0;
            var naturalWidth = Columns.Count == 0 ? 0 : Columns.Sum(NaturalColumnWidth);
            var width = Width.IsFixed() ? Width : naturalWidth;
            var height = Height.IsFixed() ? Height : headerRows + Items.Count * RowHeight;
            return ApplyConstraints(new Size(width, height)) + Margin;
        }

        public override void UpdateLayout(Rect rect)
        {
            if (IsGone)
            {
                BoundingRect = Rect.Empty;
                return;
            }

            EnsureRows();
            _selectedIndex = ClampIndex(_selectedIndex);
            base.UpdateLayout(rect);

            var content = BoundingRect - Margin - BorderThickness;
            ResolveColumnWidths(content.Width);

            var totalColumns = TotalColumnsWidth;
            var horizontalActive = AllowHorizontalScroll && totalColumns > content.Width + 0.5f;
            InnerWidth = horizontalActive ? totalColumns : content.Width;

            // The inner Grid distributes height (Auto header + star body) and the horizontal
            // ScrollViewer constrains that Grid to the viewport height, so no explicit sizing is
            // needed here — setting child sizes mid-layout would re-enter InvalidateLayout.
            _horizontalScroll.UpdateLayout(content);
        }

        public override IEnumerable<Control> GetDescendants()
        {
            EnsureRows();
            yield return _horizontalScroll;
        }

        protected override Brush? GetThemeBackgroundBrush(PortableTheme theme)
        {
            return theme.ListBoxBackgroundBrush;
        }

        protected override ControlStyle? GetThemeStyle(PortableTheme theme)
        {
            return theme.ListBox;
        }

        internal IReadOnlyList<DataGridColumn> ResolvedColumns => Columns;

        /// <summary>Test/inspection hook: the non-scrolling header row.</summary>
        internal HeaderControl HeaderRow => _header;

        /// <summary>Test/inspection hook: the vertical scroll viewer hosting the rows.</summary>
        internal ScrollViewer Scroller => _scrollViewer;

        /// <summary>Test/inspection hook: the outer horizontal scroll viewer.</summary>
        internal ScrollViewer HorizontalScroller => _horizontalScroll;

        /// <summary>Test/inspection hook: the materialized row controls.</summary>
        internal IReadOnlyList<RowControl> Rows
        {
            get
            {
                EnsureRows();
                return _rows;
            }
        }

        internal float ColumnOffset(int columnIndex)
        {
            var offset = 0f;
            for (var i = 0; i < columnIndex && i < Columns.Count; i++)
                offset += Columns[i].ActualWidth;
            return offset;
        }

        internal float TotalColumnsWidth => Columns.Sum(column => column.ActualWidth);

        private void EnsureRows()
        {
            _rowsPanel.SuppressUpdate(true);
            try
            {
                while (_rows.Count > Items.Count)
                {
                    var last = _rows[_rows.Count - 1];
                    _rowsPanel.Children.Remove(last);
                    _rows.RemoveAt(_rows.Count - 1);
                }

                while (_rows.Count < Items.Count)
                {
                    var row = new RowControl(this);
                    _rows.Add(row);
                    _rowsPanel.AddChild(row);
                }

                for (var i = 0; i < _rows.Count; i++)
                    _rows[i].SetItem(Items[i], i, _columnsDirty);
            }
            finally
            {
                _rowsPanel.SuppressUpdate(false);
            }

            _columnsDirty = false;

            var clamped = ClampIndex(_selectedIndex);
            if (_selectedIndex != clamped)
                _selectedIndex = clamped;

            UpdateRowVisuals();
        }

        private void RebuildRows()
        {
            _rowsPanel.SuppressUpdate(true);
            try
            {
                foreach (var row in _rows)
                    _rowsPanel.Children.Remove(row);
                _rows.Clear();
            }
            finally
            {
                _rowsPanel.SuppressUpdate(false);
            }

            EnsureRows();
        }

        private void UpdateRowVisuals()
        {
            for (var i = 0; i < _rows.Count; i++)
                _rows[i].ApplyVisualState(i == _selectedIndex);
        }

        private int ClampIndex(int value)
        {
            if (Items.Count == 0 || value < 0)
                return -1;
            return Math.Max(0, Math.Min(value, Items.Count - 1));
        }

        private void DataGridKeyPressed(object? sender, KeyEventArgs args)
        {
            if (args.InputType != InputType.Command || Items.Count == 0)
                return;

            switch (args.Command)
            {
                case KeyboardCommand.CursorUp:
                    SelectRow(SelectedIndex < 0 ? 0 : Math.Max(0, SelectedIndex - 1), false);
                    break;
                case KeyboardCommand.CursorDown:
                    SelectRow(SelectedIndex < 0 ? 0 : Math.Min(Items.Count - 1, SelectedIndex + 1), false);
                    break;
                case KeyboardCommand.Enter:
                    if (SelectedIndex >= 0)
                        InvokeRow(SelectedIndex);
                    break;
            }
        }

        private static float NaturalColumnWidth(DataGridColumn column)
        {
            return column.Width.Unit == GridLengthUnit.Absolute
                ? Math.Max(column.MinWidth, column.Width.Value)
                : Math.Max(column.MinWidth, MeasureTextWidth(column.Header) + CellHorizontalPadding * 2);
        }

        private void ResolveColumnWidths(float availableWidth)
        {
            if (Columns.Count == 0)
                return;

            var starTotal = 0f;
            var fixedTotal = 0f;

            foreach (var column in Columns)
            {
                if (column.ResizeOverride.HasValue)
                {
                    column.ActualWidth = Math.Max(column.MinWidth, column.ResizeOverride.Value);
                    fixedTotal += column.ActualWidth;
                    continue;
                }

                switch (column.Width.Unit)
                {
                    case GridLengthUnit.Absolute:
                        column.ActualWidth = Math.Max(column.MinWidth, column.Width.Value);
                        fixedTotal += column.ActualWidth;
                        break;
                    case GridLengthUnit.Auto:
                        column.ActualWidth = Math.Max(column.MinWidth, MeasureAutoColumnWidth(column));
                        fixedTotal += column.ActualWidth;
                        break;
                    case GridLengthUnit.Relative:
                        starTotal += Math.Max(0.0001f, column.Width.Value);
                        column.ActualWidth = 0; // resolved below
                        break;
                }
            }

            var leftover = availableWidth.IsFixed() ? Math.Max(0, availableWidth - fixedTotal) : 0;
            var perStar = starTotal > 0 ? leftover / starTotal : 0;
            foreach (var column in Columns)
            {
                if (!column.ResizeOverride.HasValue && column.Width.Unit == GridLengthUnit.Relative)
                    column.ActualWidth = Math.Max(column.MinWidth, Math.Max(0.0001f, column.Width.Value) * perStar);
            }
        }

        private float MeasureAutoColumnWidth(DataGridColumn column)
        {
            var max = MeasureTextWidth(column.Header);
            foreach (var item in Items)
                max = Math.Max(max, MeasureTextWidth(column.GetText(item)));
            return max + CellHorizontalPadding * 2;
        }

        internal const float CellHorizontalPadding = 8;

        internal static float MeasureTextWidth(string? text)
        {
            var font = FontManager.DefaultFont;
            if (font != null)
                return font.MeasureString(text ?? "").X;
            return (text?.Length ?? 0) * 7f;
        }

        internal static void FillRect(SpriteBatch spriteBatch, Brush? brush, Rect rect, float opacity)
        {
            if (brush == null || rect.Width <= 0 || rect.Height <= 0)
                return;
            brush.Draw(spriteBatch, rect, opacity);
        }
    }
}
