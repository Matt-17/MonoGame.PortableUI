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

            var text = (TextBlock?)button.Content;

            Assert.IsNotNull(text);
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
        public void Check_box_toggles_and_raises_checked_once_per_click()
        {
            var checkBox = new CheckBox { Text = "Enable" };
            var calls = 0;
            bool? lastValue = null;
            checkBox.Checked += (sender, args) =>
            {
                calls++;
                lastValue = args.IsChecked;
            };

            checkBox.OnClick();

            Assert.IsTrue(checkBox.IsChecked);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(true, lastValue);
        }

        [TestMethod]
        public void Check_box_uses_visible_default_box_size()
        {
            var checkBox = new CheckBox();

            Assert.AreEqual(20, checkBox.BoxSize);
        }

        [TestMethod]
        public void Check_box_lays_content_to_the_right_of_the_box()
        {
            var content = new FixedSizeControl(new Size(40, 20));
            var checkBox = new CheckBox
            {
                BoxSize = 18,
                BoxSpacing = 6,
                Content = content,
                Width = 120,
                Height = 30
            };

            checkBox.UpdateLayout(new Rect(0, 0, 120, 30));

            Assert.AreEqual(24, content.BoundingRect.Left);
            Assert.AreEqual(96, content.BoundingRect.Width);
        }

        [TestMethod]
        public void Check_box_x_marker_stays_inside_box_and_reaches_edges()
        {
            var box = new Rect(4, 6, 16, 16);
            var markerRects = CheckBox.GetCheckMarkRects(box, 2);
            var count = 0;
            var minLeft = float.MaxValue;
            var minTop = float.MaxValue;
            var maxRight = float.MinValue;
            var maxBottom = float.MinValue;
            var coversCenter = false;

            foreach (var markerRect in markerRects)
            {
                count++;
                minLeft = MathHelper.Min(minLeft, markerRect.Left);
                minTop = MathHelper.Min(minTop, markerRect.Top);
                maxRight = MathHelper.Max(maxRight, markerRect.Right);
                maxBottom = MathHelper.Max(maxBottom, markerRect.Bottom);
                if (markerRect.Contains(new PointF(12, 14)))
                    coversCenter = true;

                Assert.IsTrue(markerRect.Left >= 3.99f);
                Assert.IsTrue(markerRect.Top >= 5.99f);
                Assert.IsTrue(markerRect.Right <= 20.01f);
                Assert.IsTrue(markerRect.Bottom <= 22.01f);
                Assert.IsTrue(markerRect.Width <= 3);
                Assert.IsTrue(markerRect.Height <= 3);
            }

            Assert.IsTrue(count > 0);
            Assert.IsTrue(coversCenter);
            Assert.AreEqual(4, minLeft, 0.01f);
            Assert.AreEqual(6, minTop, 0.01f);
            Assert.AreEqual(20, maxRight, 0.01f);
            Assert.AreEqual(22, maxBottom, 0.01f);
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
        public void Textbox_accepts_key_pressed_events()
        {
            var textBox = new KeyboardBackedTextBox();

            textBox.Press('a');
            textBox.Press('b');
            textBox.Press(KeyboardCommand.CursorLeft);
            textBox.Press('X');
            textBox.Press(KeyboardCommand.Backspace);

            Assert.AreEqual("ab", textBox.Text);
            Assert.AreEqual(1, textBox.CursorPosition);
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

        private sealed class KeyboardBackedTextBox : TextBox
        {
            public void Press(char key)
            {
                OnKeyPressed(key);
            }

            public void Press(KeyboardCommand command)
            {
                OnKeyPressed(command);
            }
        }

        private sealed class FixedSizeControl : Control
        {
            private readonly Size _size;

            public FixedSizeControl(Size size)
            {
                _size = size;
            }

            public override Size MeasureLayout()
            {
                return _size;
            }
        }
    }
}
