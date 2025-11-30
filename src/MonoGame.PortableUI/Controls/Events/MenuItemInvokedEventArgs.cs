using System;

namespace MonoGame.PortableUI.Controls.Events
{
    public class MenuItemInvokedEventArgs : EventArgs
    {
        public MenuItemInvokedEventArgs(MenuItem item)
        {
            Item = item;
        }

        public MenuItem Item { get; }
    }
}
