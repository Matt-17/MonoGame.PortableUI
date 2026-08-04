using System;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    /// Describes a single column of a <see cref="DataGrid"/>: its header, how wide it is, how each
    /// row's cell renders, and (optionally) how the column sorts. Cells are materialized from the
    /// grid's plain <c>object</c> items — there is no data-binding layer, matching the rest of the
    /// library (see <see cref="ListBox"/>).
    /// </summary>
    public sealed class DataGridColumn
    {
        /// <summary>Text shown in the column header.</summary>
        public string Header { get; set; } = "";

        /// <summary>
        /// Column width. Absolute (pixels), Auto (widest cell/header) or Relative/star (shares the
        /// leftover space) — the same semantics as <see cref="Grid"/> column definitions.
        /// Defaults to one star.
        /// </summary>
        public GridLength Width { get; set; } = new GridLength(1, GridLengthUnit.Relative);

        /// <summary>Smallest width the column may shrink to, including via user resizing.</summary>
        public float MinWidth { get; set; } = 32;

        /// <summary>When true, a resize splitter is drawn on the column's trailing edge.</summary>
        public bool CanResize { get; set; } = true;

        /// <summary>When true, clicking the header sorts by this column.</summary>
        public bool CanSort { get; set; } = true;

        /// <summary>Horizontal alignment of the (default) text cell content.</summary>
        public TextAlignment CellAlignment { get; set; } = TextAlignment.Left;

        /// <summary>
        /// Produces the cell's display text for an item. When null the item's
        /// <see cref="object.ToString"/> is used.
        /// </summary>
        public Func<object, string>? CellText { get; set; }

        /// <summary>
        /// Optional custom cell factory. When set it takes precedence over <see cref="CellText"/>
        /// and lets a column render an arbitrary control per row (modeled on the
        /// ContentControl.ControlTemplate delegate).
        /// </summary>
        public Func<object, Control>? CellTemplate { get; set; }

        /// <summary>
        /// Optional sort key selector. When null and <see cref="CanSort"/> is true, the column sorts
        /// by its display text (case-insensitive).
        /// </summary>
        public Func<object, IComparable?>? SortKey { get; set; }

        /// <summary>Resolved pixel width for the current layout pass (set by <see cref="DataGrid"/>).</summary>
        internal float ActualWidth { get; set; }

        /// <summary>User-resized width override in pixels; null until the column is dragged.</summary>
        internal float? ResizeOverride { get; set; }

        internal string GetText(object item)
        {
            if (CellText != null)
                return CellText(item) ?? "";
            return item?.ToString() ?? "";
        }

        internal IComparable? GetSortValue(object item)
        {
            if (SortKey != null)
                return SortKey(item);
            return GetText(item);
        }
    }
}
