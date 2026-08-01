using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Demo
{
    public sealed class MainScreen : Screen
    {
        private readonly Action<DemoThemePreset> _applyTheme;
        private readonly Texture2D _deleteIcon;
        private DemoThemePreset _themePreset;
        private TextBlock _status = null!;

        private DemoThemePalette Palette => _themePreset.Palette;
        private Brush ScreenBackgroundBrush => Palette.BackgroundBrush ?? _themePreset.BackgroundColor;
        private Brush SurfaceBrush => Palette.SurfaceBrush ?? Palette.Surface;
        private Brush SurfaceAltBrush => Palette.SurfaceAltBrush ?? Palette.SurfaceAlt;
        private Brush SelectionBrush => Palette.SelectionBrush ?? Palette.Selection;
        private Brush FieldFrameBrush => Palette.FieldFrameBrush ?? Palette.FieldFrame;
        private bool IsGlassTheme => string.Equals(_themePreset.Id, "glass", StringComparison.OrdinalIgnoreCase);

        public MainScreen(Texture2D deleteIcon, DemoThemePreset themePreset, Action<DemoThemePreset> applyTheme)
        {
            _deleteIcon = deleteIcon;
            _themePreset = themePreset ?? DemoThemeRegistry.Default;
            _applyTheme = applyTheme ?? throw new ArgumentNullException(nameof(applyTheme));
            RebuildContent();
        }

        public void ApplyThemePreset(DemoThemePreset themePreset)
        {
            _themePreset = themePreset ?? DemoThemeRegistry.Default;
            RebuildContent();
        }

        private void RebuildContent()
        {
            ScreenEngine.FocusedControl = null;
            BackgroundBrush = ScreenBackgroundBrush;
            _status = Label("READY.", Palette.MutedText);
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
                    new ColumnDefinition { Width = new GridLength(230) },
                    new ColumnDefinition { Width = new GridLength(220) }
                }
            };

            var titleStack = new StackPanel
            {
                Orientation = Orientation.Vertical
            };
            titleStack.AddChild(Label("MonoGame.PortableUI", Palette.HeadingText, 22, new Thickness(0, 0, 0, 2)));
            titleStack.AddChild(Label($"{_themePreset.DisplayName.ToUpperInvariant()} THEME - CODE-FIRST CONTROLS", Palette.MutedText));
            header.AddChild(titleStack);

            header.AddChild(CreateThemeSelector(), column: 1);

            var next = CommandButton("Open second screen", Palette.Primary, Palette.SelectionText);
            next.Height = 42;
            next.ToolTip = "Navigate to the secondary demo screen";
            next.Click += (sender, args) => ScreenEngine?.NavigateToScreen(new SecondScreen(_themePreset, _applyTheme));
            header.AddChild(next, column: 2);

            return header;
        }

        private ComboBox CreateThemeSelector()
        {
            var combo = new ComboBox
            {
                Margin = new Thickness(0, 0, 12, 0),
                Height = 42,
                BackgroundBrush = SurfaceAltBrush,
                TextColor = Palette.Text,
                HoverTextColor = Palette.Text,
                PressedTextColor = Palette.SelectionText,
                ToolTip = "Switch demo theme"
            };

            foreach (var preset in DemoThemeRegistry.Presets)
                combo.Items.Add(preset);

            combo.SelectedIndex = DemoThemeRegistry.IndexOf(_themePreset.Id);
            combo.SelectionChanged += (sender, args) =>
            {
                if (combo.SelectedItem is not DemoThemePreset preset)
                    return;
                if (string.Equals(preset.Id, _themePreset.Id, StringComparison.OrdinalIgnoreCase))
                    return;

                _applyTheme(preset);
                ClearFlyOut();
                ApplyThemePreset(preset);
            };
            return combo;
        }

        private Control CreateTabs()
        {
            var tabs = new TabControl
            {
                BackgroundBrush = IsGlassTheme ? null : SurfaceBrush,
                HeaderHeight = 38,
                HeaderBackground = IsGlassTheme ? new SolidColorBrush(new Color(255, 255, 255, 26)) : SurfaceBrush,
                SelectedHeaderBackground = SelectionBrush,
                HeaderTextColor = Palette.TabText,
                SelectedHeaderTextColor = Palette.SelectedTabText
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

            panel.AddChild(Label("Single-line TextBox", Palette.MutedText));
            var textBox = new TextBox
            {
                HintText = "Type a screen note",
                ToolTip = "Click or tap to focus text input"
            };
            textBox.TextChanged += (sender, args) => _status.Text = $"Text: {args.NewText}";
            panel.AddChild(FieldFrame(textBox, 38, new Thickness(0, 6, 12, 10)));

            panel.AddChild(Label("Password TextBox", Palette.MutedText));
            var passwordBox = new TextBox
            {
                HintText = "Password",
                PasswordChar = '*',
                ToolTip = "Password input masks display and disables copy"
            };
            passwordBox.TextChanged += (sender, args) => _status.Text = $"Password length: {args.NewText.Length}";
            panel.AddChild(FieldFrame(passwordBox, 38, new Thickness(0, 6, 12, 10)));

            panel.AddChild(Label("Multiline TextBox", Palette.MutedText));
            var multilineBox = new TextBox
            {
                HintText = "Write a short note",
                IsMultiline = true,
                ToolTip = "Enter adds lines; Ctrl+Enter submits"
            };
            multilineBox.EnterPressed += (sender, args) => _status.Text = "Multiline submitted";
            multilineBox.TextChanged += (sender, args) => _status.Text = $"Lines: {args.NewText.Split('\n').Length}";
            panel.AddChild(FieldFrame(multilineBox, 86, new Thickness(0, 6, 12, 14)));

            panel.AddChild(Label("ComboBox", Palette.MutedText));
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
                TextColor = Palette.Text,
                BoxBorderBrush = Palette.Primary,
                CheckMarkBrush = Palette.Selection,
                ToolTip = "Toggle a checkbox state"
            };
            checkBox.Checked += (sender, args) => _status.Text = args.IsChecked ? "Layout guides on" : "Layout guides off";
            panel.AddChild(checkBox);

            var toggle = new ToggleButton
            {
                Text = "Toggle preview mode",
                Margin = new Thickness(0, 0, 12, 10),
                Height = 38,
                BackgroundBrush = SurfaceAltBrush,
                TextColor = Palette.Text,
                ToggleBrush = SelectionBrush,
                ToggleTextColor = Palette.SelectionText,
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

            panel.AddChild(Label("Scrollable items", Palette.MutedText));
            var listBox = new ListBox
            {
                Height = 190,
                Margin = new Thickness(0, 6, 12, 10),
                ItemHeight = 30,
                ItemBackgroundBrush = SurfaceBrush,
                SelectedItemBackgroundBrush = SelectionBrush,
                ItemTextColor = Palette.Text,
                SelectedItemTextColor = Palette.SelectionText,
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

            var primary = CommandButton("Primary action", Palette.Primary, Palette.SelectionText);
            primary.ToolTip = "Run the primary demo action";
            primary.Click += (sender, args) => _status.Text = "Primary action clicked";
            panel.AddChild(primary);

            var secondary = CommandButton("Secondary action", Palette.Secondary, Palette.SelectionText);
            secondary.ToolTip = "Run the secondary demo action";
            secondary.Click += (sender, args) => _status.Text = "Secondary action clicked";
            panel.AddChild(secondary);

            var danger = CommandButton("Danger action", Palette.Danger, Palette.SelectionText);
            danger.ToolTip = "Run the destructive demo action";
            danger.Click += (sender, args) => _status.Text = "Danger action clicked";
            panel.AddChild(danger);

            var disabled = CommandButton("Disabled action", Palette.DisabledSurface, Palette.DisabledText);
            disabled.IsEnabled = false;
            panel.AddChild(disabled);

            panel.AddChild(Label("ImageButton", Palette.MutedText, 14, new Thickness(0, 8, 0, 2)));
            var imageButton = new ImageButton
            {
                Source = _deleteIcon,
                Width = 46,
                Height = 46,
                Margin = new Thickness(0, 2, 0, 12),
                BackgroundBrush = SurfaceAltBrush,
                TintColor = Palette.Text,
                ToolTip = "Delete the current demo item"
            };
            imageButton.Click += (sender, args) => _status.Text = "ImageButton clicked";
            panel.AddChild(imageButton);

            var menuButton = CommandButton("Open context menu", SurfaceAltBrush, Palette.Text);
            menuButton.ToolTip = "Right-click or long-press to open commands";
            var menu = new ContextMenu { BackgroundBrush = SurfaceBrush };
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

            var auto = InfoTile("Auto", "Measures content", Palette.Primary);
            var star = InfoTile("Star", "Uses remaining space", Palette.Secondary);
            var fixedSize = InfoTile("Fixed", "260 px", Palette.Danger, new Thickness(0));
            grid.AddChild(auto);
            grid.AddChild(star, column: 1);
            grid.AddChild(fixedSize, column: 2);

            var preview = new Border
            {
                Margin = new Thickness(0, 14, 0, 14),
                BackgroundBrush = SurfaceBrush,
                BorderColor = Palette.Primary,
                BorderWidth = 2,
                Padding = 16,
                Content = Label("Resize the window. The header, tiles and status strip keep their roles.", Palette.Text, 16)
            };
            grid.AddChild(preview, row: 1, columnSpan: 3);

            var bottom = Label("Grid columns: auto, star, fixed", Palette.MutedText);
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
                var row = CommandButton($"Scrollable row {i:00}", i % 3 == 0 ? (Brush)Palette.Secondary : SurfaceAltBrush, i % 3 == 0 ? Palette.SelectionText : Palette.Text);
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
                BackgroundBrush = SurfaceBrush,
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

            root.AddChild(Label("500 controls in a 20 x 25 grid", Palette.Text, 16, new Thickness(0, 0, 0, 10)));

            var grid = new Grid
            {
                BackgroundBrush = SurfaceBrush
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
                        BackgroundBrush = index % 7 == 0 ? SurfaceAltBrush : SurfaceBrush,
                        BorderColor = index % 5 == 0 ? Palette.Primary : Palette.MutedText,
                        BorderWidth = 1,
                        Padding = 2,
                        Content = Label(index.ToString("000"), Palette.MutedText, 10)
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

            strip.AddChild(Label("STATUS", Palette.MutedText));
            _status.Margin = new Thickness(0);
            strip.AddChild(_status, column: 1);
            return strip;
        }

        private StackPanel PanelStack(string title)
        {
            var panel = IsGlassTheme
                ? new GlassStackPanel
                {
                    BorderBrush = new SolidColorBrush(new Color(255, 255, 255, 116)),
                    HighlightBrush = new SolidColorBrush(new Color(255, 255, 255, 150)),
                    ShadowBrush = new SolidColorBrush(new Color(0, 0, 0, 72))
                }
                : new StackPanel();

            panel.Orientation = Orientation.Vertical;
            panel.Margin = new Thickness(0, 0, 12, 0);
            panel.VerticalAlignment = IsGlassTheme ? VerticalAlignment.Top : VerticalAlignment.Stretch;
            panel.BackgroundBrush = SurfaceBrush;
            panel.AddChild(Label(title, Palette.HeadingText, 17, new Thickness(12, 10, 12, 8)));
            return panel;
        }

        private Border FieldFrame(TextBox textBox, float height, Thickness margin)
        {
            textBox.Margin = 0;
            textBox.Height = Size.Auto;

            return new Border
            {
                Height = height,
                Margin = margin,
                BackgroundBrush = FieldFrameBrush,
                BorderColor = Palette.FieldBorder,
                BorderWidth = new Thickness(IsGlassTheme ? 2 : 1),
                Padding = new Thickness(2),
                Content = textBox
            };
        }

        private Border InfoTile(string title, string detail, Color accent)
        {
            return InfoTile(title, detail, accent, new Thickness(0, 0, 12, 0));
        }

        private Border InfoTile(string title, string detail, Color accent, Thickness margin)
        {
            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = 0
            };
            stack.AddChild(Label(title, accent, 18, new Thickness(0, 0, 0, 2)));
            stack.AddChild(Label(detail, Palette.MutedText));

            return new Border
            {
                Margin = margin,
                BackgroundBrush = SurfaceBrush,
                BorderColor = accent,
                BorderWidth = 2,
                Padding = new Thickness(12, 10, 14, 10),
                Content = stack
            };
        }

        private TextButton CommandButton(string text, Brush background, Color foreground)
        {
            return new TextButton(text)
            {
                Height = 38,
                Margin = new Thickness(0, 0, 12, 8),
                BackgroundBrush = background,
                TextColor = foreground,
                HoverTextColor = Palette.Text,
                PressedTextColor = Palette.Background
            };
        }

        private TextBlock Label(string text, Color color)
        {
            return Label(text, color, 14, new Thickness(0, 3));
        }

        private TextBlock Label(string text, Color color, int size)
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
