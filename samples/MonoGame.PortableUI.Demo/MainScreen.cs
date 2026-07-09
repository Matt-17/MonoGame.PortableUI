using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Demo
{
    public sealed class MainScreen : Screen
    {
        private static readonly Color Ink = new Color(28, 31, 35);
        private static readonly Color Paper = new Color(244, 246, 248);
        private static readonly Color Panel = new Color(232, 237, 240);
        private static readonly Color Line = new Color(82, 101, 111);
        private static readonly Color Teal = new Color(20, 126, 133);
        private static readonly Color Amber = new Color(213, 151, 54);
        private static readonly Color Red = new Color(174, 68, 62);

        private readonly Texture2D _deleteIcon;
        private readonly TextBlock _status;

        public MainScreen(Texture2D deleteIcon)
        {
            _deleteIcon = deleteIcon;
            BackgroundBrush = Ink;
            _status = Label("Ready", new Color(207, 214, 219));
            Content = CreateLayout();
        }

        private Control CreateLayout()
        {
            var root = new Grid
            {
                Margin = 16,
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition(),
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            root.AddChild(CreateHeader());
            root.AddChild(CreateTabs(), row: 1);
            root.AddChild(CreateStatusStrip(), row: 2);
            return root;
        }

        private Control CreateHeader()
        {
            var header = new Grid
            {
                Margin = new Thickness(0, 0, 0, 12),
                ColumnDefinitions =
                {
                    new ColumnDefinition(),
                    new ColumnDefinition { Width = new GridLength(210) }
                }
            };

            var titleStack = new StackPanel
            {
                Orientation = Orientation.Vertical
            };
            titleStack.AddChild(Label("MonoGame.PortableUI", Paper, 22, new Thickness(0, 0, 0, 2)));
            titleStack.AddChild(Label("Code-first controls for DesktopGL screens", new Color(172, 185, 192)));
            header.AddChild(titleStack);

            var next = CommandButton("Open second screen", Teal, Color.White);
            next.Height = 42;
            next.ToolTip = "Navigate to the secondary demo screen";
            next.Click += (sender, args) => ScreenEngine?.NavigateToScreen(new SecondScreen());
            header.AddChild(next, column: 1);

            return header;
        }

        private Control CreateTabs()
        {
            var tabs = new TabControl
            {
                BackgroundBrush = Paper,
                HeaderHeight = 38,
                HeaderBackground = new Color(202, 211, 216),
                SelectedHeaderBackground = Paper
            };

            tabs.Items.Add(new TabItem { Header = "Controls", Content = CreateControlsTab() });
            tabs.Items.Add(new TabItem { Header = "Layout", Content = CreateLayoutTab() });
            tabs.Items.Add(new TabItem { Header = "Scroll", Content = CreateScrollTab() });
            tabs.Items.Add(new TabItem { Header = "Stress", Content = CreateStressTab() });
            tabs.SelectedIndex = 0;
            return tabs;
        }

        private Control CreateControlsTab()
        {
            var grid = new Grid
            {
                Margin = 16,
                ColumnDefinitions =
                {
                    new ColumnDefinition(),
                    new ColumnDefinition(),
                    new ColumnDefinition()
                }
            };

            grid.AddChild(CreateInputPanel());
            grid.AddChild(CreateListPanel(), column: 1);
            grid.AddChild(CreateActionPanel(), column: 2);
            return grid;
        }

        private Control CreateInputPanel()
        {
            var panel = PanelStack("Input and selection");

            panel.AddChild(Label("TextBox", Line));
            var textBox = new TextBox
            {
                HintText = "Type a screen note",
                Margin = new Thickness(0, 6, 12, 14),
                Height = 38,
                ToolTip = "Click or tap to focus text input"
            };
            textBox.TextChanged += (sender, args) => _status.Text = $"Text: {args.NewText}";
            panel.AddChild(textBox);

            panel.AddChild(Label("ComboBox", Line));
            var combo = new ComboBox { Margin = new Thickness(0, 6, 12, 14), Height = 38, ToolTip = "Choose a density preset" };
            combo.Items.Add("Compact density");
            combo.Items.Add("Comfortable density");
            combo.Items.Add("Touch density");
            combo.SelectedIndex = 1;
            combo.SelectionChanged += (sender, args) => _status.Text = $"Density: {combo.SelectedItem}";
            panel.AddChild(combo);

            var checkBox = new CheckBox
            {
                Text = "Enable layout guides",
                Margin = new Thickness(0, 0, 12, 10),
                Height = 34,
                TextColor = Ink,
                BoxBorderBrush = Line,
                CheckMarkBrush = Teal,
                ToolTip = "Toggle a checkbox state"
            };
            checkBox.Checked += (sender, args) => _status.Text = args.IsChecked ? "Layout guides on" : "Layout guides off";
            panel.AddChild(checkBox);

            var toggle = new ToggleButton
            {
                Text = "Toggle preview mode",
                Margin = new Thickness(0, 0, 12, 10),
                Height = 38,
                BackgroundBrush = new Color(218, 225, 229),
                TextColor = Ink,
                ToggleBrush = Teal,
                ToolTip = "Toggle the preview mode state"
            };
            toggle.Checked += (sender, args) => _status.Text = args.IsChecked ? "Preview mode on" : "Preview mode off";
            panel.AddChild(toggle);

            var radioA = new RadioButton { Text = "Mouse first", RadioGroup = "input", Margin = new Thickness(0, 0, 12, 4), Height = 32 };
            var radioB = new RadioButton { Text = "Touch first", RadioGroup = "input", Margin = new Thickness(0, 0, 12, 0), Height = 32 };
            radioA.Checked += (sender, args) => _status.Text = "Input profile: mouse";
            radioB.Checked += (sender, args) => _status.Text = "Input profile: touch";
            panel.AddChild(radioA);
            panel.AddChild(radioB);

            return panel;
        }

        private Control CreateListPanel()
        {
            var panel = PanelStack("ListBox");

            panel.AddChild(Label("Scrollable items", Line));
            var listBox = new ListBox
            {
                Height = 190,
                Margin = new Thickness(0, 6, 12, 10),
                ItemHeight = 30,
                ItemBackgroundBrush = Color.White,
                SelectedItemBackgroundBrush = Teal,
                ItemTextColor = Ink,
                SelectedItemTextColor = Color.White,
                ToolTip = "Scroll and select a list item"
            };

            for (var i = 1; i <= 24; i++)
                listBox.Items.Add($"Inventory slot {i:00}");

            listBox.SelectedIndex = 3;
            listBox.SelectionChanged += (sender, args) => _status.Text = $"ListBox: {listBox.SelectedItem}";
            listBox.ItemInvoked += (sender, args) => _status.Text = $"ListBox invoked: {args.Item}";
            panel.AddChild(listBox);

            return panel;
        }

        private Control CreateActionPanel()
        {
            var panel = PanelStack("Buttons and menus");

            var primary = CommandButton("Primary action", Teal, Color.White);
            primary.ToolTip = "Run the primary demo action";
            primary.Click += (sender, args) => _status.Text = "Primary action clicked";
            panel.AddChild(primary);

            var secondary = CommandButton("Secondary action", Amber, Ink);
            secondary.ToolTip = "Run the secondary demo action";
            secondary.Click += (sender, args) => _status.Text = "Secondary action clicked";
            panel.AddChild(secondary);

            var danger = CommandButton("Danger action", Red, Color.White);
            danger.ToolTip = "Run the destructive demo action";
            danger.Click += (sender, args) => _status.Text = "Danger action clicked";
            panel.AddChild(danger);

            var disabled = CommandButton("Disabled action", new Color(184, 193, 199), new Color(82, 101, 111));
            disabled.IsEnabled = false;
            panel.AddChild(disabled);

            panel.AddChild(Label("ImageButton", Line, 14, new Thickness(0, 8, 0, 2)));
            var imageButton = new ImageButton
            {
                Source = _deleteIcon,
                Width = 46,
                Height = 46,
                Margin = new Thickness(0, 2, 0, 12),
                BackgroundBrush = new Color(224, 229, 232),
                ToolTip = "Delete the current demo item"
            };
            imageButton.Click += (sender, args) => _status.Text = "ImageButton clicked";
            panel.AddChild(imageButton);

            var menuButton = CommandButton("Open context menu", new Color(62, 80, 91), Color.White);
            menuButton.ToolTip = "Right-click or long-press to open commands";
            var menu = new ContextMenu { BackgroundBrush = new Color(232, 237, 240) };
            menu.Items.Add(new MenuItem("Inspect", () => _status.Text = "Inspect command"));
            menu.Items.Add(new MenuItem("Duplicate", () => _status.Text = "Duplicate command"));
            menu.Items.Add(new MenuItem("Archive", () => _status.Text = "Archive command"));
            menu.ItemInvoked += (sender, args) => _status.Text = $"Menu: {args.Item.Text}";
            menuButton.ContextMenu = menu;
            panel.AddChild(menuButton);

            return panel;
        }

        private Control CreateLayoutTab()
        {
            var grid = new Grid
            {
                Margin = 16,
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition(),
                    new RowDefinition { Height = new GridLength(46) }
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition(),
                    new ColumnDefinition { Width = new GridLength(260) }
                }
            };

            var auto = InfoTile("Auto", "Measures content", Teal);
            var star = InfoTile("Star", "Uses remaining space", Amber);
            var fixedSize = InfoTile("Fixed", "260 px", Red, new Thickness(0));
            grid.AddChild(auto);
            grid.AddChild(star, column: 1);
            grid.AddChild(fixedSize, column: 2);

            var preview = new Border
            {
                Margin = new Thickness(0, 14, 0, 14),
                BackgroundBrush = Color.White,
                BorderColor = Line,
                BorderWidth = 2,
                Padding = 16,
                Content = Label("Resize the window. The header, tiles and status strip keep their roles.", Ink, 16)
            };
            grid.AddChild(preview, row: 1, columnSpan: 3);

            var bottom = Label("Grid columns: auto, star, fixed", Line);
            bottom.VerticalAlignment = VerticalAlignment.Center;
            grid.AddChild(bottom, row: 2, columnSpan: 3);
            return grid;
        }

        private Control CreateScrollTab()
        {
            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = 12
            };

            for (var i = 1; i <= 40; i++)
            {
                var row = CommandButton($"Scrollable row {i:00}", i % 3 == 0 ? Amber : new Color(224, 230, 233), Ink);
                row.Height = 30;
                row.Margin = new Thickness(0, 0, 8, 5);
                var rowNumber = i;
                row.Click += (sender, args) => _status.Text = $"Row {rowNumber:00} clicked";
                stack.AddChild(row);
            }

            return new ScrollViewer
            {
                Content = stack,
                Margin = 16,
                BackgroundBrush = new Color(236, 240, 242),
                ScrollOrientation = Orientation.Vertical
            };
        }

        private Control CreateStressTab()
        {
            var root = new Grid
            {
                Margin = 16,
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition()
                }
            };

            root.AddChild(Label("500 controls in a 20 x 25 grid", Ink, 16, new Thickness(0, 0, 0, 10)));

            var grid = new Grid
            {
                BackgroundBrush = new Color(238, 242, 244)
            };

            for (var row = 0; row < 25; row++)
                grid.RowDefinitions.Add(new RowDefinition());

            for (var column = 0; column < 20; column++)
                grid.ColumnDefinitions.Add(new ColumnDefinition());

            for (var row = 0; row < 25; row++)
            {
                for (var column = 0; column < 20; column++)
                {
                    var index = row * 20 + column + 1;
                    var cell = new Border
                    {
                        Margin = new Thickness(1),
                        BackgroundBrush = index % 7 == 0 ? new Color(217, 228, 230) : Color.White,
                        BorderColor = index % 5 == 0 ? Teal : new Color(202, 211, 216),
                        BorderWidth = 1,
                        Padding = 2,
                        Content = Label(index.ToString("000"), Line, 10)
                    };
                    grid.AddChild(cell, row, column);
                }
            }

            root.AddChild(grid, row: 1);
            return root;
        }

        private Control CreateStatusStrip()
        {
            var strip = new Grid
            {
                Margin = new Thickness(0, 12, 0, 0),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(130) },
                    new ColumnDefinition()
                }
            };

            strip.AddChild(Label("Status", new Color(143, 160, 168)));
            _status.Margin = new Thickness(0);
            strip.AddChild(_status, column: 1);
            return strip;
        }

        private static StackPanel PanelStack(string title)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 12, 0),
                BackgroundBrush = Panel
            };
            panel.AddChild(Label(title, Ink, 17, new Thickness(12, 10, 12, 8)));
            return panel;
        }

        private static Border InfoTile(string title, string detail, Color accent)
        {
            return InfoTile(title, detail, accent, new Thickness(0, 0, 12, 0));
        }

        private static Border InfoTile(string title, string detail, Color accent, Thickness margin)
        {
            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = 0
            };
            stack.AddChild(Label(title, accent, 18, new Thickness(0, 0, 0, 2)));
            stack.AddChild(Label(detail, Line));

            return new Border
            {
                Margin = margin,
                BackgroundBrush = Color.White,
                BorderColor = accent,
                BorderWidth = 2,
                Padding = new Thickness(12, 10, 14, 10),
                Content = stack
            };
        }

        private static TextButton CommandButton(string text, Color background, Color foreground)
        {
            return new TextButton(text)
            {
                Height = 38,
                Margin = new Thickness(0, 0, 12, 8),
                BackgroundBrush = background,
                TextColor = foreground,
                HoverTextColor = Color.White,
                PressedTextColor = Color.White
            };
        }

        private static TextBlock Label(string text, Color color)
        {
            return Label(text, color, 14, new Thickness(0, 3));
        }

        private static TextBlock Label(string text, Color color, int size)
        {
            return Label(text, color, size, new Thickness(0, 3));
        }

        private static TextBlock Label(string text, Color color, int size, Thickness margin)
        {
            return new TextBlock
            {
                Text = text,
                TextColor = color,
                TextSize = size,
                Margin = margin
            };
        }
    }
}
