using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;

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
            Click += ComboBoxClick;
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
                Text = SelectedItem?.ToString() ?? "";
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public object SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

        public event EventHandler SelectionChanged;

        private void ComboBoxClick(object sender, EventArgs e)
        {
            if (Screen == null || Items.Count == 0)
                return;

            var contextMenu = new ContextMenu { ContextMenuType = ContextMenuTypes.OpenAndClick };
            for (var i = 0; i < Items.Count; i++)
            {
                var index = i;
                contextMenu.Items.Add(new MenuItem(Items[i]?.ToString() ?? "", () => SelectedIndex = index));
            }
            Screen.CreateContextMenu(new PointF(BoundingRect.Left, BoundingRect.Bottom + Items.Count * 28), contextMenu, false);
        }

        private int ClampIndex(int value)
        {
            if (Items.Count == 0)
                return -1;
            return Math.Max(0, Math.Min(value, Items.Count - 1));
        }
    }
}
