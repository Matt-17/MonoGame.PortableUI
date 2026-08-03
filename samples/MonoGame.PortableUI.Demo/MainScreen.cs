using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Animation;
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
        private TextBlock _themeTitle = null!;
        private TabControl? _tabs;
        private Color _inspectorColor;
        private Border? _inspectorColorPreview;
        private TextBlock? _inspectorCode;
        private ProgressBar? _liveProgress;
        private TextBlock? _liveProgressLabel;

        private ThemePalette Palette => _themePreset.Palette;
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

        protected override void OnBeforeDraw(SpriteBatch spriteBatch)
        {
            base.OnBeforeDraw(spriteBatch);
            if (_liveProgress == null)
                return;

            // Simulated task: 0 → 100 % over six seconds, restarting — a real, moving value.
            var percent = (float)(ScreenSystem.TotalTime.TotalSeconds % 6 / 6 * 100);
            _liveProgress.Value = percent;
            if (_liveProgressLabel != null)
                _liveProgressLabel.Text = $"{percent:0}%";
        }

        public void ApplyThemePreset(DemoThemePreset themePreset)
        {
            _themePreset = themePreset ?? DemoThemeRegistry.Default;

            // The demo styles every control from the preset palette at construction time, so a theme
            // change has to rebuild the tree. The selected tab survives the rebuild.
            var selectedTab = _tabs?.SelectedIndex ?? 0;
            RebuildContent();
            if (_tabs != null && selectedTab >= 0 && selectedTab < _tabs.Items.Count)
                _tabs.SelectedIndex = selectedTab;
            _status.Text = $"Theme applied: {_themePreset.DisplayName}";
        }

        internal bool TrySelectTab(string? header)
        {
            if (_tabs == null || string.IsNullOrWhiteSpace(header))
                return false;

            for (var i = 0; i < _tabs.Items.Count; i++)
            {
                if (string.Equals(_tabs.Items[i].Header, header.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    _tabs.SelectedIndex = i;
                    return true;
                }
            }

            return false;
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
                    new ColumnDefinition { Width = new GridLength(230) }
                }
            };

            var titleStack = new StackPanel
            {
                Orientation = Orientation.Vertical
            };
            titleStack.AddChild(Label("MonoGame.PortableUI", Palette.HeadingText, 22, new Thickness(0, 0, 0, 2)));
            var themeTitle = _themePreset.DisplayName.ToUpperInvariant();
            if (!themeTitle.EndsWith(" THEME", StringComparison.Ordinal))
                themeTitle += " THEME";
            _themeTitle = Label($"{themeTitle} - CODE-FIRST CONTROLS", Palette.MutedText);
            titleStack.AddChild(_themeTitle);
            header.AddChild(titleStack);

            header.AddChild(CreateThemeSelector(), column: 1);

            var navigation = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            var next = CommandButton("Second screen", Palette.Primary, Palette.SelectionText);
            next.Height = 30;
            next.ToolTip = "Navigate to the secondary demo screen";
            next.Click += (sender, args) => ScreenEngine?.NavigateToScreen(new SecondScreen(_themePreset, _applyTheme));
            navigation.AddChild(next);

            var worldSpace = CommandButton("World space demo", Palette.Secondary, Palette.SelectionText);
            worldSpace.Height = 30;
            worldSpace.ToolTip = "Interactive UISurface on a 3D quad (raycast input)";
            worldSpace.Click += (sender, args) =>
            {
                if (ScreenEngine != null)
                    ScreenEngine.NavigateToScreen(new WorldSpaceScreen(ScreenEngine.Game, _themePreset));
            };
            navigation.AddChild(worldSpace);
            header.AddChild(navigation, column: 2);

            return header;
        }

        private ComboBox CreateThemeSelector()
        {
            var combo = new ComboBox
            {
                Margin = new Thickness(0, 0, 12, 0),
                Height = 42,
                DropDownMaxHeight = 440,
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

            tabs.Items.Add(new TabItem { Header = "Gallery", Content = CreateGalleryTab() });
            tabs.Items.Add(new TabItem { Header = "Inspector", Content = CreateInspectorTab() });
            tabs.Items.Add(new TabItem { Header = "Controls", Content = CreateControlsTab() });
            tabs.Items.Add(new TabItem { Header = "Visual FX", Content = CreateVisualFxTab() });
            tabs.Items.Add(new TabItem { Header = "Motion", Content = CreateMotionTab() });
            tabs.Items.Add(new TabItem { Header = "Layout", Content = CreateLayoutTab() });
            tabs.Items.Add(new TabItem { Header = "Drag & drop", Content = CreateDragDropTab() });
            tabs.Items.Add(new TabItem { Header = "Scroll", Content = CreateScrollTab() });
            tabs.Items.Add(new TabItem { Header = "Stress", Content = CreateStressTab() });
            tabs.SelectedIndex = 0;

            _tabs = tabs;
            return tabs;
        }

        private Control CreateGalleryTab()
        {
            var scroller = new ScrollViewer
            {
                Margin = 16,
                BackgroundBrush = SurfaceBrush,
                ScrollOrientation = Orientation.Vertical
            };

            var grid = new Grid
            {
                Margin = 8
            };

            for (var column = 0; column < 3; column++)
                grid.ColumnDefinitions.Add(new ColumnDefinition());

            for (var i = 0; i < DemoThemeRegistry.Presets.Count; i++)
            {
                if (i % 3 == 0)
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var preset = DemoThemeRegistry.Presets[i];
                grid.AddChild(CreateThemeCard(preset), i / 3, i % 3);
            }

            scroller.Content = grid;
            return scroller;
        }

        private Control CreateThemeCard(DemoThemePreset preset)
        {
            var palette = preset.Palette;
            var theme = preset.SharedTheme;
            var card = new Button
            {
                Margin = new Thickness(0, 0, 12, 12),
                Padding = 10,
                BackgroundBrush = palette.SurfaceBrush ?? palette.Surface,
                BorderBrush = palette.Primary,
                BorderThickness = string.Equals(preset.Id, _themePreset.Id, StringComparison.OrdinalIgnoreCase) ? 3 : 1,
                CornerRadius = 6,
                HoverColor = new SolidColorBrush(new Color((int)palette.Primary.R, (int)palette.Primary.G, (int)palette.Primary.B, 42)),
                PressedColor = palette.SelectionBrush ?? palette.Selection,
                ToolTip = $"Apply {preset.DisplayName}"
            };

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical
            };
            stack.AddChild(Label(preset.DisplayName, palette.HeadingText, 17, new Thickness(0, 0, 0, 2)));
            stack.AddChild(Label($"{preset.FontName} - {preset.Id}", palette.MutedText, 12, new Thickness(0, 0, 0, 8)));

            var preview = new ThemeIsland
            {
                Theme = theme,
                Content = CreateMiniPreview(palette, theme),
                // The mini controls are decoration; clicks must hit the card button only.
                IsHitTestVisible = false
            };
            stack.AddChild(preview);
            card.Content = stack;
            card.Click += (sender, args) =>
            {
                _applyTheme(preset);
                ApplyThemePreset(preset);
            };
            return card;
        }

        private Control CreateMiniPreview(ThemePalette palette, PortableTheme theme)
        {
            var preview = new Grid
            {
                Height = 118,
                BackgroundBrush = palette.BackgroundBrush ?? palette.Background,
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition(),
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            preview.AddChild(new TextBlock
            {
                Text = "Button  TextBox  Progress",
                TextColor = palette.Text,
                TextSize = 12,
                Margin = new Thickness(8, 6, 8, 4)
            });

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(8, 2, 8, 6)
            };
            row.AddChild(new TextButton("OK")
            {
                Width = 64,
                Height = 30,
                Margin = new Thickness(0, 0, 8, 0),
                BackgroundBrush = palette.SelectionBrush ?? palette.Selection,
                TextColor = palette.SelectionText,
                // Controls snapshot the global theme in their constructor; show the card's theme instead.
                Shadow = theme.ButtonShadow
            });
            row.AddChild(new TextBox
            {
                Width = 100,
                Height = 30,
                Text = "Aa",
                BackgroundBrush = palette.FieldFrameBrush ?? palette.FieldFrame,
                TextColor = palette.Text
            });
            preview.AddChild(row, row: 1);

            preview.AddChild(new ProgressBar
            {
                Value = 64,
                Margin = new Thickness(8, 0, 8, 8),
                BackgroundBrush = palette.FieldFrameBrush ?? palette.FieldFrame,
                FillBrush = palette.SelectionBrush ?? palette.Selection
            }, row: 2);
            return preview;
        }

        private Control CreateInspectorTab()
        {
            _inspectorColor = Palette.Primary;
            var grid = new Grid
            {
                Margin = 16,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(260) },
                    new ColumnDefinition()
                }
            };

            var swatches = PanelStack("Palette");
            AddInspectorSwatch(swatches, "Background", Palette.Background);
            AddInspectorSwatch(swatches, "Surface", Palette.Surface);
            AddInspectorSwatch(swatches, "Text", Palette.Text);
            AddInspectorSwatch(swatches, "Primary", Palette.Primary);
            AddInspectorSwatch(swatches, "Secondary", Palette.Secondary);
            AddInspectorSwatch(swatches, "Selection", Palette.Selection);
            grid.AddChild(swatches);

            var editor = PanelStack("RGB edit and export");
            _inspectorColorPreview = new Border
            {
                Height = 76,
                Margin = new Thickness(0, 0, 0, 12),
                BackgroundBrush = _inspectorColor,
                BorderBrush = Palette.FieldBorder,
                BorderThickness = 1,
                Content = Label("SELECTED COLOR", Palette.SelectionText, 16)
            };
            editor.AddChild(_inspectorColorPreview);

            editor.AddChild(LabeledSlider("Red", 0, 255, _inspectorColor.R, "0", value => UpdateInspectorColor(r: (byte)value)));
            editor.AddChild(LabeledSlider("Green", 0, 255, _inspectorColor.G, "0", value => UpdateInspectorColor(g: (byte)value)));
            editor.AddChild(LabeledSlider("Blue", 0, 255, _inspectorColor.B, "0", value => UpdateInspectorColor(b: (byte)value)));

            _inspectorCode = Label("", Palette.Text, 13, new Thickness(0, 8, 0, 8));
            editor.AddChild(_inspectorCode);

            var export = CommandButton("Copy C# theme color", Palette.Primary, Palette.SelectionText);
            export.Click += (sender, args) =>
            {
                var code = CreateInspectorColorCode();
                ScreenEngine?.Options.ClipboardService.SetText(code);
                _status.Text = "Inspector color copied";
            };
            editor.AddChild(export);
            UpdateInspectorCode();
            grid.AddChild(editor, column: 1);
            return grid;
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

        private Control CreateVisualFxTab()
        {
            var grid = new Grid
            {
                Margin = 16,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(300) },
                    new ColumnDefinition()
                }
            };

            var controls = PanelStack("Visual FX");
            var gradient = new LinearGradientBrush(
                new GradientStop(0, Palette.Primary),
                new GradientStop(0.55f, Palette.Secondary),
                new GradientStop(1, Palette.Info))
            {
                AngleDegrees = 35
            };
            var sample = new Border
            {
                Margin = new Thickness(18, 0, 0, 0),
                Padding = new Thickness(24),
                BackgroundBrush = gradient,
                BorderBrush = Palette.Primary,
                BorderThickness = 2,
                CornerRadius = 8,
                Shadow = ShadowStyle.Level2(),
                Content = Label("FX SAMPLE", Palette.SelectionText, 22)
            };

            controls.AddChild(LabeledSlider("Corner radius", 0, 24, 8, "0", value =>
            {
                sample.CornerRadius = value;
                _status.Text = $"Corner radius: {value:0}";
            }));

            controls.AddChild(LabeledSlider("Shadow blur", 0, 24, sample.Shadow!.Blur, "0", value =>
            {
                sample.Shadow!.Blur = value;
                _status.Text = $"Shadow blur: {value:0}";
            }));

            controls.AddChild(LabeledSlider("Shadow offset X", -18, 18, sample.Shadow!.Offset.X, "0", value =>
            {
                sample.Shadow!.Offset = new Vector2(value, sample.Shadow!.Offset.Y);
                _status.Text = $"Shadow offset X: {value:0}";
            }));

            controls.AddChild(LabeledSlider("Shadow offset Y", -18, 18, sample.Shadow!.Offset.Y, "0", value =>
            {
                sample.Shadow!.Offset = new Vector2(sample.Shadow!.Offset.X, value);
                _status.Text = $"Shadow offset Y: {value:0}";
            }));

            controls.AddChild(LabeledSlider("Shadow opacity", 0, 100, sample.Shadow!.Opacity * 100, "0'%'", value =>
            {
                sample.Shadow!.Opacity = value / 100f;
                _status.Text = $"Shadow opacity: {value:0}%";
            }));

            controls.AddChild(LabeledSlider("Gradient angle", 0, 180, gradient.AngleDegrees, "0' deg'", value =>
            {
                gradient.AngleDegrees = value;
                _status.Text = $"Gradient angle: {value:0}";
            }));

            grid.AddChild(controls);
            grid.AddChild(sample, column: 1);
            return grid;
        }

        private Control CreateMotionTab()
        {
            var grid = new Grid
            {
                Margin = 16,
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(130) },
                    new RowDefinition()
                }
            };

            var stage = new Border
            {
                BackgroundBrush = SurfaceBrush,
                BorderBrush = Palette.Primary,
                BorderThickness = 1,
                Padding = 12
            };
            var box = new Border
            {
                Width = 96,
                Height = 58,
                BackgroundBrush = SelectionBrush,
                BorderBrush = Palette.Secondary,
                BorderThickness = 2,
                Content = Label("MOVE", Palette.SelectionText, 14)
            };
            stage.Content = box;
            grid.AddChild(stage);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 14, 0, 0)
            };
            buttons.AddChild(MotionButton("Linear", box, Easings.Linear));
            buttons.AddChild(MotionButton("Quad", box, Easings.QuadOut));
            buttons.AddChild(MotionButton("Back", box, Easings.BackOut));
            buttons.AddChild(MotionButton("Elastic", box, Easings.ElasticOut));
            buttons.AddChild(MotionButton("Bounce", box, Easings.BounceOut));
            grid.AddChild(buttons, row: 1);
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
            panel.AddChild(FieldFrame(textBox, 38, new Thickness(0, 6, 0, 10)));

            panel.AddChild(Label("Password TextBox", Palette.MutedText));
            var passwordBox = new TextBox
            {
                HintText = "Password",
                PasswordChar = '*',
                ToolTip = "Password input masks display and disables copy"
            };
            passwordBox.TextChanged += (sender, args) => _status.Text = $"Password length: {args.NewText.Length}";
            panel.AddChild(FieldFrame(passwordBox, 38, new Thickness(0, 6, 0, 10)));

            panel.AddChild(Label("Multiline TextBox", Palette.MutedText));
            var multilineBox = new TextBox
            {
                HintText = "Write a short note",
                IsMultiline = true,
                ToolTip = "Enter adds lines; Ctrl+Enter submits"
            };
            multilineBox.EnterPressed += (sender, args) => _status.Text = "Multiline submitted";
            multilineBox.TextChanged += (sender, args) => _status.Text = $"Lines: {args.NewText.Split('\n').Length}";
            panel.AddChild(FieldFrame(multilineBox, 86, new Thickness(0, 6, 0, 14)));

            panel.AddChild(Label("Read-only TextBox", Palette.MutedText));
            var readOnlyBox = new TextBox
            {
                Text = "Read-only: try to edit me",
                IsReadOnly = true,
                ToolTip = "IsReadOnly blocks typing/cut/paste but allows selection"
            };
            panel.AddChild(FieldFrame(readOnlyBox, 38, new Thickness(0, 6, 0, 10)));

            panel.AddChild(Label("ComboBox", Palette.MutedText));
            var combo = new ComboBox { Margin = new Thickness(0, 6, 0, 14), Height = 38, ToolTip = "Choose a density preset" };
            combo.Items.Add("Compact density");
            combo.Items.Add("Comfortable density");
            combo.Items.Add("Touch density");
            combo.SelectedIndex = 1;
            combo.SelectionChanged += (sender, args) => _status.Text = $"Density: {combo.SelectedItem}";
            panel.AddChild(combo);

            return panel;
        }

        private Control CreateListPanel()
        {
            var panel = PanelStack("ListBox");

            panel.AddChild(Label("Scrollable items", Palette.MutedText));
            var listBox = new ListBox
            {
                Height = 132,
                Margin = new Thickness(0, 6, 0, 10),
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

            panel.AddChild(Label("Slider + ProgressBar", Palette.MutedText));
            var progressBar = new ProgressBar
            {
                Value = 42,
                Margin = new Thickness(0, 6, 0, 8),
                BackgroundBrush = FieldFrameBrush,
                FillBrush = SelectionBrush,
                ToolTip = "Determinate progress value"
            };
            var slider = new Slider
            {
                Value = 42,
                Margin = new Thickness(0, 0, 0, 10),
                TrackBrush = FieldFrameBrush,
                FillBrush = SelectionBrush,
                ThumbBrush = SurfaceAltBrush,
                ThumbBorderBrush = Palette.Primary,
                ToolTip = "Drag or use arrow keys to update progress"
            };
            slider.ValueChanged += (sender, args) =>
            {
                progressBar.Value = args.NewValue;
                _status.Text = $"Slider: {args.NewValue:0}%";
            };
            panel.AddChild(progressBar);
            panel.AddChild(slider);

            var liveHeader = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(),
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };
            _liveProgressLabel = Label("0%", Palette.Text);
            liveHeader.AddChild(Label("Live + indeterminate", Palette.MutedText));
            liveHeader.AddChild(_liveProgressLabel, column: 1);
            panel.AddChild(liveHeader);
            _liveProgress = new ProgressBar
            {
                Margin = new Thickness(0, 4, 0, 6),
                BackgroundBrush = FieldFrameBrush,
                FillBrush = SelectionBrush,
                ToolTip = "Real value animated by the demo loop"
            };
            panel.AddChild(_liveProgress);
            panel.AddChild(new ProgressBar
            {
                IsIndeterminate = true,
                Margin = new Thickness(0, 0, 0, 8),
                BackgroundBrush = FieldFrameBrush,
                FillBrush = SelectionBrush,
                ToolTip = "Indeterminate marquee"
            });

            var mediaRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(),
                    new ColumnDefinition()
                }
            };
            var indicatorCell = new StackPanel { Orientation = Orientation.Vertical };
            indicatorCell.AddChild(Label("ProgressIndicator", Palette.MutedText));
            indicatorCell.AddChild(new ProgressIndicator
            {
                Height = 34,
                Margin = new Thickness(0, 2, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = Palette.Primary,
                ToolTip = "Indeterminate busy indicator"
            });
            mediaRow.AddChild(indicatorCell);

            var imageCell = new StackPanel { Orientation = Orientation.Vertical };
            imageCell.AddChild(Label("Image", Palette.MutedText));
            imageCell.AddChild(new Image
            {
                Source = _deleteIcon,
                Width = 32,
                Height = 32,
                Margin = new Thickness(0, 2, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                TintColor = Palette.Text,
                ToolTip = "Image control with tint color"
            });
            mediaRow.AddChild(imageCell, column: 1);
            panel.AddChild(mediaRow);

            return panel;
        }

        private Control CreateActionPanel()
        {
            var panel = PanelStack("Buttons and menus");

            var primary = CommandButton("Primary action", Palette.Primary, Palette.SelectionText);
            primary.PressedHorizontalInset = 5;
            primary.PressedVerticalInset = 3;
            primary.PressedTranslation = new Vector2(0, 1);
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

            panel.AddChild(Label("ImageButton + ContextMenu", Palette.MutedText, 14, new Thickness(0, 8, 0, 2)));
            var buttonRow = new Grid
            {
                Margin = new Thickness(0, 2, 0, 4),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition()
                }
            };
            var imageButton = new ImageButton
            {
                Source = _deleteIcon,
                Width = 46,
                Height = 38,
                Margin = new Thickness(0, 0, 8, 8),
                BackgroundBrush = SurfaceAltBrush,
                TintColor = Palette.Text,
                ToolTip = "Delete the current demo item"
            };
            imageButton.Click += (sender, args) => _status.Text = "ImageButton clicked";
            buttonRow.AddChild(imageButton);

            var menuButton = CommandButton("Menu (left click)", SurfaceAltBrush, SurfaceAltTextColor);
            menuButton.ToolTip = "Opens on left click (also right-click/long-press)";
            var menu = new ContextMenu { BackgroundBrush = SurfaceBrush, ContextMenuType = ContextMenuTypes.OpenOnLeftClick };
            menu.Items.Add(new MenuItem("Inspect", () => _status.Text = "Inspect command"));
            menu.Items.Add(new MenuItem("Duplicate", () => _status.Text = "Duplicate command"));
            menu.Items.Add(new MenuItem("Archive", () => _status.Text = "Archive command"));
            menu.ItemInvoked += (sender, args) => _status.Text = $"Menu: {args.Item.Text}";
            menuButton.ContextMenu = menu;
            buttonRow.AddChild(menuButton, column: 1);
            panel.AddChild(buttonRow);

            panel.AddChild(Label("Selection states", Palette.MutedText, 14, new Thickness(0, 4, 0, 2)));
            var checkBox = new CheckBox
            {
                Text = "Enable layout guides",
                Margin = new Thickness(0, 0, 0, 8),
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
                Margin = new Thickness(0, 0, 0, 10),
                Height = 38,
                BackgroundBrush = SurfaceAltBrush,
                TextColor = SurfaceAltTextColor,
                ToggleBrush = SelectionBrush,
                ToggleTextColor = Palette.SelectionText,
                ToolTip = "Toggle the preview mode state"
            };
            toggle.Checked += (sender, args) => _status.Text = args.IsChecked ? "Preview mode on" : "Preview mode off";
            panel.AddChild(toggle);

            var radioA = new RadioButton { Text = "Mouse first", RadioGroup = "input", Margin = new Thickness(0, 0, 0, 4), Height = 32 };
            var radioB = new RadioButton { Text = "Touch first", RadioGroup = "input", Margin = new Thickness(0, 0, 0, 0), Height = 32 };
            radioA.Checked += (sender, args) => _status.Text = "Input profile: mouse";
            radioB.Checked += (sender, args) => _status.Text = "Input profile: touch";
            panel.AddChild(radioA);
            panel.AddChild(radioB);

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

        private readonly List<(string Title, int Column)> _dragCards = new List<(string, int)>
        {
            ("Fix save-game crash", 0),
            ("Design inventory UI", 0),
            ("Wire dialogue trees", 0),
            ("Playtest chapter 2", 1),
            ("Ship demo build", 2)
        };
        private readonly StackPanel?[] _dragColumns = new StackPanel?[3];

        private Control CreateDragDropTab()
        {
            var grid = new Grid
            {
                Margin = 16,
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition()
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition(),
                    new ColumnDefinition(),
                    new ColumnDefinition()
                }
            };

            grid.AddChild(Label("Drag the issue cards between columns (Esc or right-click cancels).", Palette.MutedText, 14, new Thickness(0, 0, 0, 8)), columnSpan: 3);

            var headers = new[] { "Backlog", "Doing", "Done" };
            for (var column = 0; column < 3; column++)
            {
                var columnIndex = column;
                var stack = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Padding = new Thickness(10)
                };
                _dragColumns[column] = stack;
                var frame = new Border
                {
                    Margin = new Thickness(0, 0, column < 2 ? 12 : 0, 0),
                    BackgroundBrush = SurfaceBrush,
                    BorderColor = Palette.FieldBorder,
                    BorderWidth = 1,
                    AllowDrop = true,
                    Content = stack
                };
                frame.DragEnter += (sender, args) =>
                {
                    args.Effect = DragDropEffects.Move;
                    frame.BorderColor = Palette.Primary;
                    frame.BorderWidth = 2;
                    _status.Text = $"Drag over {headers[columnIndex]}";
                };
                frame.DragOver += (sender, args) => args.Effect = DragDropEffects.Move;
                frame.DragLeave += (sender, args) =>
                {
                    frame.BorderColor = Palette.FieldBorder;
                    frame.BorderWidth = 1;
                };
                frame.Drop += (sender, args) =>
                {
                    frame.BorderColor = Palette.FieldBorder;
                    frame.BorderWidth = 1;
                    if (args.Payload is string title)
                    {
                        var index = _dragCards.FindIndex(card => card.Title == title);
                        if (index >= 0)
                            _dragCards[index] = (title, columnIndex);
                        RebuildDragColumns();
                        _status.Text = $"Dropped '{title}' into {headers[columnIndex]}";
                    }
                };

                var panel = new StackPanel { Orientation = Orientation.Vertical };
                panel.AddChild(Label(headers[column], Palette.HeadingText, 16, new Thickness(2, 0, 0, 6)));
                panel.AddChild(frame);
                grid.AddChild(panel, row: 1, column: column);
            }

            RebuildDragColumns();
            return grid;
        }

        private void RebuildDragColumns()
        {
            for (var column = 0; column < 3; column++)
            {
                var stack = _dragColumns[column];
                if (stack == null)
                    continue;

                stack.Children.Clear();
                foreach (var card in _dragCards)
                {
                    if (card.Column == column)
                        stack.AddChild(CreateDragCard(card.Title));
                }
            }
        }

        private Control CreateDragCard(string title)
        {
            var card = new Border
            {
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(10, 8, 10, 8),
                BackgroundBrush = SurfaceAltBrush,
                BorderColor = Palette.Primary,
                BorderWidth = 1,
                CornerRadius = 4,
                Content = Label(title, SurfaceAltTextColor, 14, new Thickness(0))
            };
            card.MouseDown += (sender, args) =>
            {
                var ghost = new Border
                {
                    Width = 200,
                    Padding = new Thickness(10, 8, 10, 8),
                    BackgroundBrush = Palette.Primary,
                    CornerRadius = 4,
                    Content = Label(title, Palette.SelectionText, 14, new Thickness(0))
                };
                var operation = card.BeginDrag(title, DragDropEffects.Move, ghost);
                if (operation != null)
                {
                    operation.GrabOffset = new PointF(100, 16);
                    operation.Canceled += (s, e) => _status.Text = $"Drag of '{title}' canceled";
                }

                _status.Text = $"Dragging '{title}'";
                args.Handled = true;
            };
            return card;
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

        private void AddInspectorSwatch(StackPanel swatches, string name, Color color)
        {
            var button = CommandButton(name, color, GetReadableTextColor(color));
            button.Click += (sender, args) =>
            {
                _inspectorColor = color;
                UpdateInspectorCode();
                if (_inspectorColorPreview != null)
                    _inspectorColorPreview.BackgroundBrush = _inspectorColor;
                _status.Text = $"Inspector swatch: {name}";
            };
            swatches.AddChild(button);
        }

        private void UpdateInspectorColor(byte? r = null, byte? g = null, byte? b = null)
        {
            _inspectorColor = new Color(r ?? _inspectorColor.R, g ?? _inspectorColor.G, b ?? _inspectorColor.B, _inspectorColor.A);
            if (_inspectorColorPreview != null)
                _inspectorColorPreview.BackgroundBrush = _inspectorColor;
            UpdateInspectorCode();
            _status.Text = $"Inspector color: #{_inspectorColor.R:X2}{_inspectorColor.G:X2}{_inspectorColor.B:X2}";
        }

        private void UpdateInspectorCode()
        {
            if (_inspectorCode != null)
                _inspectorCode.Text = CreateInspectorColorCode();
        }

        private string CreateInspectorColorCode()
        {
            return $"new Color({_inspectorColor.R}, {_inspectorColor.G}, {_inspectorColor.B}, {_inspectorColor.A})";
        }

        private static Color GetReadableTextColor(Color background)
        {
            var luma = background.R * 0.2126 + background.G * 0.7152 + background.B * 0.0722;
            return luma > 150 ? Color.Black : Color.White;
        }

        private StackPanel PanelStack(string title)
        {
            var panel = IsGlassTheme
                ? new GlassStackPanel
                {
                    BorderBrush = new SolidColorBrush(new Color(255, 255, 255, 116)),
                    HighlightBrush = new SolidColorBrush(new Color(255, 255, 255, 150))
                }
                : new StackPanel();

            panel.Orientation = Orientation.Vertical;
            panel.Margin = new Thickness(0, 0, 12, 0);
            panel.Padding = new Thickness(14, 10, 14, 14);
            panel.VerticalAlignment = IsGlassTheme ? VerticalAlignment.Top : VerticalAlignment.Stretch;
            panel.BackgroundBrush = SurfaceBrush;
            panel.Shadow = CurrentTheme?.PanelShadow;
            panel.AddChild(Label(title, Palette.HeadingText, 17, new Thickness(0, 0, 0, 8)));
            return panel;
        }

        private PortableTheme? CurrentTheme => ScreenEngine?.Options.Theme ?? PortableUI.ScreenEngine.Instance?.Options.Theme;

        /// <summary>
        ///     Text color for content on SurfaceAlt: the palette text when it reads well there,
        ///     otherwise plain ink/paper picked by the surface's luminance (fixes e.g. DOS's
        ///     light-gray text on gray dialog buttons).
        /// </summary>
        private Color SurfaceAltTextColor
        {
            get
            {
                var surface = Palette.SurfaceAlt;
                if (ContrastRatio(Palette.Text, surface) >= 3)
                    return Palette.Text;
                var luminance = (0.2126 * surface.R + 0.7152 * surface.G + 0.0722 * surface.B) / 255.0;
                return luminance > 0.5 ? new Color(20, 20, 20) : Color.White;
            }
        }

        private static double ContrastRatio(Color a, Color b)
        {
            static double Luminance(Color color)
            {
                static double Channel(byte value)
                {
                    var c = value / 255.0;
                    return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
                }

                return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
            }

            var la = Luminance(a);
            var lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
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
                Margin = new Thickness(0, 0, 0, 8),
                BackgroundBrush = background,
                TextColor = foreground,
                HoverTextColor = Palette.Text,
                PressedTextColor = Palette.Background
            };
        }

        private Control LabeledSlider(string label, float minimum, float maximum, float value, string format, Action<float> changed)
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical };
            var header = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(),
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };
            var valueLabel = Label(value.ToString(format), Palette.Text);
            header.AddChild(Label(label, Palette.MutedText));
            header.AddChild(valueLabel, column: 1);
            panel.AddChild(header);
            panel.AddChild(ValueSlider(minimum, maximum, value, newValue =>
            {
                valueLabel.Text = newValue.ToString(format);
                changed(newValue);
            }));
            return panel;
        }

        private Slider ValueSlider(float minimum, float maximum, float value, Action<float> changed)
        {
            var slider = new Slider
            {
                Minimum = minimum,
                Maximum = maximum,
                Value = value,
                Margin = new Thickness(0, 4, 0, 10),
                TrackBrush = FieldFrameBrush,
                FillBrush = SelectionBrush,
                ThumbBrush = SurfaceAltBrush,
                ThumbBorderBrush = Palette.Primary
            };
            slider.ValueChanged += (sender, args) => changed(args.NewValue);
            return slider;
        }

        private TextButton MotionButton(string label, Control target, Easing easing)
        {
            var button = CommandButton(label, SurfaceAltBrush, SurfaceAltTextColor);
            button.Click += (sender, args) =>
            {
                target.Translation = Vector2.Zero;
                target.Scale = Vector2.One;
                target.Opacity = 1;
                target.Animate()
                    .TranslateTo(new Vector2(160, 0))
                    .Scale(new Vector2(1.08f, 1.08f))
                    .Duration(TimeSpan.FromMilliseconds(650))
                    .Ease(easing)
                    .Start();
                _status.Text = $"Motion: {label}";
            };
            return button;
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
