using Microsoft.Xna.Framework;
using MonoGame.PortableUI;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Demo.Android
{
    /// <summary>
    /// A single scrollable screen exercising the core touch-driven controls on Android:
    /// a tap counter button, a text box (soft keyboard), and a selectable list.
    /// </summary>
    public sealed class AndroidDemoScreen : Screen
    {
        private static readonly Color Heading = new Color(20, 30, 40);
        private static readonly Color Muted = new Color(105, 115, 125);
        private static readonly Color Surface = new Color(245, 245, 245);
        private static readonly Color Accent = new Color(20, 126, 133);

        private int _tapCount;

        private readonly System.Action<bool>? _setFullscreen;
        private readonly SafeAreaPanel _safeArea;
        private StackPanel? _contentPanel;

        /// <param name="setFullscreen">Hides (true) or shows the system bars; from the activity.</param>
        public AndroidDemoScreen(System.Action<bool>? setFullscreen = null)
        {
            _setFullscreen = setFullscreen;
            BackgroundBrush = Color.White;
            // The panel fills the whole window (also under the bars and the cutout) with the surface
            // colour and pads only its content into the safe area.
            _safeArea = new SafeAreaPanel(BuildContent()) { BackgroundBrush = Surface };
            Content = _safeArea;
        }

        private Control BuildContent()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = 0,
                BackgroundBrush = Surface
            };
            _contentPanel = panel;

            // Top row: fullscreen switch (hides status and navigation bar; swipe from an edge to peek).
            var fullscreenRow = new Grid
            {
                Margin = new Thickness(16, 12, 16, 0),
                ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition { Width = GridLength.Auto } }
            };
            fullscreenRow.AddChild(new TextBlock
            {
                Text = "Fullscreen",
                TextColor = Heading,
                TextSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            }, column: 0);
            var fullscreenSwitch = new ToggleSwitch
            {
                Width = 52,
                Height = 30,
                VerticalAlignment = VerticalAlignment.Center,
                OffTrackBrush = new SolidColorBrush(new Color(185, 190, 195)),
                OnTrackBrush = new SolidColorBrush(Accent)
            };
            fullscreenSwitch.Toggled += (_, _) =>
            {
                if (fullscreenSwitch.IsOn)
                {
                    // Fullscreen: the scroll viewer reaches the very top (content scrolls under the
                    // camera area), while the former top inset moves into the scrolled content, so the
                    // first row - and this switch - start where they were, clear of any cutout.
                    var top = _safeArea.AppliedInsets.Top;
                    _safeArea.Edges = SafeAreaEdges.None;
                    if (_contentPanel != null)
                        _contentPanel.Margin = new Thickness(0, top, 0, 0);
                }
                else
                {
                    _safeArea.Edges = SafeAreaEdges.All;
                    if (_contentPanel != null)
                        _contentPanel.Margin = new Thickness(0);
                }
                _setFullscreen?.Invoke(fullscreenSwitch.IsOn);
            };
            fullscreenRow.AddChild(fullscreenSwitch, column: 1);
            panel.AddChild(fullscreenRow);

            panel.AddChild(new TextBlock
            {
                Text = "MonoGame.PortableUI",
                TextColor = Heading,
                TextSize = 24,
                Margin = new Thickness(16, 16, 16, 2)
            });
            panel.AddChild(new TextBlock
            {
                Text = "Running on Android",
                TextColor = Muted,
                TextSize = 14,
                Margin = new Thickness(16, 0, 16, 16)
            });

            var tapLabel = new TextBlock
            {
                Text = "Taps: 0",
                TextColor = Heading,
                TextSize = 16,
                Margin = new Thickness(16, 4, 16, 4)
            };

            var tapButton = new TextButton("Tap me")
            {
                BackgroundBrush = Accent,
                TextColor = Color.White,
                Height = 56,
                Margin = new Thickness(16, 4, 16, 16)
            };
            tapButton.Click += (_, _) =>
            {
                _tapCount++;
                tapLabel.Text = $"Taps: {_tapCount}";
            };
            panel.AddChild(tapLabel);
            panel.AddChild(tapButton);

            panel.AddChild(new TextBlock
            {
                Text = "Type (soft keyboard):",
                TextColor = Muted,
                TextSize = 14,
                Margin = new Thickness(16, 4, 16, 4)
            });
            panel.AddChild(new TextBox
            {
                HintText = "Enter some text...",
                Margin = new Thickness(16, 0, 16, 8)
            });
            panel.AddChild(new TextBox
            {
                HintText = "E-mail (InputPurpose.Email)",
                InputPurpose = MonoGame.PortableUI.Input.TextInputPurpose.Email,
                Margin = new Thickness(16, 0, 16, 16)
            });

            panel.AddChild(new TextBlock
            {
                Text = $"SpriteFont vs FontStashSharp · font scale {MonoGame.PortableUI.Text.TextScaling.Factor:0.00}",
                TextColor = Muted,
                TextSize = 14,
                Margin = new Thickness(16, 4, 16, 4)
            });
            panel.AddChild(new TextBlock { Text = "Size 24: Zoë", TextSize = 24, TextColor = Heading, FontOverride = FontManager.DefaultFont, Margin = new Thickness(16, 0, 16, 0) });
            if (DemoFonts.Selawik is { } selawik)
            {
                panel.AddChild(new TextBlock { Text = "Size 24: Zoë", TextSize = 24, TextColor = Heading, DynamicFont = selawik, Margin = new Thickness(16, 0, 16, 0) });
                panel.AddChild(new TextBlock { Text = "Łódź · ½ · € · naïve", TextSize = 18, TextColor = Muted, DynamicFont = selawik, Margin = new Thickness(16, 0, 16, 16) });
            }

            panel.AddChild(new TextBlock
            {
                Text = "Pick a fruit (scroll + tap):",
                TextColor = Muted,
                TextSize = 14,
                Margin = new Thickness(16, 4, 16, 4)
            });

            var list = new ListBox
            {
                Height = 260,
                Margin = new Thickness(16, 0, 16, 16)
            };
            foreach (var item in new[]
            {
                "Apple", "Banana", "Cherry", "Date", "Elderberry",
                "Fig", "Grape", "Honeydew", "Kiwi", "Lemon", "Mango", "Nectarine"
            })
            {
                list.Items.Add(item);
            }
            panel.AddChild(list);

            panel.AddChild(new TextBlock
            {
                Text = "Data grid (tap a header to sort):",
                TextColor = Muted,
                TextSize = 14,
                Margin = new Thickness(16, 4, 16, 4)
            });
            panel.AddChild(BuildDataGrid());

            return new ScrollViewer
            {
                ScrollOrientation = Orientation.Vertical,
                Content = panel
            };
        }

        private DataGrid BuildDataGrid()
        {
            var grid = new DataGrid
            {
                Height = 220,
                Margin = new Thickness(16, 0, 16, 16),
                SelectedRowBackgroundBrush = Accent,
                SelectedRowTextColor = Color.White,
                RowHeight = 34,
                HeaderHeight = 36
            };
            grid.Columns.Add(new DataGridColumn
            {
                Header = "Fruit",
                Width = new GridLength(2, GridLengthUnit.Relative),
                CellText = i => (((string Name, int Qty))i).Name,
                SortKey = i => (((string Name, int Qty))i).Name
            });
            grid.Columns.Add(new DataGridColumn
            {
                Header = "Qty",
                Width = new GridLength(70, GridLengthUnit.Absolute),
                CellAlignment = TextAlignment.Right,
                CellText = i => (((string Name, int Qty))i).Qty.ToString(),
                SortKey = i => (((string Name, int Qty))i).Qty
            });
            grid.Items.AddRange(new object[]
            {
                ("Apple", 12), ("Banana", 7), ("Cherry", 40), ("Date", 3),
                ("Elderberry", 21), ("Fig", 9), ("Grape", 55), ("Kiwi", 18)
            });
            grid.Refresh();
            grid.SortBy(grid.Columns[0], true);
            return grid;
        }
    }
}
