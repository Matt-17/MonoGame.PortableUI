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
        public void Textbox_reuses_prefix_measurements_for_cursor_rect()
        {
            var measurer = new CountingCharacterWidthMeasurer(10, 16);
            var textBox = new TextBox
            {
                TextMeasurer = measurer,
                Text = "abcdefgh",
                Width = 200,
                Height = 30
            };
            textBox.CursorPosition = textBox.Text.Length;
            textBox.UpdateLayout(new Rect(0, 0, 200, 30));
            var textRect = textBox.ClippingRect - textBox.Padding;
            measurer.MeasurementCount = 0;

            textBox.GetCursorRect(textRect);
            textBox.GetCursorRect(textRect);

            // Prefix widths and the line height are cached with the line metrics: no re-measuring.
            Assert.AreEqual(0, measurer.MeasurementCount);
        }


        [TestMethod]
        public void List_box_refresh_picks_up_in_place_item_edits()
        {
            var item = new MutableItem { Name = "before" };
            var listBox = new ListBox();
            listBox.Items.Add(item);
            listBox.UpdateLayout(new Rect(0, 0, 200, 200));

            item.Name = "after";
            listBox.UpdateLayout(new Rect(0, 0, 200, 200));
            Assert.AreEqual("before", listBox.ItemButtons[0].Text, "unchanged items are not re-stringified per pass");

            listBox.Refresh();
            Assert.AreEqual("after", listBox.ItemButtons[0].Text);
        }

        private sealed class MutableItem
        {
            public string Name { get; set; } = "";

            public override string ToString() => Name;
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
        public void Textbox_enforces_max_length_for_text_typing_and_paste()
        {
            using var game = new Game();
            var clipboard = new FakeClipboardService { Text = "cdef" };
            ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, ClipboardService = clipboard });
            var textBox = new KeyboardBackedTextBox { MaxLength = 3 };

            textBox.Text = "abcdef";

            Assert.AreEqual("abc", textBox.Text);

            textBox.CursorPosition = textBox.Text.Length;
            textBox.Press('Z');

            Assert.AreEqual("abc", textBox.Text);

            textBox.MaxLength = 5;
            textBox.Press(KeyboardCommand.Paste);

            Assert.AreEqual("abccd", textBox.Text);
            Assert.AreEqual(5, textBox.CursorPosition);
        }

        [TestMethod]
        public void Textbox_read_only_blocks_mutations_but_allows_selection_copy_and_navigation()
        {
            using var game = new Game();
            var clipboard = new FakeClipboardService { Text = "paste" };
            ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, ClipboardService = clipboard });
            var textBox = new KeyboardBackedTextBox { Text = "abcd", IsReadOnly = true };
            textBox.Select(1, 2);

            textBox.Copy();
            textBox.Cut();
            textBox.Press(KeyboardCommand.Backspace);
            textBox.Press(KeyboardCommand.Delete);
            textBox.Press(KeyboardCommand.Paste);
            textBox.Press(KeyboardCommand.CursorLeft);

            Assert.AreEqual("bc", clipboard.Text);
            Assert.AreEqual("abcd", textBox.Text);
            // Left without Shift collapses the selection to its start, as in standard editors.
            Assert.AreEqual(1, textBox.CursorPosition);
            Assert.AreEqual(0, textBox.SelectionLength);
        }

        [TestMethod]
        public void Textbox_treats_a_surrogate_pair_as_one_character()
        {
            var textBox = new KeyboardBackedTextBox { Text = "a😀b" };
            textBox.Select(3, 0);

            textBox.Press(KeyboardCommand.CursorLeft);
            Assert.AreEqual(1, textBox.CursorPosition);

            textBox.Press(KeyboardCommand.Delete);
            Assert.AreEqual("ab", textBox.Text);
        }

        [TestMethod]
        public void Textbox_right_arrow_collapses_selection_to_its_end()
        {
            var textBox = new KeyboardBackedTextBox { Text = "abcd" };
            textBox.Select(1, 2);

            textBox.Press(KeyboardCommand.CursorRight);

            Assert.AreEqual(3, textBox.CursorPosition);
            Assert.AreEqual(0, textBox.SelectionLength);
        }

        [TestMethod]
        public void Slider_range_does_not_depend_on_assignment_order_and_ignores_nan()
        {
            var slider = new Slider { Maximum = -10, Minimum = -50 };

            Assert.AreEqual(-50, slider.Minimum);
            Assert.AreEqual(-10, slider.Maximum);

            slider.Value = -20;
            slider.Value = float.NaN;
            Assert.AreEqual(-20, slider.Value);

            var progress = new ProgressBar { Maximum = -10, Minimum = -50 };
            Assert.AreEqual(-10, progress.Maximum);
        }

        [TestMethod]
        public void Selected_index_set_before_items_is_applied_once_items_arrive()
        {
            var listBox = new ListBox { SelectedIndex = 2 };
            listBox.Items.AddRange(new object[] { "a", "b", "c" });
            listBox.UpdateLayout(new Rect(0, 0, 200, 200));
            Assert.AreEqual(2, listBox.SelectedIndex);

            var comboBox = new ComboBox { SelectedIndex = 1 };
            comboBox.Items.AddRange(new object[] { "a", "b" });
            comboBox.MeasureLayout();
            Assert.AreEqual(1, comboBox.SelectedIndex);
            Assert.AreEqual("b", comboBox.Text);

            var tabs = new TabControl { SelectedIndex = 1 };
            tabs.Items.Add(new TabItem { Header = "A", Content = new Border() });
            tabs.Items.Add(new TabItem { Header = "B", Content = new Border() });
            tabs.UpdateLayout(new Rect(0, 0, 200, 200));
            Assert.AreEqual(1, tabs.SelectedIndex);
        }

        [TestMethod]
        public void Textbox_ignores_commands_it_has_no_use_for()
        {
            var textBox = new KeyboardBackedTextBox { Text = "abc" };

            textBox.Press(KeyboardCommand.PageDown);
            textBox.Press(KeyboardCommand.Escape);

            Assert.AreEqual("abc", textBox.Text);
        }

        [TestMethod]
        public void Textbox_undo_reverts_typed_words_as_one_step_and_redo_reapplies()
        {
            var textBox = new KeyboardBackedTextBox();
            foreach (var c in "hello")
                textBox.Press(c);
            textBox.Press(' ');
            foreach (var c in "world")
                textBox.Press(c);

            textBox.Press(KeyboardCommand.Undo);
            Assert.AreEqual("hello ", textBox.Text);
            textBox.Press(KeyboardCommand.Undo);
            Assert.AreEqual("hello", textBox.Text);
            textBox.Press(KeyboardCommand.Undo);
            Assert.AreEqual("", textBox.Text);
            Assert.IsFalse(textBox.CanUndo);

            textBox.Press(KeyboardCommand.Redo);
            textBox.Press(KeyboardCommand.Redo);
            Assert.AreEqual("hello ", textBox.Text);
            Assert.AreEqual(6, textBox.CursorPosition);
        }

        [TestMethod]
        public void Textbox_undo_restores_deleted_selection_and_code_assignment_clears_history()
        {
            var textBox = new KeyboardBackedTextBox { Text = "keep this" };
            textBox.Select(4, 5);
            textBox.Press(KeyboardCommand.Backspace);
            Assert.AreEqual("keep", textBox.Text);

            textBox.Press(KeyboardCommand.Undo);
            Assert.AreEqual("keep this", textBox.Text);

            textBox.Text = "reset";
            Assert.IsFalse(textBox.CanUndo);
        }

        [TestMethod]
        public void Textbox_ctrl_arrows_jump_by_word_and_ctrl_backspace_deletes_a_word()
        {
            var textBox = new KeyboardBackedTextBox { Text = "one two, three" };
            textBox.Select(textBox.Text.Length, 0);

            textBox.Press(KeyboardCommand.CursorLeft, KeyboardModifiers.Control);
            Assert.AreEqual(9, textBox.CursorPosition);
            textBox.Press(KeyboardCommand.CursorLeft, KeyboardModifiers.Control);
            Assert.AreEqual(7, textBox.CursorPosition);
            textBox.Press(KeyboardCommand.CursorLeft, KeyboardModifiers.Control);
            Assert.AreEqual(4, textBox.CursorPosition);

            textBox.Press(KeyboardCommand.CursorRight, KeyboardModifiers.Control);
            Assert.AreEqual(7, textBox.CursorPosition);

            textBox.Select(textBox.Text.Length, 0);
            textBox.Press(KeyboardCommand.Backspace, KeyboardModifiers.Control);
            Assert.AreEqual("one two, ", textBox.Text);
        }

        [TestMethod]
        public void Multiline_textbox_wraps_long_lines_at_word_boundaries()
        {
            var textBox = new TextBox
            {
                TextMeasurer = new CountingCharacterWidthMeasurer(10, 16),
                IsMultiline = true,
                TextWrapping = TextWrapping.Wrap,
                Width = 100,
                Height = float.NaN,
                Padding = new Thickness(0),
                Text = "hello world again"
            };

            // 10 px per character, 10 characters per line: "hello " | "world " | "again".
            Assert.AreEqual(3 * 16, textBox.MeasureLayout().Height, 0.001f);
        }

        [TestMethod]
        public void Textbox_replaces_selection_and_deletes_selection_first()
        {
            var textBox = new KeyboardBackedTextBox { Text = "abcde" };

            textBox.Select(1, 3);
            textBox.Press('X');

            Assert.AreEqual("aXe", textBox.Text);
            Assert.AreEqual(2, textBox.CursorPosition);

            textBox.Text = "abcde";
            textBox.Select(1, 3);
            textBox.Press(KeyboardCommand.Backspace);

            Assert.AreEqual("ae", textBox.Text);
            Assert.AreEqual(1, textBox.CursorPosition);

            textBox.Text = "abcde";
            textBox.Select(1, 3);
            textBox.Press(KeyboardCommand.Delete);

            Assert.AreEqual("ae", textBox.Text);
            Assert.AreEqual(1, textBox.CursorPosition);
        }

        [TestMethod]
        public void Textbox_select_all_copy_cut_and_paste_use_configured_clipboard()
        {
            using var game = new Game();
            var clipboard = new FakeClipboardService { Text = "ZZ" };
            ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, ClipboardService = clipboard });
            var textBox = new KeyboardBackedTextBox { Text = "abcdef" };

            textBox.Select(1, 3);
            textBox.Copy();

            Assert.AreEqual("bcd", clipboard.Text);
            Assert.AreEqual("abcdef", textBox.Text);

            textBox.Cut();

            Assert.AreEqual("bcd", clipboard.Text);
            Assert.AreEqual("aef", textBox.Text);
            Assert.AreEqual(1, textBox.CursorPosition);

            clipboard.Text = "ZZ";
            textBox.Press(KeyboardCommand.Paste);

            Assert.AreEqual("aZZef", textBox.Text);

            textBox.Press(KeyboardCommand.SelectAll);
            textBox.Press('Q');

            Assert.AreEqual("Q", textBox.Text);
        }

        [TestMethod]
        public void Textbox_password_char_masks_clipboard_copy_and_cut_but_allows_paste()
        {
            using var game = new Game();
            var clipboard = new FakeClipboardService { Text = "old" };
            ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, ClipboardService = clipboard });
            var textBox = new KeyboardBackedTextBox { Text = "secret", PasswordChar = '*' };

            textBox.SelectAll();
            textBox.Copy();
            textBox.Cut();

            Assert.AreEqual("old", clipboard.Text);
            Assert.AreEqual("secret", textBox.Text);

            textBox.CursorPosition = textBox.Text.Length;
            clipboard.Text = "!";
            textBox.Press(KeyboardCommand.Paste);

            Assert.AreEqual("secret!", textBox.Text);
        }

        [TestMethod]
        public void Textbox_multiline_enter_adds_newline_and_control_enter_raises_event()
        {
            var textBox = new KeyboardBackedTextBox { IsMultiline = true };
            var enterPressed = 0;
            textBox.EnterPressed += (sender, args) => enterPressed++;

            textBox.Press('a');
            textBox.Press(KeyboardCommand.Enter);
            textBox.Press('b');
            textBox.Press(KeyboardCommand.Enter, KeyboardModifiers.Control);

            Assert.AreEqual("a\nb", textBox.Text);
            Assert.AreEqual(1, enterPressed);
        }

        [TestMethod]
        public void Textbox_multiline_navigation_preserves_visual_column()
        {
            var textBox = new KeyboardBackedTextBox
            {
                IsMultiline = true,
                Text = "abc\nde\nwxyz",
                TextMeasurer = new CharacterWidthMeasurer(10, 16)
            };
            textBox.CursorPosition = 9;

            textBox.Press(KeyboardCommand.CursorUp);

            Assert.AreEqual(6, textBox.CursorPosition);

            textBox.Press(KeyboardCommand.CursorUp);

            Assert.AreEqual(2, textBox.CursorPosition);

            textBox.Press(KeyboardCommand.CursorDown);

            Assert.AreEqual(6, textBox.CursorPosition);
        }

        [TestMethod]
        public void Textbox_home_and_end_are_line_aware_in_multiline_mode()
        {
            var textBox = new KeyboardBackedTextBox
            {
                IsMultiline = true,
                Text = "abc\ndef"
            };
            textBox.CursorPosition = 5;

            textBox.Press(KeyboardCommand.Home);
            Assert.AreEqual(4, textBox.CursorPosition);

            textBox.Press(KeyboardCommand.End);
            Assert.AreEqual(7, textBox.CursorPosition);

            textBox.Press(KeyboardCommand.Home, KeyboardModifiers.Control);
            Assert.AreEqual(0, textBox.CursorPosition);

            textBox.Press(KeyboardCommand.End, KeyboardModifiers.Control);
            Assert.AreEqual(7, textBox.CursorPosition);
        }

        [TestMethod]
        public void Textbox_multiline_click_uses_x_and_y_position()
        {
            var textBox = new TextBox
            {
                IsMultiline = true,
                Text = "aa\nbbbb",
                TextMeasurer = new CharacterWidthMeasurer(10, 16),
                Width = 200,
                Height = 60
            };
            textBox.UpdateLayout(new Rect(0, 0, 200, 60));

            textBox.OnMouseUp(new MouseEventArgs(new PointF(25, 24), MouseButton.Left));

            Assert.AreEqual(5, textBox.CursorPosition);
        }

        [TestMethod]
        public void Textbox_single_line_scrolls_horizontally_to_keep_cursor_visible()
        {
            var textBox = new KeyboardBackedTextBox
            {
                TextMeasurer = new CharacterWidthMeasurer(10, 16),
                Width = 60,
                Height = 30
            };
            textBox.UpdateLayout(new Rect(0, 0, 60, 30));

            foreach (var character in "abcdefgh")
                textBox.Press(character);

            Assert.IsTrue(textBox.HorizontalScrollOffset > 0);

            textBox.Press(KeyboardCommand.Home);

            Assert.AreEqual(0, textBox.HorizontalScrollOffset);
        }

        [TestMethod]
        public void Textbox_multiline_scrolls_vertically_to_keep_cursor_visible()
        {
            var textBox = new KeyboardBackedTextBox
            {
                IsMultiline = true,
                Text = "a\nb\nc\nd",
                TextMeasurer = new CharacterWidthMeasurer(10, 16),
                Width = 120,
                Height = 40
            };
            textBox.CursorPosition = textBox.Text.Length;
            textBox.UpdateLayout(new Rect(0, 0, 120, 40));

            Assert.AreEqual(32, textBox.VerticalScrollOffset);

            textBox.Press(KeyboardCommand.Home, KeyboardModifiers.Control);

            Assert.AreEqual(0, textBox.VerticalScrollOffset);
        }

        [TestMethod]
        public void Textbox_multiline_cursor_rect_uses_vertical_scroll_offset()
        {
            var textBox = new KeyboardBackedTextBox
            {
                IsMultiline = true,
                Text = "a\nb\nc\nd",
                TextMeasurer = new CharacterWidthMeasurer(10, 16),
                Width = 120,
                Height = 40
            };
            textBox.CursorPosition = textBox.Text.Length;
            textBox.UpdateLayout(new Rect(0, 0, 120, 40));

            var textRect = textBox.ClippingRect - textBox.Padding;
            var cursorRect = textBox.GetCursorRect(textRect);

            Assert.IsTrue(cursorRect.Top >= textRect.Top);
            Assert.IsTrue(cursorRect.Bottom <= textRect.Bottom);
        }

        [TestMethod]
        public void Textbox_multiline_click_accounts_for_vertical_scroll_offset()
        {
            var textBox = new TextBox
            {
                IsMultiline = true,
                Text = "a\nb\nc\nd",
                TextMeasurer = new CharacterWidthMeasurer(10, 16),
                Width = 120,
                Height = 40
            };
            textBox.CursorPosition = textBox.Text.Length;
            textBox.UpdateLayout(new Rect(0, 0, 120, 40));

            textBox.OnMouseUp(new MouseEventArgs(new PointF(4, 4), MouseButton.Left));

            Assert.AreEqual(4, textBox.CursorPosition);
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

        private sealed class CountingCharacterWidthMeasurer : ITextMeasurer
        {
            private readonly float _characterWidth;
            private readonly float _height;

            public CountingCharacterWidthMeasurer(float characterWidth, float height)
            {
                _characterWidth = characterWidth;
                _height = height;
            }

            public int MeasurementCount { get; set; }

            public Vector2 MeasureString(string text)
            {
                MeasurementCount++;
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

            public void Press(KeyboardCommand command, KeyboardModifiers modifiers)
            {
                OnKeyPressed(command, modifiers);
            }
        }

        private sealed class FakeClipboardService : IClipboardService
        {
            public string? Text { get; set; }

            public string? GetText()
            {
                return Text;
            }

            public void SetText(string? text)
            {
                Text = text;
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
