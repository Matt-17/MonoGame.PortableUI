using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
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
        private int _selectedIndex = -1;
        private Brush _selectedItemBackgroundBrush = new SolidColorBrush(new Color(20, 126, 133));
        private Color _selectedItemTextColor;

        public ListBox()
        {
            Items = new List<object>();
            _itemButtons = new List<Button>();
            _itemsPanel = new StackPanel { Orientation = Orientation.Vertical };
            _scrollViewer = new ScrollViewer
            {
                Parent = this,
                Content = _itemsPanel,
                ScrollOrientation = Orientation.Vertical
            };

            BackgroundBrush = Color.White;
            ItemHeight = 28;
            ItemTextColor = Color.Black;
            SelectedItemTextColor = Color.White;
            ShowFocusVisual = true;
            KeyPressed += ListBoxKeyPressed;
        }

        public List<object> Items { get; }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                var clamped = ClampIndex(value);
                if (_selectedIndex == clamped)
                    return;

                _selectedIndex = clamped;
                UpdateItemButtonVisuals();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public object? SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

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

        public event EventHandler? SelectionChanged;
        public event EventHandler<ListBoxItemInvokedEventArgs>? ItemInvoked;

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            EnsureItemButtons();
            var contentSize = _itemsPanel.MeasureLayout();
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
            _scrollViewer.UpdateLayout(BoundingRect - Margin);
        }

        public override IEnumerable<Control> GetDescendants()
        {
            EnsureItemButtons();
            yield return _scrollViewer;
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

            var clamped = ClampIndex(_selectedIndex);
            if (_selectedIndex != clamped)
            {
                _selectedIndex = clamped;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }

            for (var i = 0; i < _itemButtons.Count; i++)
            {
                var button = _itemButtons[i];
                button.Tag = i;
                button.Height = ItemHeight;
                button.Text = Items[i]?.ToString() ?? "";
            }

            UpdateItemButtonVisuals();
        }

        private Button CreateItemButton(int index)
        {
            var button = new Button
            {
                Height = ItemHeight,
                Tag = index,
                TextAlignment = TextAlignment.Left,
                Padding = new Thickness(8, 0)
            };
            button.Click += ItemButtonClick;
            return button;
        }

        private void ItemButtonClick(object? sender, EventArgs e)
        {
            if (sender is not Button { Tag: int index })
                return;

            SelectedIndex = index;
            ItemInvoked?.Invoke(this, new ListBoxItemInvokedEventArgs(index, SelectedItem));
        }

        private void ListBoxKeyPressed(object? sender, KeyEventArgs args)
        {
            if (args.InputType != InputType.Command || Items.Count == 0)
                return;

            switch (args.Command)
            {
                case KeyboardCommand.CursorUp:
                    SelectedIndex = SelectedIndex < 0 ? 0 : Math.Max(0, SelectedIndex - 1);
                    break;
                case KeyboardCommand.CursorDown:
                    SelectedIndex = SelectedIndex < 0 ? 0 : Math.Min(Items.Count - 1, SelectedIndex + 1);
                    break;
                case KeyboardCommand.Enter:
                    if (SelectedIndex >= 0)
                        ItemInvoked?.Invoke(this, new ListBoxItemInvokedEventArgs(SelectedIndex, SelectedItem));
                    break;
            }
        }

        private void UpdateItemButtonVisuals()
        {
            for (var i = 0; i < _itemButtons.Count; i++)
            {
                var selected = i == SelectedIndex;
                var button = _itemButtons[i];
                var backgroundBrush = selected ? SelectedItemBackgroundBrush : ItemBackgroundBrush;
                var textColor = selected ? SelectedItemTextColor : ItemTextColor;
                if (!ReferenceEquals(button.BackgroundBrush, backgroundBrush))
                    button.BackgroundBrush = backgroundBrush;
                if (button.TextColor != textColor)
                    button.TextColor = textColor;
            }
        }

        private int ClampIndex(int value)
        {
            if (Items.Count == 0 || value < 0)
                return -1;

            return Math.Max(0, Math.Min(value, Items.Count - 1));
        }
    }
}
