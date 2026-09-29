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

        public event EventHandler? Opening;
        public event EventHandler? Opened;
        public event EventHandler? Closing;
        public event EventHandler? Closed;
        public event EventHandler<MenuItemInvokedEventArgs>? ItemInvoked;

        public ContextMenu()
        {
            var theme = PortableTheme.ResolveCurrent();

            Items = new MenuItemList();
            BackgroundBrush = theme.ContextMenuBackgroundBrush;
        }

        public ContextMenuTypes ContextMenuType { get; set; }

        internal Control CreateControl(Screen? screen, bool optimizeForTouch)
        {
            var panel = new MenuPanel(this, screen, optimizeForTouch)
            {
                BackgroundBrush = BackgroundBrush
            };
            if (optimizeForTouch)
                panel.Orientation = Orientation.Horizontal;
            panel.Show(Items, parent: null);
            return panel;
        }

        /// <summary>
        ///     The rows of an open menu. Submenus open in place (drill-down): the panel swaps to the
        ///     child items with a back row on top, which works the same for mouse, touch and gamepad.
        /// </summary>
        private sealed class MenuPanel : StackPanel
        {
            private readonly ContextMenu _menu;
            private readonly Screen? _screen;
            private readonly bool _touch;
            private readonly System.Collections.Generic.Stack<(MenuItemList Items, MenuItem? Parent)> _path = new();

            public MenuPanel(ContextMenu menu, Screen? screen, bool touch)
            {
                _menu = menu;
                _screen = screen;
                _touch = touch;
            }

            internal MenuItem? CurrentParent { get; private set; }

            public void Show(MenuItemList items, MenuItem? parent)
            {
                CurrentParent = parent;
                Children.Clear();
                if (parent != null)
                    AddChild(CreateRow(parent.Text, isBack: true, null));

                foreach (var item in items)
                {
                    if (item.IsSeparator)
                    {
                        AddChild(new Border
                        {
                            Height = 1,
                            Margin = new Thickness(6, 3),
                            BackgroundBrush = new SolidColorBrush(new Color(128, 128, 128, 140)),
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        });
                        continue;
                    }

                    AddChild(CreateRow(item.Text, isBack: false, item));
                }

                // Keyboard/gamepad users keep focus inside the menu while drilling in and out.
                if (ScreenEngine.FocusedControl == null || ScreenEngine.FocusedControl.Parent == this || ScreenEngine.FocusedControl.Parent == null)
                {
                    foreach (var child in Children)
                    {
                        if (child is Button { IsEnabled: true } first && _screen?.ScreenEngine?.KeyboardNavigationActive == true)
                        {
                            first.Focus();
                            break;
                        }
                    }
                }
            }

            private Button CreateRow(string text, bool isBack, MenuItem? item)
            {
                var button = new Button
                {
                    Height = _touch ? 40 : 28,
                    Shadow = null,
                    Margin = new Thickness(0),
                    UseThemeStyle = false,
                    IsEnabled = item?.IsEnabled ?? true,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                var textColor = button.IsEnabled ? button.TextColor : (button.DisabledTextColor ?? button.TextColor);
                button.Content = BuildRowContent(text, isBack, item, textColor);

                if (isBack)
                {
                    button.Click += (_, _) => GoBack();
                    return button;
                }

                if (item!.HasSubmenu)
                {
                    button.Click += (_, _) => Open(item);
                    return button;
                }

                button.MouseUp += (sender, args) => { if (item.IsEnabled) _screen?.ClearFlyOut(); args.Handled = true; };
                button.TouchUp += (sender, args) => { if (item.IsEnabled) _screen?.ClearFlyOut(); args.Handled = true; };
                if (_menu.ContextMenuType != ContextMenuTypes.OpenAndHold)
                {
                    button.Click += (s, e) =>
                    {
                        _menu.InvokeMenuItem(item);
                        _screen?.ClearFlyOut();
                    };
                }
                else
                {
                    button.HandleTouchDownEnter = true;
                    button.MouseUp += (sender, args) => _menu.InvokeMenuItem(item);
                    button.TouchUp += (sender, args) => _menu.InvokeMenuItem(item);
                    // Keyboard/gamepad activation (Enter/A) has no release gesture.
                    button.KeyPressed += (sender, args) =>
                    {
                        if (args.InputType != InputType.Command || args.Command != KeyboardCommand.Enter)
                            return;
                        _menu.InvokeMenuItem(item);
                        _screen?.ClearFlyOut();
                    };
                }
                return button;
            }

            private Control BuildRowContent(string text, bool isBack, MenuItem? item, Color textColor)
            {
                var row = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
                row.ColumnDefinitions.Add(new ColumnDefinition());
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });

                if (isBack)
                    row.AddChild(Glyph(BackGeometry, textColor), column: 0);
                else if (item!.IsCheckable && item.IsChecked)
                    row.AddChild(Glyph(CheckGeometry, textColor), column: 0);
                else if (item.Icon != null)
                    row.AddChild(new Image { Source = item.Icon, Width = 16, Height = 16, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center }, column: 0);

                row.AddChild(new TextBlock { Text = text, TextColor = textColor, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) }, column: 1);
                if (!isBack && !string.IsNullOrEmpty(item!.ShortcutText))
                    row.AddChild(new TextBlock { Text = item.ShortcutText!, TextColor = textColor * 0.7f, VerticalAlignment = VerticalAlignment.Center }, column: 2);
                if (!isBack && item!.HasSubmenu)
                    row.AddChild(Glyph(ChevronGeometry, textColor), column: 3);
                return row;
            }

            private static PathShape Glyph(PathGeometry geometry, Color color) => new PathShape
            {
                Geometry = geometry,
                Fill = null,
                Stroke = color,
                StrokeWidth = 2.2f,
                Width = 12,
                Height = 12,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            private void Open(MenuItem item)
            {
                _path.Push((CurrentItems, CurrentParent));
                Show(item.Items, item);
            }

            private void GoBack()
            {
                if (_path.Count == 0)
                    return;
                var (items, parent) = _path.Pop();
                Show(items, parent);
            }

            private MenuItemList CurrentItems => CurrentParent?.Items ?? _menu.Items;

            private static readonly PathGeometry CheckGeometry = new PathGeometry(12, 12).MoveTo(2, 6).LineTo(5, 9).LineTo(10, 3);
            private static readonly PathGeometry ChevronGeometry = new PathGeometry(12, 12).MoveTo(4, 2).LineTo(8, 6).LineTo(4, 10);
            private static readonly PathGeometry BackGeometry = new PathGeometry(12, 12).MoveTo(8, 2).LineTo(4, 6).LineTo(8, 10);
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

        internal void InvokeMenuItem(MenuItem item)
        {
            if (!item.IsEnabled)
                return;
            if (item.IsCheckable)
                item.IsChecked = !item.IsChecked;
            item.Action?.Invoke();
            ItemInvoked?.Invoke(this, new MenuItemInvokedEventArgs(item));
        }
    }
}
