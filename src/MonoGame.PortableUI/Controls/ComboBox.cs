using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class ComboBox : Button
    {
        private int _selectedIndex = -1;

        public ComboBox()
        {
            Items = new List<object>();
            Height = 32;
            BackgroundBrush = Color.White;
            TextAlignment = TextAlignment.Left;
            DropDownMaxHeight = 160;
            ItemHeight = 28;
            DropDownBackgroundBrush = Color.White;
            ItemBackgroundBrush = Color.White;
            SelectedItemBackgroundBrush = new Color(20, 126, 133);
            ItemTextColor = Color.Black;
            SelectedItemTextColor = Color.White;
            Click += ComboBoxClick;
        }

        public List<object> Items { get; }
        public float DropDownMaxHeight { get; set; }
        public float ItemHeight { get; set; }
        public Brush DropDownBackgroundBrush { get; set; }
        public Brush ItemBackgroundBrush { get; set; }
        public Brush SelectedItemBackgroundBrush { get; set; }
        public Color ItemTextColor { get; set; }
        public Color SelectedItemTextColor { get; set; }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                var clamped = ClampIndex(value);
                if (_selectedIndex == clamped)
                    return;
                _selectedIndex = clamped;
                Text = SelectedItem?.ToString() ?? "";
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public object? SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

        public event EventHandler? SelectionChanged;

        private void ComboBoxClick(object? sender, EventArgs e)
        {
            if (Screen == null || Items.Count == 0)
                return;

            var listBox = CreateDropDownListBox();
            listBox.ItemInvoked += DropDownItemInvoked;

            var targetHeight = Math.Max(0, Math.Min(DropDownMaxHeight, Items.Count * ItemHeight));
            listBox.Width = (BoundingRect - Margin).Width;
            listBox.Height = targetHeight;

            var bounds = BoundingRect - Margin;
            Screen.ShowFlyOut(new PointF(bounds.Left, bounds.Bottom + targetHeight), listBox, false);
        }

        internal ListBox CreateDropDownListBox()
        {
            var listBox = new ListBox
            {
                BackgroundBrush = DropDownBackgroundBrush,
                ItemHeight = ItemHeight,
                ItemBackgroundBrush = ItemBackgroundBrush,
                SelectedItemBackgroundBrush = SelectedItemBackgroundBrush,
                ItemTextColor = ItemTextColor,
                SelectedItemTextColor = SelectedItemTextColor,
                SelectedIndex = ClampIndex(SelectedIndex)
            };

            foreach (var item in Items)
                listBox.Items.Add(item);

            listBox.SelectedIndex = ClampIndex(SelectedIndex);
            return listBox;
        }

        private void DropDownItemInvoked(object? sender, ListBoxItemInvokedEventArgs args)
        {
            SelectedIndex = args.Index;
            Screen?.ClearFlyOut();
        }

        private int ClampIndex(int value)
        {
            if (Items.Count == 0 || value < 0)
                return -1;
            return Math.Max(0, Math.Min(value, Items.Count - 1));
        }
    }
}
