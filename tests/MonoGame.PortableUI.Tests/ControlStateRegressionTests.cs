using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ControlStateRegressionTests
    {
        [TestMethod]
        public void Button_applies_initial_text_color_to_created_text_content()
        {
            var button = new Button { TextColor = Color.Red, Text = "Run" };

            var text = (TextBlock)button.Content;

            Assert.AreEqual(Color.Red, text.TextColor);
        }

        [TestMethod]
        public void Toggle_button_checked_event_fires_once_per_click()
        {
            var button = new ToggleButton { Text = "Toggle" };
            var calls = 0;
            button.Checked += (sender, args) => calls++;

            button.OnClick();

            Assert.IsTrue(button.IsChecked);
            Assert.AreEqual(1, calls);
        }

        [TestMethod]
        public void Focus_is_cleared_when_control_is_hidden_disabled_or_removed()
        {
            var panel = new StackPanel();
            var first = new Button { Text = "First" };
            var second = new Button { Text = "Second" };
            panel.AddChild(first);
            panel.AddChild(second);

            first.Focus();
            Assert.AreSame(first, ScreenEngine.FocusedControl);

            first.IsVisible = false;
            Assert.IsNull(ScreenEngine.FocusedControl);

            second.Focus();
            second.IsEnabled = false;
            Assert.IsNull(ScreenEngine.FocusedControl);

            second.IsEnabled = true;
            second.Focus();
            panel.Children.Remove(second);
            Assert.IsNull(ScreenEngine.FocusedControl);
        }

        [TestMethod]
        public void Textbox_sets_cursor_from_click_position()
        {
            var textBox = new TextBox
            {
                Text = "abcd",
                TextMeasurer = new CharacterWidthMeasurer(10, 16),
                Width = 200,
                Height = 30
            };
            textBox.UpdateLayout(new Rect(0, 0, 200, 30));

            textBox.OnMouseUp(new MouseEventArgs(new PointF(25, 10), MouseButton.Left));

            Assert.AreEqual(2, textBox.CursorPosition);
        }

        [TestMethod]
        public void Textblock_without_font_uses_text_measurer()
        {
            var text = new TextBlock
            {
                Text = "Fallback",
                TextMeasurer = new CharacterWidthMeasurer(8, 14)
            };

            var size = text.MeasureLayout();

            Assert.AreEqual(64, size.Width);
            Assert.AreEqual(14, size.Height);
        }

        private sealed class CharacterWidthMeasurer : ITextMeasurer
        {
            private readonly float _characterWidth;
            private readonly float _height;

            public CharacterWidthMeasurer(float characterWidth, float height)
            {
                _characterWidth = characterWidth;
                _height = height;
            }

            public Vector2 MeasureString(string text)
            {
                return new Vector2((text ?? "").Length * _characterWidth, _height);
            }
        }
    }
}
