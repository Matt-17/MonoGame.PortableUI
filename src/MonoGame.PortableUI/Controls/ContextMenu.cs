using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class ContextMenu
    {
        public MenuItemList Items { get; }
        public Brush BackgroundBrush { get; set; }

        public event EventHandler Opening;
        public event EventHandler Opened;
        public event EventHandler Closing;
        public event EventHandler Closed;
        public event EventHandler<MenuItemInvokedEventArgs> ItemInvoked;

        public ContextMenu()
        {
            Items = new MenuItemList();
            BackgroundBrush = Color.Silver;
        }

        public ContextMenuTypes ContextMenuType { get; set; }

        internal Control CreateControl(Screen screen, bool optimizeForTouch)
        {
            var stackPanel = new StackPanel()
            {
                BackgroundBrush = BackgroundBrush
            };
            if (optimizeForTouch)
                stackPanel.Orientation = Orientation.Horizontal;
            foreach (var item in Items)
            {
                var button = new Button
                {
                    Text = item.Text,
                    Height = optimizeForTouch ? 40 : 28,
                };
                if (!optimizeForTouch)
                    button.TextAlignment = TextAlignment.Left;
                button.MouseUp += (sender, args) => { screen.ClearFlyOut(); args.Handled = true; };
                button.TouchUp += (sender, args) => { screen.ClearFlyOut(); args.Handled = true; };
                if (ContextMenuType == ContextMenuTypes.OpenAndClick)
                    button.Click += (s, e) => InvokeMenuItem(item);
                else
                {
                    button.HandleTouchDownEnter = true;
                    button.MouseUp += (sender, args) => InvokeMenuItem(item);
                    button.TouchUp += (sender, args) => InvokeMenuItem(item);
                }
                stackPanel.AddChild(button);
            }
            return stackPanel;
        }

        internal void OnOpening()
        {
            Opening?.Invoke(this, EventArgs.Empty);
        }

        internal void OnOpened()
        {
            Opened?.Invoke(this, EventArgs.Empty);
        }

        internal void OnClosing()
        {
            Closing?.Invoke(this, EventArgs.Empty);
        }

        internal void OnClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void InvokeMenuItem(MenuItem item)
        {
            item.Action?.Invoke();
            ItemInvoked?.Invoke(this, new MenuItemInvokedEventArgs(item));
        }
    }
}
