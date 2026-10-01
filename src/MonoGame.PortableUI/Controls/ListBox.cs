using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class ListBox : Control
    {
        private readonly List<Button> _itemButtons;
        private readonly StackPanel _itemsPanel;
        private readonly ScrollViewer _scrollViewer;
        private Brush _itemBackgroundBrush = new SolidColorBrush(Color.White);
        private Color _itemTextColor;
        private float _itemHeight;
        private Thickness _itemPadding;
        private bool _isMouseSelecting;
        private int _mouseSelectionStartIndex = -1;
        private int _selectedIndex = -1;
        // SelectedIndex set while Items is still empty would clamp to -1 and be lost; it is kept
        // here and applied once items arrive (on the next layout pass).
        private int _pendingSelectedIndex = -1;
        private readonly List<object?> _syncedItems = new List<object?>();
        private bool _refreshItemTexts;
        private Brush _selectedItemBackgroundBrush = new SolidColorBrush(new Color(20, 126, 133));
        private Color _selectedItemTextColor;

        public ListBox()
        {
            var theme = PortableTheme.ResolveCurrent();

            Items = new List<object>();
            _itemButtons = new List<Button>();
            _itemsPanel = new StackPanel { Orientation = Orientation.Vertical };
            _scrollViewer = new ScrollViewer
            {
                Parent = this,
                Content = _itemsPanel,
                ScrollOrientation = Orientation.Vertical
            };

            ItemHeight = theme.ListBoxItemHeight;
            ItemPadding = theme.ListBoxItemPadding;
            ItemBackgroundBrush = theme.ListBoxItemBackgroundBrush;
            SelectedItemBackgroundBrush = theme.ListBoxSelectedItemBackgroundBrush;
            ItemTextColor = theme.ListBoxItemTextColor;
            SelectedItemTextColor = theme.ListBoxSelectedItemTextColor;
            ShowFocusVisual = false;
            KeyPressed += ListBoxKeyPressed;
            MouseMove += ListBoxMouseMove;
            MouseUp += ListBoxMouseUp;
        }

        protected override ControlStyle? GetThemeStyle(PortableTheme theme)
        {
            return theme.ListBox;
        }

        protected override Brush? GetThemeBackgroundBrush(PortableTheme theme)
        {
            return theme.ListBoxBackgroundBrush;
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

            if (ItemHeight.Equals(oldTheme.ListBoxItemHeight))
                ItemHeight = newTheme.ListBoxItemHeight;
            if (ItemPadding.Equals(oldTheme.ListBoxItemPadding))
                ItemPadding = newTheme.ListBoxItemPadding;
            if (ReferenceEquals(ItemBackgroundBrush, oldTheme.ListBoxItemBackgroundBrush))
                ItemBackgroundBrush = newTheme.ListBoxItemBackgroundBrush;
            if (ReferenceEquals(SelectedItemBackgroundBrush, oldTheme.ListBoxSelectedItemBackgroundBrush))
                SelectedItemBackgroundBrush = newTheme.ListBoxSelectedItemBackgroundBrush;
            if (ItemTextColor.Equals(oldTheme.ListBoxItemTextColor))
                ItemTextColor = newTheme.ListBoxItemTextColor;
            if (SelectedItemTextColor.Equals(oldTheme.ListBoxSelectedItemTextColor))
                SelectedItemTextColor = newTheme.ListBoxSelectedItemTextColor;
            UpdateItemButtonVisuals();
        }

        public List<object> Items { get; }

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
                // Setting the index selects exactly that item (in every mode).
                _selectedIndices.Clear();
                if (clamped >= 0)
                    _selectedIndices.Add(clamped);
                _rangeAnchor = clamped;
                UpdateItemButtonVisuals();
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(oldIndex, clamped));
            }
        }

        public object? SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

        private readonly List<int> _selectedIndices = new List<int>();
        private SelectionMode _selectionMode = SelectionMode.Single;
        private int _rangeAnchor = -1;
        private Func<object, Control>? _itemTemplate;

        /// <summary>Single (default), Multiple (click toggles) or Extended (Ctrl/Shift+click).</summary>
        public SelectionMode SelectionMode
        {
            get => _selectionMode;
            set
            {
                if (_selectionMode == value)
                    return;
                _selectionMode = value;
                // Leaving a multi mode keeps only the current item.
                if (value == SelectionMode.Single)
                    SetSelection(_selectedIndex >= 0 ? new[] { _selectedIndex } : Array.Empty<int>(), _selectedIndex);
            }
        }

        /// <summary>All selected item indices in ascending order (one entry in Single mode).</summary>
        public IReadOnlyList<int> SelectedIndices => _selectedIndices;

        public IReadOnlyList<object> SelectedItems
        {
            get
            {
                var items = new List<object>(_selectedIndices.Count);
                foreach (var index in _selectedIndices)
                {
                    if (index < Items.Count)
                        items.Add(Items[index]);
                }
                return items;
            }
        }

        /// <summary>
        ///     Builds the visual for each item instead of its <c>ToString()</c> text (icons, two-line
        ///     rows, ...). Called again for an item after it was replaced or after <see cref="Refresh"/>.
        /// </summary>
        public Func<object, Control>? ItemTemplate
        {
            get => _itemTemplate;
            set
            {
                _itemTemplate = value;
                _refreshItemTexts = true;
                InvalidateLayout(true);
            }
        }

        public bool IsSelected(int index) => _selectedIndices.Contains(index);

        private void SetSelection(IEnumerable<int> indices, int current)
        {
            var oldIndex = _selectedIndex;
            var before = _selectedIndices.ToArray();
            _selectedIndices.Clear();
            foreach (var index in indices)
            {
                if (index >= 0 && index < Items.Count && !_selectedIndices.Contains(index))
                    _selectedIndices.Add(index);
            }
            _selectedIndices.Sort();
            _selectedIndex = current >= 0 && current < Items.Count ? current : (_selectedIndices.Count > 0 ? _selectedIndices[0] : -1);

            if (oldIndex == _selectedIndex && before.SequenceEqual(_selectedIndices))
                return;
            UpdateItemButtonVisuals();
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(oldIndex, _selectedIndex));
        }

        /// <summary>Applies a pointer/keyboard selection gesture according to <see cref="SelectionMode"/>.</summary>
        private void SelectWithGesture(int index, bool toggle, bool range)
        {
            if (SelectionMode == SelectionMode.Single || (SelectionMode == SelectionMode.Extended && !toggle && !range))
            {
                _rangeAnchor = index;
                SelectedIndex = index;
                return;
            }

            if (range && SelectionMode == SelectionMode.Extended && _rangeAnchor >= 0)
            {
                var from = Math.Min(_rangeAnchor, index);
                var to = Math.Max(_rangeAnchor, index);
                var indices = new List<int>();
                for (var i = from; i <= to; i++)
                    indices.Add(i);
                SetSelection(indices, index);
                return;
            }

            // Multiple mode click, or Extended Ctrl+click: toggle.
            _rangeAnchor = index;
            var next = new List<int>(_selectedIndices);
            if (!next.Remove(index))
                next.Add(index);
            SetSelection(next, next.Contains(index) ? index : (next.Count > 0 ? next[next.Count - 1] : -1));
        }

        private KeyboardModifiers CurrentModifiers()
        {
            var keyboard = Screen?.InputSource?.KeyboardState ?? default;
            var modifiers = KeyboardModifiers.None;
            if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift))
                modifiers |= KeyboardModifiers.Shift;
            if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) || keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl))
                modifiers |= KeyboardModifiers.Control;
            return modifiers;
        }

        public float ItemHeight
        {
            get { return _itemHeight; }
            set
            {
                if (Math.Abs(_itemHeight - value) < float.Epsilon)
                    return;

                _itemHeight = Math.Max(0, value);
                foreach (var button in _itemButtons)
                    button.Height = _itemHeight;
                InvalidateLayout(true);
            }
        }

        public Thickness ItemPadding
        {
            get { return _itemPadding; }
            set
            {
                _itemPadding = value;
                foreach (var button in _itemButtons)
                    button.Padding = _itemPadding;
                InvalidateLayout(true);
            }
        }

        public Color ItemTextColor
        {
            get { return _itemTextColor; }
            set
            {
                if (_itemTextColor == value)
                    return;

                _itemTextColor = value;
                UpdateItemButtonVisuals();
            }
        }

        public Color SelectedItemTextColor
        {
            get { return _selectedItemTextColor; }
            set
            {
                if (_selectedItemTextColor == value)
                    return;

                _selectedItemTextColor = value;
                UpdateItemButtonVisuals();
            }
        }

        public Brush ItemBackgroundBrush
        {
            get { return _itemBackgroundBrush; }
            set
            {
                _itemBackgroundBrush = value;
                UpdateItemButtonVisuals();
            }
        }

        public Brush SelectedItemBackgroundBrush
        {
            get { return _selectedItemBackgroundBrush; }
            set
            {
                _selectedItemBackgroundBrush = value;
                UpdateItemButtonVisuals();
            }
        }

        public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;
        public event EventHandler<ListBoxItemInvokedEventArgs>? ItemInvoked;

        /// <summary>Scrolls the selected item into view (call after the list has been laid out).</summary>
        public void ScrollSelectedIntoView()
        {
            EnsureItemButtons();
            if (SelectedIndex >= 0 && SelectedIndex < _itemButtons.Count)
                _scrollViewer.BringIntoView(_itemButtons[SelectedIndex]);
        }

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            EnsureItemButtons();
            var contentSize = _itemsPanel.Measure();
            var width = Width.IsFixed() ? Width : contentSize.Width;
            var height = Height.IsFixed() ? Height : contentSize.Height;
            return ApplyConstraints(new Size(width, height)) + Margin;
        }

        public override void UpdateLayout(Rect rect)
        {
            if (IsGone)
            {
                BoundingRect = Rect.Empty;
                return;
            }

            EnsureItemButtons();
            SelectedIndex = ClampIndex(SelectedIndex);
            base.UpdateLayout(rect);
            // Keep the frame visible: items sit inside the themed border instead of covering it.
            _scrollViewer.UpdateLayout(BoundingRect - Margin - BorderThickness);
        }

        public override IEnumerable<Control> GetDescendants()
        {
            // GetDescendants runs several times per frame (draw + input walks); the full item sync
            // belongs to the layout pass. Only a structural mismatch (items added/removed without
            // an invalidation) forces a rebuild here. In-place item edits need Refresh().
            if (_itemButtons.Count != Items.Count)
                EnsureItemButtons();
            yield return _scrollViewer;
        }

        protected internal override int VisualChildCount
        {
            get
            {
                if (_itemButtons.Count != Items.Count)
                    EnsureItemButtons();
                return 1;
            }
        }

        protected internal override Control GetVisualChild(int index) => _scrollViewer;

        /// <summary>Re-syncs the item buttons after mutating <see cref="Items"/> in place
        /// (adding/removing items is picked up automatically on the next layout pass).</summary>
        public void Refresh()
        {
            _refreshItemTexts = true;
            EnsureItemButtons();
            InvalidateLayout(true);
        }

        internal IReadOnlyList<Button> ItemButtons
        {
            get
            {
                EnsureItemButtons();
                return _itemButtons;
            }
        }

        private void EnsureItemButtons()
        {
            _itemsPanel.SuppressUpdate(true);
            try
            {
                while (_itemButtons.Count > Items.Count)
                {
                    var last = _itemButtons[_itemButtons.Count - 1];
                    _itemsPanel.Children.Remove(last);
                    _itemButtons.RemoveAt(_itemButtons.Count - 1);
                }

                while (_itemButtons.Count < Items.Count)
                {
                    var button = CreateItemButton(_itemButtons.Count);
                    _itemButtons.Add(button);
                    _itemsPanel.AddChild(button);
                }
            }
            finally
            {
                _itemsPanel.SuppressUpdate(false);
            }

            if (_pendingSelectedIndex >= 0 && Items.Count > 0)
                SelectedIndex = _pendingSelectedIndex;

            _selectedIndices.RemoveAll(index => index >= Items.Count);
            var clamped = ClampIndex(_selectedIndex);
            if (_selectedIndex != clamped)
            {
                var oldIndex = _selectedIndex;
                _selectedIndex = clamped;
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(oldIndex, clamped));
            }

            // This runs in every measure and arrange; only items that changed (or all after
            // Refresh, for in-place edits) pay for ToString().
            while (_syncedItems.Count > _itemButtons.Count)
                _syncedItems.RemoveAt(_syncedItems.Count - 1);
            for (var i = 0; i < _itemButtons.Count; i++)
            {
                var button = _itemButtons[i];
                button.Tag = i;
                button.Height = ItemHeight;
                var item = Items[i];
                if (i < _syncedItems.Count && ReferenceEquals(_syncedItems[i], item) && !_refreshItemTexts)
                    continue;
                if (ItemTemplate != null && item != null)
                {
                    button.Content = ItemTemplate(item);
                }
                else
                {
                    if (button.Content is not TextBlock)
                        button.Content = null;
                    button.Text = item?.ToString() ?? "";
                }
                if (i < _syncedItems.Count)
                    _syncedItems[i] = item;
                else
                    _syncedItems.Add(item);
            }
            _refreshItemTexts = false;

            UpdateItemButtonVisuals();
        }

        private Button CreateItemButton(int index)
        {
            var button = new Button
            {
                Height = ItemHeight,
                Tag = index,
                TextAlignment = TextAlignment.Left,
                Padding = ItemPadding,
                ShowFocusVisual = false,
                AnimatePressedState = false,
                Shadow = null,
                Margin = new Thickness(0),
                UseThemeStyle = false
            };
            button.MouseDown += ItemButtonMouseDown;
            button.MouseEnter += ItemButtonMouseEnter;
            button.MouseUp += ItemButtonMouseUp;
            button.Click += ItemButtonClick;
            return button;
        }

        private void ItemButtonClick(object? sender, EventArgs e)
        {
            if (sender is not Button { Tag: int index })
                return;

            // Touch/keyboard activation in a multi mode toggles instead of replacing the selection.
            // The pointer gesture already applied the selection on mouse down.
            if (_pointerToggled)
            {
                _pointerToggled = false;
                // A plain click in Extended mode still invokes the item; Ctrl/Shift clicks only select.
                if (SelectionMode == SelectionMode.Extended && _selectedIndices.Count == 1 && SelectedIndex == index)
                    ItemInvoked?.Invoke(this, new ListBoxItemInvokedEventArgs(index, SelectedItem));
                return;
            }

            if (SelectionMode == SelectionMode.Multiple)
            {
                if (!_isMouseSelecting)
                    SelectWithGesture(index, toggle: true, range: false);
                return;
            }

            SelectItem(index, true);
            InvokeItem(index);
        }

        private void ItemButtonMouseDown(object? sender, MouseEventArgs args)
        {
            if (!args.Buttons.Contains(MouseButton.Left) || sender is not Button { Tag: int index })
                return;

            if (SelectionMode != SelectionMode.Single)
            {
                _pointerToggled = true;
                var modifiers = CurrentModifiers();
                SelectWithGesture(index, (modifiers & KeyboardModifiers.Control) != 0, (modifiers & KeyboardModifiers.Shift) != 0);
                Focus();
                args.Handled = true;
                return;
            }

            BeginMouseSelection(index);
            args.Handled = true;
        }

        private void ItemButtonMouseEnter(object? sender, MouseEventArgs args)
        {
            if (!args.Buttons.Contains(MouseButton.Left))
            {
                if (_isMouseSelecting)
                    EndMouseSelection(args.Position, false);
                return;
            }

            if (!_isMouseSelecting || sender is not Button { Tag: int index })
                return;

            SelectItem(index, false);
        }

        private void ItemButtonMouseUp(object? sender, MouseEventArgs args)
        {
            if (!_isMouseSelecting || !args.Buttons.Contains(MouseButton.Left))
                return;

            EndMouseSelection(args.Position, false);
        }

        private void ListBoxMouseMove(object? sender, MouseEventArgs args)
        {
            if (!_isMouseSelecting)
                return;

            if (!args.Buttons.Contains(MouseButton.Left))
            {
                EndMouseSelection(args.Position, false);
                args.Handled = true;
                return;
            }

            SynchronizeItemHover(args.Position, args.Buttons);
            if (TryGetItemIndexAt(args.Position, out var index))
                SelectItem(index, false);
            args.Handled = true;
        }

        private void ListBoxMouseUp(object? sender, MouseEventArgs args)
        {
            if (!_isMouseSelecting || !args.Buttons.Contains(MouseButton.Left))
                return;

            EndMouseSelection(args.Position, true);
            args.Handled = true;
        }

        private void BeginMouseSelection(int index)
        {
            _isMouseSelecting = true;
            _mouseSelectionStartIndex = index;
            SelectItem(index, true);
            Focus();
            Screen?.CaptureMouse(this);
        }

        private void EndMouseSelection(PointF position, bool invokeStartedItem)
        {
            var startIndex = _mouseSelectionStartIndex;
            var releaseIndex = TryGetItemIndexAt(position, out var index) ? index : -1;

            _isMouseSelecting = false;
            _mouseSelectionStartIndex = -1;
            Screen?.ReleaseMouse(this);
            ResetItemInputs(position);

            if (invokeStartedItem && releaseIndex == startIndex && releaseIndex >= 0)
                InvokeItem(releaseIndex);
        }

        private void SelectItem(int index, bool bringIntoView)
        {
            if (index < 0 || index >= Items.Count)
                return;

            SelectedIndex = index;
            if (bringIntoView && index < _itemButtons.Count)
                _scrollViewer.BringIntoView(_itemButtons[index]);
        }

        private void InvokeItem(int index)
        {
            SelectItem(index, true);
            if (SelectedIndex == index)
                ItemInvoked?.Invoke(this, new ListBoxItemInvokedEventArgs(index, SelectedItem));
        }

        private bool TryGetItemIndexAt(PointF position, out int index)
        {
            EnsureItemButtons();
            for (var i = 0; i < _itemButtons.Count; i++)
            {
                if (!_itemButtons[i].ClippingRect.Contains(position))
                    continue;

                index = i;
                return true;
            }

            index = -1;
            return false;
        }

        private void ResetItemInputs(PointF hoverPosition)
        {
            foreach (var button in _itemButtons)
                button.ResetInputs();
            SynchronizeItemHover(hoverPosition, new List<MouseButton>());
        }

        private void SynchronizeItemHover(PointF position, List<MouseButton> buttons)
        {
            var args = new MouseEventArgs(position, buttons);
            foreach (var button in _itemButtons)
            {
                var containsPosition = button.ClippingRect.Contains(position);
                if (containsPosition && !button.IsMouseHovering)
                    button.OnMouseEnter(args);
                else if (!containsPosition && button.IsMouseHovering)
                    button.OnMouseLeave(args);
            }
        }

        private bool _pointerToggled;

        private void ListBoxKeyPressed(object? sender, KeyEventArgs args)
        {
            if (args.InputType == InputType.Char && args.Char == ' ' && SelectionMode != SelectionMode.Single)
            {
                // The anchor is the current item even after it was toggled off.
                var current = _rangeAnchor >= 0 && _rangeAnchor < Items.Count ? _rangeAnchor : SelectedIndex;
                if (current >= 0)
                    SelectWithGesture(current, toggle: true, range: false);
                return;
            }

            if (args.InputType != InputType.Command || Items.Count == 0)
                return;

            switch (args.Command)
            {
                case KeyboardCommand.CursorUp:
                    SelectedIndex = SelectedIndex < 0 ? 0 : Math.Max(0, SelectedIndex - 1);
                    ScrollSelectedIntoView();
                    break;
                case KeyboardCommand.CursorDown:
                    SelectedIndex = SelectedIndex < 0 ? 0 : Math.Min(Items.Count - 1, SelectedIndex + 1);
                    ScrollSelectedIntoView();
                    break;
                case KeyboardCommand.PageUp:
                    SelectedIndex = Math.Max(0, (SelectedIndex < 0 ? 0 : SelectedIndex) - PageSize);
                    ScrollSelectedIntoView();
                    break;
                case KeyboardCommand.PageDown:
                    SelectedIndex = Math.Min(Items.Count - 1, (SelectedIndex < 0 ? 0 : SelectedIndex) + PageSize);
                    ScrollSelectedIntoView();
                    break;
                case KeyboardCommand.Home:
                    SelectedIndex = 0;
                    ScrollSelectedIntoView();
                    break;
                case KeyboardCommand.End:
                    SelectedIndex = Items.Count - 1;
                    ScrollSelectedIntoView();
                    break;
                case KeyboardCommand.Enter:
                    if (SelectedIndex >= 0)
                        InvokeItem(SelectedIndex);
                    break;
            }
        }

        // Items per visible page (one item of overlap), for PageUp/PageDown.
        private int PageSize => Math.Max(1, (int)(_scrollViewer.BoundingRect.Height / Math.Max(1, ItemHeight)) - 1);

        private static readonly Brush TransparentHoverBrush = new SolidColorBrush(Color.Transparent);

        private void UpdateItemButtonVisuals()
        {
            var theme = ResolveTheme();
            for (var i = 0; i < _itemButtons.Count; i++)
            {
                var selected = _selectedIndices.Contains(i);
                var button = _itemButtons[i];
                var backgroundBrush = selected ? SelectedItemBackgroundBrush : ItemBackgroundBrush;
                var textColor = selected ? SelectedItemTextColor : ItemTextColor;
                if (!ReferenceEquals(button.BackgroundBrush, backgroundBrush))
                    button.BackgroundBrush = backgroundBrush;
                if (button.TextColor != textColor)
                    button.TextColor = textColor;
                // Hovering the selected item must not wash out its selected look — the hover
                // overlay would make it read as unselected.
                var hoverBrush = selected ? TransparentHoverBrush : theme.ButtonHoverBrush;
                if (!ReferenceEquals(button.HoverColor, hoverBrush))
                    button.HoverColor = hoverBrush;
                var hoverText = selected ? SelectedItemTextColor : (Color?)null;
                if (!Nullable.Equals(button.HoverTextColor, hoverText))
                    button.HoverTextColor = hoverText;
            }
        }

        private int ClampIndex(int value)
        {
            return HelperEx.ClampSelectionIndex(value, Items.Count);
        }
    }
}
