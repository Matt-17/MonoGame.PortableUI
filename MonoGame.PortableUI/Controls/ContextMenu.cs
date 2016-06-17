using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class ContextMenu
    {
        public MenuItemList Items { get; }
        public Brush BackgroundBrush { get; set; }

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
            float maxWidth = 0;
            foreach (var item in Items)
            {
                var button = new Button
                {
                    Text = item.Text,
                    Height = optimizeForTouch ? 40 : 28,
                };
                button.MouseUp += (sender, args) => { screen.ClearFlyOut(); args.Handled = true; };
                button.TouchUp += (sender, args) => { screen.ClearFlyOut(); args.Handled = true; };
                if (ContextMenuType == ContextMenuTypes.OpenAndClick)
                    button.Click += (s, e) => item.Action();
                else
                {
                    button.HandleTouchDownEnter = true;
                    button.MouseUp += (sender, args) => item.Action();
                    button.TouchUp += (sender, args) => item.Action();
                }
                var width = button.MeasureLayout().Width;
                if (width > maxWidth)
                    maxWidth = width;
                stackPanel.AddChild(button);
            }
            stackPanel.Width = maxWidth;
            return stackPanel;
        }
    }
}