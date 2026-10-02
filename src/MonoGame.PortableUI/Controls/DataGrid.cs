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
    /// <see cref="ScrollViewer"/> hosting the rows; only the rows in view are realized while
    /// <see cref="IsVirtualizing"/> is on (default). Column widths use the same
    /// Auto/Absolute/star semantics as <see cref="Grid"/>.
    /// </summary>
    public class DataGrid : Control
    {
        private readonly HeaderControl _header;
        private readonly VirtualItemsPanel _rowsPanel;
        private readonly ScrollViewer _scrollViewer;
        private readonly Grid _bodyGrid;
        private readonly ScrollViewer _horizontalScroll;
        private readonly List<RowControl> _rows = new List<RowControl>();
        // Display position -> Items index. Sorting reorders this list only; the caller's Items
        // list is never mutated, so external indices into Items stay valid across sorts.
        private readonly List<int> _displayOrder = new List<int>();

        private int _selectedIndex = -1;
        // SelectedIndex set while Items is still empty would clamp to -1 and be lost; it is kept
        // here and applied once items arrive (on the next layout pass).
        private int _pendingSelectedIndex = -1;
        private Color _headerTextColor;
        private Color _rowTextColor;
        private Color _selectedRowTextColor;
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
            _rowsPanel = new VirtualItemsPanel(
                () => Items.Count,
                () => new RowControl(this),
                (row, displayIndex) => BindRow((RowControl)row, displayIndex),
                row => row.ResetInputs());
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
            HeaderBackgroundBrush = theme.DataGridHeaderBackgroundBrush;
            HeaderTextColor = theme.DataGridHeaderTextColor;
            RowBackgroundBrush = theme.ListBoxItemBackgroundBrush;
            AlternateRowBackgroundBrush = theme.DataGridAlternateRowBackgroundBrush;
            SelectedRowBackgroundBrush = theme.ListBoxSelectedItemBackgroundBrush;
            RowTextColor = theme.ListBoxItemTextColor;
            SelectedRowTextColor = theme.ListBoxSelectedItemTextColor;
            GridLinesBrush = theme.DataGridGridLinesBrush;

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
        public Color HeaderTextColor
        {
            get => _headerTextColor;
            set
            {
                if (_headerTextColor == value)
                    return;
                _headerTextColor = value;
                _header?.ApplyTextColor();
            }
        }
        public Brush RowBackgroundBrush { get; set; }
        public Brush AlternateRowBackgroundBrush { get; set; }
        public Brush SelectedRowBackgroundBrush { get; set; }
        // Row and header text colors are snapshotted into cell TextBlocks, so changing them must
        // push the new value instead of waiting for the next Refresh().
        public Color RowTextColor
        {
            get => _rowTextColor;
            set
            {
                if (_rowTextColor == value)
                    return;
                _rowTextColor = value;
                if (_rows.Count > 0)
                    UpdateRowVisuals();
            }
        }

        public Color SelectedRowTextColor
        {
            get => _selectedRowTextColor;
            set
            {
                if (_selectedRowTextColor == value)
                    return;
                _selectedRowTextColor = value;
                if (_rows.Count > 0)
                    UpdateRowVisuals();
            }
        }
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

        // Probe in the cells' default font and size: rows never get shorter than one line of text,
        // so larger text (TextScaling, density, a dynamic font) does not overlap the next row.
        private TextBlock? _lineProbe;
        private const float RowTextPadding = 4;

        private float TextLineHeight => (_lineProbe ??= new TextBlock()).CurrentLineHeight + RowTextPadding;

        /// <summary><see cref="RowHeight"/>, raised to fit one line of cell text.</summary>
        public float EffectiveRowHeight => Math.Max(RowHeight, TextLineHeight);

        /// <summary><see cref="HeaderHeight"/> (0 stays 0), raised to fit one line of header text.</summary>
        public float EffectiveHeaderHeight => HeaderHeight <= 0 ? 0 : Math.Max(HeaderHeight, TextLineHeight);

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

        protected internal override bool HandlesDirection(FocusDirection direction)
        {
            return direction is FocusDirection.Up or FocusDirection.Down;
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _pendingSelectedIndex = Items.Count == 0 && value >= 0 ? value : -1;
                var clamped = ClampIndex(value);
                if (_selectedIndex == clamped)
                    return;
                var oldIndex = _selectedIndex;
                _selectedIndex = clamped;
                UpdateRowVisuals();
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(oldIndex, clamped));
            }
        }

        /// <summary>Items is a plain list, so removals are only noticed in the layout pass; clamp the
        /// selection there and report it like an explicit change (same as ListBox).</summary>
        private void SyncSelectionToItems()
        {
            if (_pendingSelectedIndex >= 0 && Items.Count > 0)
            {
                SelectedIndex = _pendingSelectedIndex;
                return;
            }

            var clamped = ClampIndex(_selectedIndex);
            if (_selectedIndex == clamped)
                return;
            var oldIndex = _selectedIndex;
            _selectedIndex = clamped;
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(oldIndex, clamped));
        }

        public object? SelectedItem => _selectedIndex >= 0 && _selectedIndex < Items.Count ? Items[_selectedIndex] : null;

        /// <summary>The column the grid is currently sorted by, or null.</summary>
        public DataGridColumn? SortColumn => _sortColumn;

        /// <summary>True when the current sort is ascending.</summary>
        public bool SortAscending => _sortAscending;

        public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;
        public event EventHandler<DataGridRowInvokedEventArgs>? RowInvoked;

        /// <summary>Rebuilds all rows from <see cref="Items"/> and <see cref="Columns"/>.</summary>
        public void Refresh()
        {
            _columnsDirty = true;
            RebuildDisplayOrder();
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
            RebuildDisplayOrder();
            _header.RebuildLabels();
            RebuildRows();
            InvalidateLayout(true);
        }

        private void EnsureDisplayOrder()
        {
            if (_displayOrder.Count != Items.Count)
                RebuildDisplayOrder();
        }

        private void RebuildDisplayOrder()
        {
            _displayOrder.Clear();
            for (var i = 0; i < Items.Count; i++)
                _displayOrder.Add(i);

            if (_sortColumn == null || Items.Count < 2)
                return;

            var column = _sortColumn;
            var direction = _sortAscending ? 1 : -1;
            // Ties break on the original index, making the sort stable across repeated sorts.
            _displayOrder.Sort((a, b) =>
            {
                var result = CompareValues(column.GetSortValue(Items[a]), column.GetSortValue(Items[b])) * direction;
                return result != 0 ? result : a.CompareTo(b);
            });
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

        /// <summary>Selects the row at a display position (rows are shown in sort order, so the
        /// display position maps through the display order to an <see cref="Items"/> index).</summary>
        internal void SelectRow(int displayIndex, bool invoke)
        {
            EnsureDisplayOrder();
            var itemIndex = displayIndex >= 0 && displayIndex < _displayOrder.Count ? _displayOrder[displayIndex] : -1;
            SelectedIndex = itemIndex;
            Focus();
            if (displayIndex >= 0)
                _rowsPanel.BringIndexIntoView(_scrollViewer, displayIndex);
            if (invoke)
                InvokeRow(itemIndex);
        }

        private void InvokeRow(int index)
        {
            if (index < 0 || index >= Items.Count)
                return;
            RowInvoked?.Invoke(this, new DataGridRowInvokedEventArgs(index, Items[index]));
        }

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            EnsureRows();
            var headerRows = ShowColumnHeaders ? EffectiveHeaderHeight : 0;
            var naturalWidth = 0f;
            foreach (var column in Columns)
                naturalWidth += NaturalColumnWidth(column);
            var width = Width.IsFixed() ? Width : naturalWidth;
            var height = Height.IsFixed() ? Height : headerRows + Items.Count * EffectiveRowHeight;
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
            SyncSelectionToItems();
            base.UpdateLayout(rect);

            var content = BoundingRect - Margin - BorderThickness;
            ResolveColumnWidths(content.Width);

            var totalColumns = TotalColumnsWidth;
            var horizontalActive = AllowHorizontalScroll && totalColumns > content.Width + 0.5f;
            // Columns that fit: no horizontal bar and no sideways scrolling at all; it only becomes
            // possible once the table is wider than its space.
            _horizontalScroll.ShowScrollBars = horizontalActive;
            _horizontalScroll.ScrollDirections = horizontalActive ? ScrollDirections.Horizontal : ScrollDirections.None;
            if (!horizontalActive && _horizontalScroll.Offset.X != 0)
                _horizontalScroll.ScrollTo(new PointF(0, 0));
            InnerWidth = horizontalActive ? totalColumns : content.Width;

            // The inner Grid distributes height (Auto header + star body) and the horizontal
            // ScrollViewer constrains that Grid to the viewport height, so no explicit sizing is
            // needed here — setting child sizes mid-layout would re-enter InvalidateLayout.
            _horizontalScroll.UpdateLayout(content);
        }

        public override IEnumerable<Control> GetDescendants()
        {
            // GetDescendants runs several times per frame (draw + input walks); the full row sync
            // belongs to the layout pass. Only a structural mismatch (items added/removed without
            // an invalidation) forces a rebuild here. In-place item edits need Refresh().
            if (_rowsPanel.NeedsSync)
                EnsureRows();
            yield return _horizontalScroll;
        }

        protected internal override int VisualChildCount
        {
            get
            {
                if (_rowsPanel.NeedsSync)
                    EnsureRows();
                return 1;
            }
        }

        protected internal override Control GetVisualChild(int index) => _horizontalScroll;

        protected override Brush? GetThemeBackgroundBrush(PortableTheme theme)
        {
            return theme.ListBoxBackgroundBrush;
        }

        protected override ControlStyle? GetThemeStyle(PortableTheme theme)
        {
            return theme.ListBox;
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

            if (Math.Abs(_rowHeight - oldTheme.ListBoxItemHeight) < float.Epsilon)
                _rowHeight = newTheme.ListBoxItemHeight;
            if (ReferenceEquals(HeaderBackgroundBrush, oldTheme.DataGridHeaderBackgroundBrush))
                HeaderBackgroundBrush = newTheme.DataGridHeaderBackgroundBrush;
            if (HeaderTextColor.Equals(oldTheme.DataGridHeaderTextColor))
                HeaderTextColor = newTheme.DataGridHeaderTextColor;
            if (ReferenceEquals(RowBackgroundBrush, oldTheme.ListBoxItemBackgroundBrush))
                RowBackgroundBrush = newTheme.ListBoxItemBackgroundBrush;
            if (ReferenceEquals(AlternateRowBackgroundBrush, oldTheme.DataGridAlternateRowBackgroundBrush))
                AlternateRowBackgroundBrush = newTheme.DataGridAlternateRowBackgroundBrush;
            if (ReferenceEquals(SelectedRowBackgroundBrush, oldTheme.ListBoxSelectedItemBackgroundBrush))
                SelectedRowBackgroundBrush = newTheme.ListBoxSelectedItemBackgroundBrush;
            if (RowTextColor.Equals(oldTheme.ListBoxItemTextColor))
                RowTextColor = newTheme.ListBoxItemTextColor;
            if (SelectedRowTextColor.Equals(oldTheme.ListBoxSelectedItemTextColor))
                SelectedRowTextColor = newTheme.ListBoxSelectedItemTextColor;
            if (ReferenceEquals(GridLinesBrush, oldTheme.DataGridGridLinesBrush))
                GridLinesBrush = newTheme.DataGridGridLinesBrush;

            _header.RebuildLabels();
            UpdateRowVisuals();
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

        /// <summary>The items in display (sort) order. Sorting never reorders <see cref="Items"/> itself.</summary>
        public IEnumerable<object> DisplayedItems
        {
            get
            {
                EnsureDisplayOrder();
                foreach (var index in _displayOrder)
                    yield return Items[index];
            }
        }

        internal float ColumnOffset(int columnIndex)
        {
            var offset = 0f;
            for (var i = 0; i < columnIndex && i < Columns.Count; i++)
                offset += Columns[i].ActualWidth;
            return offset;
        }

        internal float TotalColumnsWidth
        {
            get
            {
                var total = 0f;
                for (var i = 0; i < Columns.Count; i++)
                    total += Columns[i].ActualWidth;
                return total;
            }
        }

        /// <summary>
        ///     Only create row controls for the rows in view (default true), so grids with thousands
        ///     of rows stay cheap. Rows already share one height (<see cref="EffectiveRowHeight"/>).
        /// </summary>
        public bool IsVirtualizing
        {
            get => _rowsPanel.IsVirtualizing;
            set
            {
                _rowsPanel.IsVirtualizing = value;
                InvalidateLayout(true);
            }
        }

        private void BindRow(RowControl row, int displayIndex)
        {
            EnsureDisplayOrder();
            row.SetItem(Items[_displayOrder[displayIndex]], displayIndex, false);
            row.ApplyVisualState(_displayOrder[displayIndex] == _selectedIndex);
        }

        private void EnsureRows()
        {
            EnsureDisplayOrder();
            _rowsPanel.MinRowHeight = EffectiveRowHeight;

            // Changed columns: rebuild the cells of the rows already realized; rows realized by
            // Sync below start without cells and build them on their first bind.
            if (_columnsDirty)
            {
                for (var i = 0; i < _rowsPanel.Realized.Count; i++)
                {
                    var displayIndex = _rowsPanel.FirstRealizedIndex + i;
                    if (displayIndex < _displayOrder.Count)
                        ((RowControl)_rowsPanel.Realized[i]).SetItem(Items[_displayOrder[displayIndex]], displayIndex, true);
                }
                _columnsDirty = false;
            }

            _rowsPanel.Sync();

            // Realized rows follow the display order (a sort or an edit can change which item a
            // row shows); SetItem only touches cells when the item changed.
            _rows.Clear();
            for (var i = 0; i < _rowsPanel.Realized.Count; i++)
            {
                var row = (RowControl)_rowsPanel.Realized[i];
                var displayIndex = _rowsPanel.FirstRealizedIndex + i;
                row.SetItem(Items[_displayOrder[displayIndex]], displayIndex, false);
                _rows.Add(row);
            }

            SyncSelectionToItems();
            UpdateRowVisuals();
        }

        private void RebuildRows()
        {
            _rows.Clear();
            _rowsPanel.Clear();
            EnsureRows();
        }

        private void UpdateRowVisuals()
        {
            EnsureDisplayOrder();
            foreach (var row in _rows)
            {
                if (row.Index < _displayOrder.Count)
                    row.ApplyVisualState(_displayOrder[row.Index] == _selectedIndex);
            }
        }

        private int ClampIndex(int value)
        {
            return HelperEx.ClampSelectionIndex(value, Items.Count);
        }

        private void DataGridKeyPressed(object? sender, KeyEventArgs args)
        {
            if (args.InputType != InputType.Command || Items.Count == 0)
                return;

            switch (args.Command)
            {
                case KeyboardCommand.CursorUp:
                    MoveSelection(-1);
                    break;
                case KeyboardCommand.CursorDown:
                    MoveSelection(1);
                    break;
                case KeyboardCommand.PageUp:
                    MoveSelection(-PageSize);
                    break;
                case KeyboardCommand.PageDown:
                    MoveSelection(PageSize);
                    break;
                case KeyboardCommand.Home:
                    MoveSelection(-Items.Count);
                    break;
                case KeyboardCommand.End:
                    MoveSelection(Items.Count);
                    break;
                case KeyboardCommand.Enter:
                    if (SelectedIndex >= 0)
                        InvokeRow(SelectedIndex);
                    break;
            }
        }

        // Rows per visible page (one row of overlap), for PageUp/PageDown.
        private int PageSize => Math.Max(1, (int)(_scrollViewer.BoundingRect.Height / Math.Max(1, EffectiveRowHeight)) - 1);

        /// <summary>Moves the selection through the rows in display (sort) order.</summary>
        private void MoveSelection(int delta)
        {
            EnsureDisplayOrder();
            if (_displayOrder.Count == 0)
                return;

            var displayPosition = _selectedIndex < 0 ? -1 : _displayOrder.IndexOf(_selectedIndex);
            var target = displayPosition < 0
                ? 0
                : Math.Max(0, Math.Min(_displayOrder.Count - 1, displayPosition + delta));
            SelectRow(target, false);
        }

        // Must agree with ResolveColumnWidths: Auto columns size to their widest cell there, so
        // measuring only the header made auto-sized grids too narrow (spurious h-scrollbar).
        private float NaturalColumnWidth(DataGridColumn column)
        {
            return column.Width.Unit switch
            {
                GridLengthUnit.Absolute => Math.Max(column.MinWidth, column.Width.Value),
                GridLengthUnit.Auto => Math.Max(column.MinWidth, MeasureAutoColumnWidth(column)),
                _ => Math.Max(column.MinWidth, MeasureCellTextWidth(column.Header) + CellHorizontalPadding * 2)
            };
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
            var max = MeasureCellTextWidth(column.Header);
            foreach (var item in Items)
                max = Math.Max(max, MeasureCellTextWidth(column.GetText(item)));
            return max + CellHorizontalPadding * 2;
        }

        internal const float CellHorizontalPadding = 8;

        private float MeasureCellTextWidth(string? text)
        {
            return _header.MeasureTextWidth(text) ?? MeasureTextWidth(text);
        }

        internal static float MeasureTextWidth(string? text)
        {
            var font = FontManager.DefaultFont;
            if (font != null)
                return font.MeasureString(text ?? "").X * Text.TextScaling.Factor;
            return (text?.Length ?? 0) * 7f * Text.TextScaling.Factor;
        }

        internal static void FillRect(SpriteBatch spriteBatch, Brush? brush, Rect rect, float opacity)
        {
            if (brush == null || rect.Width <= 0 || rect.Height <= 0)
                return;
            brush.Draw(spriteBatch, rect, opacity);
        }
    }
}
