using System;

namespace MonoGame.PortableUI.Controls.Events
{
    public class ListBoxItemInvokedEventArgs : EventArgs
    {
        public ListBoxItemInvokedEventArgs(int index, object? item)
        {
            Index = index;
            Item = item;
        }

        public int Index { get; }
        public object? Item { get; }
    }
}
