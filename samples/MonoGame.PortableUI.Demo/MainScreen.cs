using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Demo
{
    public sealed class MainScreen : Screen
    {
        private readonly Texture2D _deleteIcon;
        private readonly TextBlock _status;

        public MainScreen(Texture2D deleteIcon)
        {
            _deleteIcon = deleteIcon;
            BackgroundBrush = new Color(29, 31, 36);
            _status = Label("Ready");
            Content = CreateLayout();
        }

        private Control CreateLayout()
        {
            var root = new Grid
            {
                Margin = 18,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(320) },
                    new ColumnDefinition()
                }
            };

            var side = CreateSidePanel();
            Grid.SetColumn(side, 0);
            root.AddChild(side);

            var tabs = CreateTabs();
            Grid.SetColumn(tabs, 1);
            root.AddChild(tabs);

            return root;
        }

        private Control CreateSidePanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                BackgroundBrush = new Color(236, 238, 241),
                Margin = new Thickness(0, 0, 14, 0)
            };

            var title = Label("PortableUI Demo");
            title.TextColor = new Color(35, 38, 43);
            title.TextSize = 18;
            title.Margin = new Thickness(12, 12, 12, 4);
            panel.AddChild(title);

            var next = new TextButton("Open second screen") { Margin = new Thickness(0, 12), Height = 38 };
            next.Click += (sender, args) => ScreenEngine.NavigateToScreen(new SecondScreen());
            panel.AddChild(next);

            var combo = new ComboBox { Margin = new Thickness(0, 4), Height = 36 };
            combo.Items.Add("Compact density");
            combo.Items.Add("Comfortable density");
            combo.Items.Add("Large touch density");
            combo.SelectedIndex = 1;
            combo.SelectionChanged += (sender, args) => _status.Text = $"Combo: {combo.SelectedItem}";
            panel.AddChild(combo);

            var toggle = new ToggleButton { Text = "Toggle state", Margin = new Thickness(0, 4), Height = 36 };
            toggle.Checked += (sender, args) => _status.Text = args.IsChecked ? "Toggle checked" : "Toggle unchecked";
            panel.AddChild(toggle);

            var radioA = new RadioButton { Text = "Radio option A", RadioGroup = "demo", Margin = new Thickness(0, 4), Height = 32 };
            var radioB = new RadioButton { Text = "Radio option B", RadioGroup = "demo", Margin = new Thickness(0, 4), Height = 32 };
            panel.AddChild(radioA);
            panel.AddChild(radioB);

            var imageButton = new ImageButton { Source = _deleteIcon, Width = 44, Height = 44, Margin = new Thickness(0, 8) };
            imageButton.Click += (sender, args) => _status.Text = "ImageButton clicked";
            panel.AddChild(imageButton);

            var menuButton = new TextButton("Open context menu") { Margin = new Thickness(0, 8), Height = 36 };
            var menu = new ContextMenu();
            menu.Items.Add(new MenuItem("First command", () => _status.Text = "First command"));
            menu.Items.Add(new MenuItem("Second command", () => _status.Text = "Second command"));
            menu.ItemInvoked += (sender, args) => _status.Text = $"Invoked: {args.Item.Text}";
            menuButton.ContextMenu = menu;
            panel.AddChild(menuButton);

            panel.AddChild(_status);
            return panel;
        }

        private Control CreateTabs()
        {
            var tabs = new TabControl
            {
                Margin = new Thickness(18, 0, 0, 0),
                BackgroundBrush = new Color(248, 249, 250)
            };

            tabs.Items.Add(new TabItem { Header = "Controls", Content = CreateControlsTab() });
            tabs.Items.Add(new TabItem { Header = "Scroll", Content = CreateScrollTab() });
            tabs.Items.Add(new TabItem { Header = "Layout", Content = CreateLayoutTab() });
            tabs.SelectedIndex = 0;
            return tabs;
        }

        private Control CreateControlsTab()
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = 18 };
            panel.AddChild(Label("Text input"));
            panel.AddChild(new TextBox { HintText = "Type here", Margin = new Thickness(0, 8), Height = 36, MinWidth = 260 });
            panel.AddChild(Label("Button states"));
            panel.AddChild(new Button
            {
                Text = "Hover / press me",
                Height = 38,
                Margin = new Thickness(0, 8),
                HoverTextColor = Color.DarkSlateBlue,
                PressedTextColor = Color.White
            });
            return panel;
        }

        private Control CreateScrollTab()
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical };
            for (var i = 1; i <= 30; i++)
            {
                stack.AddChild(new TextButton($"Scrollable row {i}") { Height = 30, Margin = new Thickness(4) });
            }

            return new ScrollViewer
            {
                Content = stack,
                Margin = 18,
                BackgroundBrush = new Color(242, 244, 247),
                ScrollOrientation = Orientation.Vertical
            };
        }

        private Control CreateLayoutTab()
        {
            var grid = new Grid
            {
                Margin = 18,
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition()
                }
            };

            var border = new Border
            {
                BackgroundBrush = Color.White,
                BorderColor = new Color(55, 92, 170),
                BorderWidth = 2,
                Padding = 14,
                Content = Label("Border inside Grid row with constrained content")
            };
            grid.AddChild(border);

            var bottom = Label("Resize the window to exercise layout invalidation.");
            Grid.SetRow(bottom, 1);
            bottom.VerticalAlignment = VerticalAlignment.Bottom;
            grid.AddChild(bottom);
            return grid;
        }

        private static TextBlock Label(string text)
        {
            return new TextBlock
            {
                Text = text,
                TextColor = new Color(62, 67, 75),
                Margin = new Thickness(0, 4)
            };
        }
    }
}
