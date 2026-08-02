using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;
using System.Linq;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ThemeRegressionTests
    {
        [TestMethod]
        public void Default_theme_matches_existing_control_defaults()
        {
            var button = new Button();
            AssertThickness(new Thickness(8), button.Padding);
            Assert.IsTrue(button.SnapToPixel);
            AssertSolidColor(button.BackgroundBrush, Color.White);
            AssertSolidColor(button.HoverColor, new Color(0, 0, 0, 0.2f));
            AssertSolidColor(button.PressedColor, new Color(0, 0, 0, 0.4f));
            Assert.AreEqual(Color.Black, button.TextColor);
            Assert.AreEqual(Color.Gray, button.DisabledTextColor);
            Assert.AreEqual(FocusVisualKind.Rectangle, button.FocusVisualKind);

            var text = new TextBlock();
            Assert.AreEqual(Color.Black, text.TextColor);
            Assert.AreEqual(14, text.TextSize);

            var textBox = new TextBox();
            AssertSolidColor(textBox.BackgroundBrush, Color.White);
            Assert.AreEqual(Color.Black, textBox.TextColor);
            AssertSolidColor(textBox.CursorColor, Color.Black);
            AssertSolidColor(textBox.SelectionBrush, new Color(51, 153, 255, 95));
            Assert.AreEqual(Color.Silver, textBox.HintTextColor);
            Assert.AreEqual(28, textBox.Height);
            AssertThickness(new Thickness(4), textBox.Padding);

            var scrollViewer = new ScrollViewer();
            Assert.AreEqual(8, scrollViewer.ScrollBarThickness);
            AssertSolidColor(scrollViewer.ScrollBarGutterBrush, new Color(245, 245, 245));
            AssertSolidColor(scrollViewer.ScrollBarBrush, new Color(0, 0, 0, 120));
            AssertSolidColor(scrollViewer.ScrollBarHoverBrush, new Color(0, 0, 0, 160));
            AssertSolidColor(scrollViewer.ScrollBarPressedBrush, new Color(0, 0, 0, 190));

            var tabControl = new TabControl();
            Assert.AreEqual(32, tabControl.HeaderHeight);
            AssertSolidColor(tabControl.HeaderBackground, Color.Silver);
            AssertSolidColor(tabControl.SelectedHeaderBackground, Color.White);
            Assert.AreEqual(Color.Black, tabControl.HeaderTextColor);
            Assert.AreEqual(Color.Black, tabControl.SelectedHeaderTextColor);

            var contextMenu = new ContextMenu();
            AssertSolidColor(contextMenu.BackgroundBrush, Color.Silver);

            var progress = new ProgressIndicator();
            Assert.AreEqual(Color.DarkBlue, progress.Foreground);
            Assert.AreEqual(48, progress.Height);

            var checkBox = new CheckBox();
            AssertSolidColor(checkBox.CheckMarkBrush, new Color(20, 126, 133));
            Assert.AreEqual(CheckBoxGlyphKind.Cross, checkBox.GlyphKind);

            var radioButton = new RadioButton();
            AssertSolidColor(radioButton.DotBrush, new Color(20, 126, 133));
            Assert.AreEqual(8, radioButton.DotSize);

            var slider = new Slider();
            Assert.AreEqual(32, slider.Height);
            Assert.AreEqual(160, slider.Width);
            Assert.AreEqual(4, slider.TrackHeight);
            Assert.AreEqual(18, slider.ThumbSize);
            AssertSolidColor(slider.TrackBrush, new Color(210, 216, 222));
            AssertSolidColor(slider.FillBrush, new Color(20, 126, 133));
            AssertSolidColor(slider.ThumbBrush, Color.White);
            AssertSolidColor(slider.ThumbBorderBrush, new Color(82, 101, 111));

            var progressBar = new ProgressBar();
            Assert.AreEqual(18, progressBar.Height);
            Assert.AreEqual(160, progressBar.Width);
            AssertSolidColor(progressBar.BackgroundBrush, new Color(225, 230, 235));
            AssertSolidColor(progressBar.FillBrush, new Color(20, 126, 133));
        }

        [TestMethod]
        public void Custom_theme_from_engine_options_is_used_for_new_controls()
        {
            var theme = PortableTheme.CreateDefault();
            theme.FocusBorderBrush = new SolidColorBrush(new Color(1, 2, 3));
            theme.PixelSnapping = false;
            theme.FocusBorderWidth = 5;
            theme.FocusVisualKind = FocusVisualKind.Dotted;
            theme.DisabledOverlayBrush = new SolidColorBrush(new Color(4, 5, 6, 120));
            theme.DisabledTextColor = new Color(7, 8, 9);
            theme.ButtonPadding = new Thickness(1, 2, 3, 4);
            theme.ButtonBackgroundBrush = new SolidColorBrush(new Color(10, 11, 12));
            theme.ButtonHoverBrush = new SolidColorBrush(new Color(13, 14, 15));
            theme.ButtonPressedBrush = new SolidColorBrush(new Color(16, 17, 18));
            theme.ButtonTextColor = new Color(19, 20, 21);
            theme.TextColor = new Color(22, 23, 24);
            theme.TextSize = 18;
            theme.TextBoxHeight = 44;
            theme.TextBoxPadding = new Thickness(6);
            theme.TextBoxBackgroundBrush = new SolidColorBrush(new Color(25, 26, 27));
            theme.TextBoxTextColor = new Color(85, 86, 87);
            theme.TextBoxCursorBrush = new SolidColorBrush(new Color(28, 29, 30));
            theme.TextBoxSelectionBrush = new SolidColorBrush(new Color(31, 32, 33));
            theme.TextBoxHintTextColor = new Color(34, 35, 36);
            theme.ScrollBarThickness = 11;
            theme.ScrollBarBrush = new SolidColorBrush(new Color(37, 38, 39));
            theme.TabHeaderHeight = 46;
            theme.TabHeaderBackgroundBrush = new SolidColorBrush(new Color(40, 41, 42));
            theme.TabSelectedHeaderBackgroundBrush = new SolidColorBrush(new Color(43, 44, 45));
            theme.TabHeaderTextColor = new Color(83, 84, 85);
            theme.TabSelectedHeaderTextColor = new Color(86, 87, 88);
            theme.ContextMenuBackgroundBrush = new SolidColorBrush(new Color(46, 47, 48));
            theme.ComboBoxHeight = 52;
            theme.ComboBoxDropDownMaxHeight = 190;
            theme.ListBoxItemHeight = 33;
            theme.ListBoxItemPadding = new Thickness(7, 8);
            theme.ListBoxItemBackgroundBrush = new SolidColorBrush(new Color(49, 50, 51));
            theme.ListBoxSelectedItemBackgroundBrush = new SolidColorBrush(new Color(52, 53, 54));
            theme.ListBoxItemTextColor = new Color(55, 56, 57);
            theme.ListBoxSelectedItemTextColor = new Color(58, 59, 60);
            theme.CheckBoxBoxSize = 23;
            theme.CheckBoxBoxSpacing = 9;
            theme.CheckBoxBoxBorderWidth = 3;
            theme.CheckBoxBoxBackgroundBrush = new SolidColorBrush(new Color(61, 62, 63));
            theme.CheckBoxBoxBorderBrush = new SolidColorBrush(new Color(64, 65, 66));
            theme.CheckBoxCheckMarkBrush = new SolidColorBrush(new Color(67, 68, 69));
            theme.CheckBoxGlyphKind = CheckBoxGlyphKind.Check;
            theme.CheckBoxTextColor = new Color(70, 71, 72);
            theme.RadioButtonDotBrush = new SolidColorBrush(new Color(71, 72, 73));
            theme.RadioButtonDotSize = 11;
            theme.ToolTipBackgroundBrush = new SolidColorBrush(new Color(73, 74, 75));
            theme.ToolTipBorderBrush = new SolidColorBrush(new Color(76, 77, 78));
            theme.ToolTipBorderWidth = new Thickness(2);
            theme.ToolTipPadding = new Thickness(9, 10, 11, 12);
            theme.ToolTipTextColor = new Color(79, 80, 81);
            theme.ProgressIndicatorForeground = new Color(82, 83, 84);
            theme.ProgressIndicatorHeight = 64;
            theme.SliderHeight = 28;
            theme.SliderWidth = 220;
            theme.SliderTrackHeight = 6;
            theme.SliderThumbSize = 16;
            theme.SliderTrackBrush = new SolidColorBrush(new Color(89, 90, 91));
            theme.SliderFillBrush = new SolidColorBrush(new Color(92, 93, 94));
            theme.SliderThumbBrush = new SolidColorBrush(new Color(95, 96, 97));
            theme.SliderThumbBorderBrush = new SolidColorBrush(new Color(98, 99, 100));
            theme.ProgressBarHeight = 24;
            theme.ProgressBarWidth = 180;
            theme.ProgressBarBackgroundBrush = new SolidColorBrush(new Color(101, 102, 103));
            theme.ProgressBarFillBrush = new SolidColorBrush(new Color(104, 105, 106));

            using var game = new Game();
            ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, Theme = theme });

            try
            {
                var button = new Button();
                Assert.IsFalse(button.SnapToPixel);
                AssertSolidColor(button.FocusBorderBrush, new Color(1, 2, 3));
                Assert.AreEqual(5, button.FocusBorderWidth);
                Assert.AreEqual(FocusVisualKind.Dotted, button.FocusVisualKind);
                AssertSolidColor(button.DisabledOverlayBrush, new Color(4, 5, 6, 120));
                Assert.AreEqual(new Color(7, 8, 9), button.DisabledTextColor);
                AssertThickness(new Thickness(1, 2, 3, 4), button.Padding);
                AssertSolidColor(button.BackgroundBrush, new Color(10, 11, 12));
                AssertSolidColor(button.HoverColor, new Color(13, 14, 15));
                AssertSolidColor(button.PressedColor, new Color(16, 17, 18));
                Assert.AreEqual(new Color(19, 20, 21), button.TextColor);

                var textBlock = new TextBlock();
                Assert.AreEqual(new Color(22, 23, 24), textBlock.TextColor);
                Assert.AreEqual(18, textBlock.TextSize);

                var textBox = new TextBox();
                Assert.AreEqual(44, textBox.Height);
                AssertThickness(new Thickness(6), textBox.Padding);
                AssertSolidColor(textBox.BackgroundBrush, new Color(25, 26, 27));
                Assert.AreEqual(new Color(85, 86, 87), textBox.TextColor);
                AssertSolidColor(textBox.CursorColor, new Color(28, 29, 30));
                AssertSolidColor(textBox.SelectionBrush, new Color(31, 32, 33));
                Assert.AreEqual(new Color(34, 35, 36), textBox.HintTextColor);

                var scrollViewer = new ScrollViewer();
                Assert.AreEqual(11, scrollViewer.ScrollBarThickness);
                AssertSolidColor(scrollViewer.ScrollBarBrush, new Color(37, 38, 39));

                var tabControl = new TabControl();
                Assert.AreEqual(46, tabControl.HeaderHeight);
                AssertSolidColor(tabControl.HeaderBackground, new Color(40, 41, 42));
                AssertSolidColor(tabControl.SelectedHeaderBackground, new Color(43, 44, 45));
                Assert.AreEqual(new Color(83, 84, 85), tabControl.HeaderTextColor);
                Assert.AreEqual(new Color(86, 87, 88), tabControl.SelectedHeaderTextColor);

                var contextMenu = new ContextMenu();
                AssertSolidColor(contextMenu.BackgroundBrush, new Color(46, 47, 48));

                var comboBox = new ComboBox();
                Assert.AreEqual(52, comboBox.Height);
                Assert.AreEqual(190, comboBox.DropDownMaxHeight);
                Assert.AreEqual(33, comboBox.ItemHeight);
                AssertSolidColor(comboBox.ItemBackgroundBrush, new Color(49, 50, 51));
                AssertSolidColor(comboBox.SelectedItemBackgroundBrush, new Color(52, 53, 54));
                Assert.AreEqual(new Color(55, 56, 57), comboBox.ItemTextColor);
                Assert.AreEqual(new Color(58, 59, 60), comboBox.SelectedItemTextColor);

                var listBox = new ListBox();
                Assert.AreEqual(33, listBox.ItemHeight);
                AssertThickness(new Thickness(7, 8), listBox.ItemPadding);
                AssertSolidColor(listBox.ItemBackgroundBrush, new Color(49, 50, 51));
                AssertSolidColor(listBox.SelectedItemBackgroundBrush, new Color(52, 53, 54));
                Assert.AreEqual(new Color(55, 56, 57), listBox.ItemTextColor);
                Assert.AreEqual(new Color(58, 59, 60), listBox.SelectedItemTextColor);

                var checkBox = new CheckBox();
                Assert.AreEqual(23, checkBox.BoxSize);
                Assert.AreEqual(9, checkBox.BoxSpacing);
                Assert.AreEqual(3, checkBox.BoxBorderWidth);
                AssertSolidColor(checkBox.BoxBackgroundBrush, new Color(61, 62, 63));
                AssertSolidColor(checkBox.BoxBorderBrush, new Color(64, 65, 66));
                AssertSolidColor(checkBox.CheckMarkBrush, new Color(67, 68, 69));
                Assert.AreEqual(CheckBoxGlyphKind.Check, checkBox.GlyphKind);
                Assert.AreEqual(new Color(70, 71, 72), checkBox.TextColor);

                var radioButton = new RadioButton();
                AssertSolidColor(radioButton.DotBrush, new Color(71, 72, 73));
                Assert.AreEqual(11, radioButton.DotSize);

                var toolTip = new ToolTipPopup("Tip");
                AssertSolidColor(toolTip.BackgroundBrush, new Color(73, 74, 75));
                AssertSolidColor(toolTip.BorderColor, new Color(76, 77, 78));
                AssertThickness(new Thickness(2), toolTip.BorderWidth);
                AssertThickness(new Thickness(9, 10, 11, 12), toolTip.Padding);
                Assert.AreEqual(new Color(79, 80, 81), ((TextBlock)toolTip.Content!).TextColor);

                var progress = new ProgressIndicator();
                Assert.AreEqual(new Color(82, 83, 84), progress.Foreground);
                Assert.AreEqual(64, progress.Height);

                var slider = new Slider();
                Assert.AreEqual(28, slider.Height);
                Assert.AreEqual(220, slider.Width);
                Assert.AreEqual(6, slider.TrackHeight);
                Assert.AreEqual(16, slider.ThumbSize);
                AssertSolidColor(slider.TrackBrush, new Color(89, 90, 91));
                AssertSolidColor(slider.FillBrush, new Color(92, 93, 94));
                AssertSolidColor(slider.ThumbBrush, new Color(95, 96, 97));
                AssertSolidColor(slider.ThumbBorderBrush, new Color(98, 99, 100));

                var progressBar = new ProgressBar();
                Assert.AreEqual(24, progressBar.Height);
                Assert.AreEqual(180, progressBar.Width);
                AssertSolidColor(progressBar.BackgroundBrush, new Color(101, 102, 103));
                AssertSolidColor(progressBar.FillBrush, new Color(104, 105, 106));
            }
            finally
            {
                ScreenEngine.Instance!.Options.Theme = PortableTheme.CreateDefault();
            }
        }

        [TestMethod]
        public void Object_initializer_values_override_theme_defaults()
        {
            var theme = PortableTheme.CreateDefault();
            theme.ButtonTextColor = Color.Green;
            theme.ButtonPadding = new Thickness(2);
            theme.TextBoxHeight = 20;

            using var game = new Game();
            ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, Theme = theme });

            try
            {
                var button = new Button
                {
                    Padding = new Thickness(9),
                    TextColor = Color.Red,
                    Text = "Run"
                };

                AssertThickness(new Thickness(9), button.Padding);
                Assert.AreEqual(Color.Red, button.TextColor);
                Assert.AreEqual(Color.Red, ((TextBlock)button.Content!).TextColor);

                var textBox = new TextBox
                {
                    Height = 99,
                    Padding = new Thickness(3)
                };

                Assert.AreEqual(99, textBox.Height);
                AssertThickness(new Thickness(3), textBox.Padding);
            }
            finally
            {
                ScreenEngine.Instance!.Options.Theme = PortableTheme.CreateDefault();
            }
        }

        [TestMethod]
        public void Button_refreshes_disabled_text_when_enabled_changes()
        {
            var button = new Button
            {
                Text = "Run",
                TextColor = Color.Black,
                DisabledTextColor = Color.Gray
            };

            button.IsEnabled = false;

            Assert.AreEqual(Color.Gray, ((TextBlock)button.Content!).TextColor);

            button.IsEnabled = true;

            Assert.AreEqual(Color.Black, ((TextBlock)button.Content!).TextColor);
        }

        [TestMethod]
        public void Tab_control_applies_selected_and_unselected_header_text_colors()
        {
            var tabControl = new TabControl
            {
                HeaderTextColor = new Color(10, 20, 30),
                SelectedHeaderTextColor = new Color(40, 50, 60)
            };
            tabControl.Items.Add(new TabItem { Header = "One" });
            tabControl.Items.Add(new TabItem { Header = "Two" });
            tabControl.SelectedIndex = 1;

            tabControl.UpdateLayout(new Rect(0, 0, 200, 100));

            var headers = tabControl.GetDescendants().OfType<Button>().Take(2).ToArray();
            Assert.AreEqual(new Color(10, 20, 30), headers[0].TextColor);
            Assert.AreEqual(new Color(40, 50, 60), headers[1].TextColor);
            Assert.AreEqual(new Color(40, 50, 60), ((TextBlock)headers[1].Content!).TextColor);
        }

        [TestMethod]
        public void Screen_engine_options_keep_theme_non_null()
        {
            var options = new ScreenEngineOptions { Theme = null! };

            Assert.IsNotNull(options.Theme);
        }

        [TestMethod]
        public void Border_legacy_properties_map_to_common_chrome_properties()
        {
            var border = new Border
            {
                BorderColor = new SolidColorBrush(Color.Red),
                BorderWidth = new Thickness(1, 2, 3, 4)
            };

            AssertSolidColor(border.BorderBrush, Color.Red);
            AssertThickness(new Thickness(1, 2, 3, 4), border.BorderThickness);
            Assert.AreSame(border.BorderBrush, border.BorderColor);
            AssertThickness(border.BorderThickness, border.BorderWidth);
        }

        [TestMethod]
        public void Chrome_controls_expose_common_border_and_corner_radius_properties()
        {
            var button = new Button
            {
                BorderBrush = new SolidColorBrush(Color.Blue),
                BorderThickness = 2,
                CornerRadius = new CornerRadius(2, 4, 6, 8)
            };

            AssertSolidColor(button.BorderBrush, Color.Blue);
            AssertThickness(new Thickness(2), button.BorderThickness);
            Assert.AreEqual(new CornerRadius(2, 4, 6, 8), button.CornerRadius);
        }

        [TestMethod]
        public void Theme_registry_exposes_unique_library_themes()
        {
            var ids = ThemeRegistry.Themes.Select(theme => theme.Id).ToArray();

            Assert.AreEqual(ids.Length, ids.Distinct().Count());
            CollectionAssert.Contains(ids, ThemeRegistry.DefaultThemeId);
            Assert.AreSame(ThemeRegistry.Default, ThemeRegistry.Resolve(null));
            Assert.AreSame(ThemeRegistry.Resolve("dos"), ThemeRegistry.Resolve("DOS"));
        }

        [TestMethod]
        public void Theme_registry_themes_have_metadata_and_create_portable_themes()
        {
            foreach (var definition in ThemeRegistry.Themes)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.Id));
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.DisplayName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.FontName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.Metadata.Description));
                Assert.IsNotNull(definition.Palette);
                Assert.IsNotNull(definition.CreateTheme());
                Assert.IsTrue(definition.Metadata.PreviewSwatches.Count >= 3);
            }
        }

        [TestMethod]
        public void Theme_registry_contains_full_wave_catalog()
        {
            var ids = ThemeRegistry.Themes.Select(theme => theme.Id).ToArray();

            Assert.IsTrue(ids.Length >= 35);
            CollectionAssert.Contains(ids, "win95");
            CollectionAssert.Contains(ids, "metro");
            CollectionAssert.Contains(ids, "fluent");
            CollectionAssert.Contains(ids, "material");
            CollectionAssert.Contains(ids, "liquid");
            CollectionAssert.Contains(ids, "solarized-light");
            CollectionAssert.Contains(ids, "solarized-dark");
            CollectionAssert.Contains(ids, "brutalist");
        }

        [TestMethod]
        public void Palette_style_builder_maps_control_styles_with_state_fallbacks()
        {
            var palette = ThemeRegistry.Resolve("dos").Palette;
            var styles = ControlStyleBuilder.FromPalette(palette);

            Assert.IsTrue(styles.Count >= 10);
            Assert.IsTrue(styles.ContainsKey("Button"));

            var button = styles["Button"];
            Assert.IsNotNull(button.Normal.Background);
            Assert.IsNotNull(button.Normal.BorderBrush);
            Assert.AreEqual(palette.Text, button.Normal.TextColor);
            // Hover no longer replaces text/background — interactive controls composite their
            // own hover overlays; unset state values fall back to Normal.
            Assert.AreEqual(palette.Text, button.Hover.Resolve(button.Normal).TextColor);
            Assert.AreSame(button.Normal.Background, button.Hover.Resolve(button.Normal).Background);
            Assert.AreSame(button.Normal.BorderBrush, button.Focused.Resolve(button.Normal).BorderBrush);
        }

        [TestMethod]
        public void Theme_post_effect_chain_accepts_terminal_effect_descriptors()
        {
            var theme = PortableTheme.CreateDefault();
            theme.PostEffects = new PostEffect[]
            {
                new ScanlinePostEffect { Strength = 0.12f },
                new CrtBarrelPostEffect { Distortion = 0.06f },
                new BloomPostEffect { Strength = 0.2f }
            };

            Assert.AreEqual(3, theme.PostEffects.Count);
            Assert.AreEqual("scanlines", theme.PostEffects[0].Name);
            Assert.IsTrue(theme.PostEffects[1].Enabled);
            Assert.AreEqual("bloom", theme.PostEffects[2].Name);
        }

        [TestMethod]
        public void Theme_island_resolution_prefers_nearest_island_then_global_theme()
        {
            using var game = new Game();
            var global = ThemeRegistry.Resolve("c64").CreateTheme();
            var outerTheme = ThemeRegistry.Resolve("dos").CreateTheme();
            var innerTheme = ThemeRegistry.Resolve("studio").CreateTheme();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, Theme = global });
            var screen = new EmptyScreen();
            var outer = new ThemeIsland { Theme = outerTheme };
            var inner = new ThemeIsland { Theme = innerTheme };
            var outerProbe = new ThemeProbe();
            var innerProbe = new ThemeProbe();

            inner.Content = innerProbe;
            outer.Content = new StackPanel
            {
                Children =
                {
                    outerProbe,
                    inner
                }
            };
            screen.Content = outer;
            engine.NavigateToScreen(screen);

            Assert.AreSame(outerTheme, outerProbe.CurrentTheme);
            Assert.AreSame(innerTheme, innerProbe.CurrentTheme);

            inner.Theme = null;

            Assert.AreSame(outerTheme, innerProbe.CurrentTheme);

            outer.Theme = null;

            Assert.AreSame(global, innerProbe.CurrentTheme);
        }

        [TestMethod]
        public void Theme_owner_resolution_lets_overlay_content_inherit_opener_theme()
        {
            var ownerTheme = ThemeRegistry.Resolve("terminal").CreateTheme();
            var owner = new ThemeIsland { Theme = ownerTheme };
            var opener = new ThemeProbe();
            var overlay = new ThemeProbe();

            owner.Content = opener;
            overlay.ThemeOwner = opener;

            Assert.AreSame(ownerTheme, overlay.CurrentTheme);
        }

        private static void AssertSolidColor(Brush? brush, Color expected)
        {
            Assert.IsInstanceOfType(brush, typeof(SolidColorBrush));
            Assert.AreEqual(expected, ((SolidColorBrush)brush!).Color);
        }

        private static void AssertThickness(Thickness expected, Thickness actual)
        {
            Assert.AreEqual(expected.Left, actual.Left);
            Assert.AreEqual(expected.Top, actual.Top);
            Assert.AreEqual(expected.Right, actual.Right);
            Assert.AreEqual(expected.Bottom, actual.Bottom);
        }

        private sealed class EmptyScreen : Screen
        {
        }

        private sealed class ThemeProbe : Control
        {
            public PortableTheme CurrentTheme => ResolveTheme();
        }
    }
}
