using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Demo
{
    public sealed class SecondScreen : Screen
    {
        private readonly Action<DemoThemePreset> _applyTheme;
        private DemoThemePreset _themePreset;

        private DemoThemePalette Palette => _themePreset.Palette;

        public SecondScreen(DemoThemePreset themePreset, Action<DemoThemePreset> applyTheme)
        {
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
            BackgroundBrush = _themePreset.BackgroundColor;
            Content = CreateLayout();
        }

        private Control CreateLayout()
        {
            var root = new Grid
            {
                Margin = 24,
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition()
                }
            };

            root.AddChild(CreateHeader());

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 18, 0, 0),
                BackgroundBrush = Palette.Surface
            };

            panel.AddChild(new TextBlock
            {
                Text = "Second screen",
                TextColor = Palette.HeadingText,
                TextSize = 18,
                Margin = new Thickness(12, 12, 12, 4)
            });

            panel.AddChild(new TextBlock
            {
                Text = $"Active theme: {_themePreset.DisplayName}",
                TextColor = Palette.MutedText,
                TextSize = 14,
                Margin = new Thickness(12, 0, 12, 10)
            });

            var back = CommandButton("Navigate back", Palette.SurfaceAlt, Palette.Text);
            back.Height = 48;
            back.Margin = new Thickness(12, 8, 12, 12);
            back.Click += (sender, args) =>
            {
                var engine = ScreenEngine;
                engine?.NavigateBack();
                if (engine?.ActiveScreen is MainScreen mainScreen)
                    mainScreen.ApplyThemePreset(_themePreset);
            };
            panel.AddChild(back);

            root.AddChild(panel, row: 1);
            return root;
        }

        private Control CreateHeader()
        {
            var header = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(),
                    new ColumnDefinition { Width = new GridLength(230) }
                }
            };

            var titleStack = new StackPanel
            {
                Orientation = Orientation.Vertical
            };
            titleStack.AddChild(Label("MonoGame.PortableUI", Palette.HeadingText, 22, new Thickness(0, 0, 0, 2)));
            titleStack.AddChild(Label("SECOND SCREEN", Palette.MutedText));
            header.AddChild(titleStack);

            header.AddChild(CreateThemeSelector(), column: 1);
            return header;
        }

        private ComboBox CreateThemeSelector()
        {
            var combo = new ComboBox
            {
                Height = 42,
                BackgroundBrush = Palette.SurfaceAlt,
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

        private TextButton CommandButton(string text, Color background, Color foreground)
        {
            return new TextButton(text)
            {
                BackgroundBrush = background,
                TextColor = foreground,
                HoverTextColor = Palette.Text,
                PressedTextColor = Palette.Background
            };
        }

        private static TextBlock Label(string text, Color color)
        {
            return Label(text, color, 14, new Thickness(0, 3));
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
