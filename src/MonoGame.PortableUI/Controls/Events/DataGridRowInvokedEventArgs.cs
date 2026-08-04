using System;

namespace MonoGame.PortableUI.Controls.Events
{
    public class DataGridRowInvokedEventArgs : EventArgs
    {
        public DataGridRowInvokedEventArgs(int index, object? item)
        {
            Index = index;
            Item = item;
        }

        public int Index { get; }
        public object? Item { get; }
    }
}
