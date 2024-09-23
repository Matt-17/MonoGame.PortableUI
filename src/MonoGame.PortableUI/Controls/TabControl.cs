using System.Collections.Generic;

namespace MonoGame.PortableUI.Controls
{
    public class TabControl : Control
    {
        private int _selectedIndex;

        public TabControl()
        {
            Items = new List<TabItem>();
        }

        public List<TabItem> Items { get; }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = value;
                InvalidateLayout(false);
            }
        }
    }
}