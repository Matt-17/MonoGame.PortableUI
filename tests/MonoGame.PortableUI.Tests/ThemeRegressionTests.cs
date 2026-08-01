using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

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
            AssertSolidColor(button.BackgroundBrush, Color.White);
            AssertSolidColor(button.HoverColor, new Color(0, 0, 0, 0.2f));
            AssertSolidColor(button.PressedColor, new Color(0, 0, 0, 0.4f));
            Assert.AreEqual(Color.Black, button.TextColor);
            Assert.AreEqual(Color.Gray, button.DisabledTextColor);

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

            var contextMenu = new ContextMenu();
            AssertSolidColor(contextMenu.BackgroundBrush, Color.Silver);

            var progress = new ProgressIndicator();
            Assert.AreEqual(Color.DarkBlue, progress.Foreground);
            Assert.AreEqual(48, progress.Height);
        }

        [TestMethod]
        public void Custom_theme_from_engine_options_is_used_for_new_controls()
        {
            var theme = PortableTheme.CreateDefault();
            theme.FocusBorderBrush = new SolidColorBrush(new Color(1, 2, 3));
            theme.FocusBorderWidth = 5;
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
            theme.CheckBoxTextColor = new Color(70, 71, 72);
            theme.ToolTipBackgroundBrush = new SolidColorBrush(new Color(73, 74, 75));
            theme.ToolTipBorderBrush = new SolidColorBrush(new Color(76, 77, 78));
            theme.ToolTipBorderWidth = new Thickness(2);
            theme.ToolTipPadding = new Thickness(9, 10, 11, 12);
            theme.ToolTipTextColor = new Color(79, 80, 81);
            theme.ProgressIndicatorForeground = new Color(82, 83, 84);
            theme.ProgressIndicatorHeight = 64;

            using var game = new Game();
            ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, Theme = theme });

            try
            {
                var button = new Button();
                AssertSolidColor(button.FocusBorderBrush, new Color(1, 2, 3));
                Assert.AreEqual(5, button.FocusBorderWidth);
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
                Assert.AreEqual(new Color(70, 71, 72), checkBox.TextColor);

                var toolTip = new ToolTipPopup("Tip");
                AssertSolidColor(toolTip.BackgroundBrush, new Color(73, 74, 75));
                AssertSolidColor(toolTip.BorderColor, new Color(76, 77, 78));
                AssertThickness(new Thickness(2), toolTip.BorderWidth);
                AssertThickness(new Thickness(9, 10, 11, 12), toolTip.Padding);
                Assert.AreEqual(new Color(79, 80, 81), ((TextBlock)toolTip.Content!).TextColor);

                var progress = new ProgressIndicator();
                Assert.AreEqual(new Color(82, 83, 84), progress.Foreground);
                Assert.AreEqual(64, progress.Height);
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
        public void Screen_engine_options_keep_theme_non_null()
        {
            var options = new ScreenEngineOptions { Theme = null! };

            Assert.IsNotNull(options.Theme);
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
    }
}
