using System;
using System.Collections.Generic;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     Item host for <see cref="ListBox"/> (and other item controls) inside a
    ///     <see cref="ScrollViewer"/>. Virtualizing, it reports the height of all rows but only keeps
    ///     controls for the rows in view (plus <see cref="Overscan"/> on each side), recycling them as
    ///     the list scrolls; rows share one height. Not virtualizing, every row is realized and
    ///     stacked at its own measured height, like a vertical <see cref="StackPanel"/>.
    /// </summary>
    internal sealed class VirtualItemsPanel : Control
    {
        private readonly List<Control> _realized = new List<Control>();
        private readonly Stack<Control> _pool = new Stack<Control>();
        private readonly Func<int> _count;
        private readonly Func<Control> _create;
        private readonly Action<Control, int> _bind;
        private readonly Action<Control>? _recycle;
        private int _first;
        private bool _virtualizing = true;
        private bool _realizing;

        /// <summary>Rows realized before the first layout tells the panel its viewport.</summary>
        internal const int InitialRealizeCount = 40;

        /// <param name="count">Number of items.</param>
        /// <param name="create">Creates an empty row control.</param>
        /// <param name="bind">Shows item <c>index</c> in a (new or recycled) row control.</param>
        /// <param name="recycle">Optional reset when a row leaves the view (hover, pressed state).</param>
        public VirtualItemsPanel(Func<int> count, Func<Control> create, Action<Control, int> bind, Action<Control>? recycle = null)
        {
            _count = count;
            _create = create;
            _bind = bind;
            _recycle = recycle;
        }

        /// <summary>Rows kept beyond each edge of the view, so short scrolls need no rebinding.</summary>
        public int Overscan { get; set; } = 3;

        /// <summary>Minimum row height; virtualizing rows use max(this, the first row's measured height).</summary>
        public float MinRowHeight { get; set; }

        public bool IsVirtualizing
        {
            get => _virtualizing;
            set
            {
                if (_virtualizing == value)
                    return;
                _virtualizing = value;
                InvalidateLayout(true);
            }
        }

        /// <summary>Uniform row height used while virtualizing (from the last measure).</summary>
        public float RowHeight { get; private set; }

        /// <summary>Item index of <see cref="Realized"/>[0].</summary>
        public int FirstRealizedIndex => _first;

        /// <summary>Realized row controls in item order (contiguous from <see cref="FirstRealizedIndex"/>).</summary>
        public IReadOnlyList<Control> Realized => _realized;

        /// <summary>The realized row for <paramref name="index"/>, or null when it is not in view.</summary>
        public Control? RowFor(int index)
        {
            var i = index - _first;
            return i >= 0 && i < _realized.Count ? _realized[i] : null;
        }

        /// <summary>True when the realized rows no longer match the item count (items added or removed).</summary>
        public bool NeedsSync
        {
            get
            {
                var count = _count();
                if (!_virtualizing)
                    return _realized.Count != count;
                return _first + _realized.Count > count || (_realized.Count == 0 && count > 0);
            }
        }

        /// <summary>Drops every realized and pooled row (e.g. after the row structure changed).</summary>
        public void Clear()
        {
            foreach (var row in _realized)
                row.Parent = null;
            _realized.Clear();
            _pool.Clear();
            _first = 0;
            InvalidateLayout(true);
        }

        /// <summary>
        ///     Scrolls <paramref name="viewer"/> so row <paramref name="index"/> is visible, also when the
        ///     row is not realized (virtualizing rows share <see cref="RowHeight"/>, so its position is known).
        /// </summary>
        public void BringIndexIntoView(ScrollViewer viewer, int index)
        {
            if (index < 0 || index >= _count())
                return;
            if (RowFor(index) is { } row)
            {
                viewer.BringIntoView(row);
                return;
            }

            var top = RowTop(index);
            var bottom = top + RowHeight;
            var viewport = viewer.Viewport.Height;
            var offset = viewer.Offset.Y;
            if (top < offset)
                offset = top;
            else if (bottom > offset + viewport)
                offset = bottom - viewport;
            viewer.ScrollTo(new PointF(viewer.Offset.X, offset));
        }

        /// <summary>Re-binds every realized row (after in-place item edits).</summary>
        public void RebindAll()
        {
            for (var i = 0; i < _realized.Count; i++)
                _bind(_realized[i], _first + i);
        }

        /// <summary>Realizes rows for the current item count and view, keeping rows whose index stays.</summary>
        public void Sync()
        {
            var count = _count();
            int from, to;
            if (!_virtualizing)
            {
                from = 0;
                to = count;
            }
            else
            {
                GetVisibleRange(count, out from, out to);
            }
            Realize(from, to);
        }

        /// <summary>Top of row <paramref name="index"/> relative to the panel (virtualizing: index × row height).</summary>
        public float RowTop(int index)
        {
            if (_virtualizing)
                return index * RowHeight;
            var row = RowFor(index);
            return row != null ? row.BoundingRect.Top - BoundingRect.Top : 0;
        }

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            Sync();
            var count = _count();
            float width = 0;
            float height = 0;
            for (var i = 0; i < _realized.Count; i++)
            {
                var size = _realized[i].Measure();
                width = Math.Max(width, size.Width);
                if (!_virtualizing)
                    height += size.Height;
            }

            if (_virtualizing)
            {
                RowHeight = Math.Max(1, Math.Max(MinRowHeight, _realized.Count > 0 ? _realized[0].Measure().Height : MinRowHeight));
                height = count * RowHeight;
            }

            return new Size(Width.IsFixed() ? Width : width, Height.IsFixed() ? Height : height) + Margin;
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            Sync();
            ArrangeRows();
        }

        internal override void OffsetArrangement(PointF delta)
        {
            base.OffsetArrangement(delta);
            if (!_virtualizing)
                return;

            // Scrolling shifts the arranged rows without a layout pass; realize the rows that came
            // into view (and arrange them) only when the visible range left the realized one.
            GetVisibleRange(_count(), out var from, out var to);
            if (from < _first || to > _first + _realized.Count)
            {
                Realize(from, to);
                ArrangeRows();
            }
        }

        private void ArrangeRows()
        {
            var box = BoundingRect - Margin;
            if (_virtualizing)
            {
                for (var i = 0; i < _realized.Count; i++)
                    _realized[i].UpdateLayout(new Rect(box.Left, box.Top + (_first + i) * RowHeight, box.Width, RowHeight));
                return;
            }

            var top = box.Top;
            for (var i = 0; i < _realized.Count; i++)
            {
                var height = _realized[i].Measure().Height;
                _realized[i].UpdateLayout(new Rect(box.Left, top, box.Width, height));
                top += height;
            }
        }

        private void GetVisibleRange(int count, out int from, out int to)
        {
            var rowHeight = RowHeight > 0 ? RowHeight : Math.Max(1, MinRowHeight);
            var viewport = Parent is Control { } host ? host.ClippingRect : Rect.Empty;
            var box = BoundingRect - Margin;
            if (viewport.Height <= 0 || box.Height <= 0 || float.IsInfinity(viewport.Height))
            {
                // Not laid out yet: realize a first screenful so measuring has real rows.
                from = 0;
                to = Math.Min(count, Math.Max(_first + _realized.Count, InitialRealizeCount));
                return;
            }

            var first = (int)Math.Floor((viewport.Top - box.Top) / rowHeight);
            var last = (int)Math.Ceiling((viewport.Bottom - box.Top) / rowHeight);
            from = Math.Max(0, Math.Min(count, first - Overscan));
            to = Math.Max(from, Math.Min(count, last + Overscan));
        }

        private void Realize(int from, int to)
        {
            if (from == _first && to == _first + _realized.Count)
                return;

            // Rows outside [from, to) go back to the pool; rows inside keep their control.
            var kept = new Dictionary<int, Control>();
            for (var i = 0; i < _realized.Count; i++)
            {
                var index = _first + i;
                if (index >= from && index < to)
                    kept[index] = _realized[i];
                else
                    Release(_realized[i]);
            }

            _realized.Clear();
            // Virtualizing rows get a fixed slot from ArrangeRows right after this, so rebinding a
            // recycled row (new text) must not schedule a layout pass for the whole screen.
            _realizing = _virtualizing;
            try
            {
                for (var index = from; index < to; index++)
                {
                    if (!kept.TryGetValue(index, out var row))
                    {
                        row = _pool.Count > 0 ? _pool.Pop() : _create();
                        row.Parent = this;
                        _bind(row, index);
                    }
                    _realized.Add(row);
                }
            }
            finally
            {
                _realizing = false;
            }

            _first = from;
            InvalidateLayout(false);
        }

        private void Release(Control row)
        {
            _recycle?.Invoke(row);
            row.Parent = null;
            _pool.Push(row);
        }

        public override void InvalidateLayout(bool boundsChanged)
        {
            if (_realizing)
                return;
            base.InvalidateLayout(boundsChanged);
        }

        public override IEnumerable<Control> GetDescendants() => _realized;

        protected internal override int VisualChildCount => _realized.Count;

        protected internal override Control GetVisualChild(int index) => _realized[index];
    }
}
