using System;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class TouchTextSelectionTests
    {
        private sealed class TestScreen : Screen
        {
        }

        /// <summary>Every character 10 units wide, lines 20 high: positions are easy to aim at.</summary>
        private sealed class MonoFont : UIFont
        {
            public override float DefaultSize => 20;
            public override float GetLineHeight(float pixelSize) => 20;
            public override Vector2 MeasureString(string text, float pixelSize) => new Vector2(text.Length * 10, 20);
            public override Vector2 MeasureString(StringBuilder text, float pixelSize) => new Vector2(text.Length * 10, 20);
            public override void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float pixelSize, Vector2 scale) { }
            public override void DrawString(SpriteBatch spriteBatch, StringBuilder text, Vector2 position, Color color, float pixelSize, Vector2 scale) { }
        }

        private sealed class MemoryClipboard : IClipboardService
        {
            public string? Text;
            public string? GetText() => Text;
            public void SetText(string? text) => Text = text;
        }

        private sealed class RecordingMenu : ITextSelectionMenu
        {
            public TextSelectionMenuRequest? Shown;
            public int Shows, Hides, Suspends;
            public void Show(TextSelectionMenuRequest request) { Shown = request; Shows++; }
            public void Hide() { Shown = null; Hides++; }
            public void Suspend() => Suspends++;
        }

        private sealed class Bar : Adorner
        {
            public int Outside;
            public int Downs;

            public Bar()
            {
                Content = new Border { Width = 100, Height = 40, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                TouchDown += (_, _) => Downs++;
            }

            protected internal override void OnPressOutside(PointF point) => Outside++;
        }

        private Game _game = null!;
        private ScreenEngine _engine = null!;
        private TestScreen _screen = null!;
        private VirtualInputSource _input = null!;
        private TextBox _box = null!;
        private Button _below = null!;
        private MemoryClipboard _clipboard = null!;
        private int _belowClicks;

        // The text starts at x = 10 (padding); "hello world foo": hello = 0..5, world = 6..11, foo = 12..15.
        private const float TextLeft = 10;
        private const float LineY = 30;

        private System.Globalization.CultureInfo _culture = null!;

        [TestInitialize]
        public void SetUp()
        {
            // The toolbar labels follow the UI language; the tests read the English ones.
            _culture = System.Globalization.CultureInfo.CurrentUICulture;
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(100);
            _game = new Game();
            _clipboard = new MemoryClipboard();
            // The toolbar labels measure with the same 10-per-character font.
            FontManager.RegisterDynamicFont("touch-selection-test", new MonoFont());
            var theme = PortableTheme.CreateDefault();
            theme.Typography.FontName = "touch-selection-test";
            // White button text (a filled theme) must not reach the white selection bar.
            theme.ButtonTextColor = Color.White;
            theme.ButtonPressedTextColor = Color.White;
            _engine = ScreenEngine.Initialize(_game, new ScreenEngineOptions { AddComponentToGame = false, ClipboardService = _clipboard, Theme = theme });
            _engine.SetScreenSize(400, 600);
            _box = new TextBox
            {
                Text = "hello world foo",
                DynamicFont = new MonoFont(),
                Width = 300,
                Height = 40,
                Padding = new Thickness(10, 10),
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            _below = new Button { Text = "below", Width = 200, Height = 60, HorizontalAlignment = HorizontalAlignment.Left };
            _belowClicks = 0;
            _below.Click += (_, _) => _belowClicks++;
            var panel = new StackPanel { Children = { _box, new Border { Height = 200 }, _below } };
            _input = new VirtualInputSource();
            _screen = new TestScreen { Content = panel, InputSource = _input };
            _engine.NavigateToScreen(_screen);
            Frame();
        }

        [TestCleanup]
        public void TearDown()
        {
            System.Globalization.CultureInfo.CurrentUICulture = _culture;
            FontManager.UnregisterDynamicFont("touch-selection-test");
            _game.Dispose();
        }

        private static float X(int position) => TextLeft + position * 10;

        private void Frame(double ms = 16)
        {
            _input.SetTouches(new TouchCollection(Array.Empty<TouchLocation>()));
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(ms);
            _screen.Update();
        }

        private void Touch(TouchLocationState state, float x, float y, double ms = 16)
        {
            _input.SetTouches(new TouchCollection(new[] { new TouchLocation(1, state, new Vector2(x, y)) }));
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(ms);
            _screen.Update();
        }

        private void Hold(float x, float y, double ms)
        {
            for (var t = 0.0; t < ms; t += 16)
                Touch(TouchLocationState.Moved, x, y);
        }

        private void Tap(float x, float y)
        {
            Touch(TouchLocationState.Pressed, x, y);
            Touch(TouchLocationState.Released, x, y);
            Frame();
        }

        private void LongPress(float x, float y)
        {
            Touch(TouchLocationState.Pressed, x, y);
            Hold(x, y, 500);
            Touch(TouchLocationState.Released, x, y);
            Frame();
        }

        private string[] ToolbarLabels()
        {
            var layer = _screen.ExistingTextSelection!;
            var toolbar = (Border)layer.Content!;
            var items = (StackPanel)toolbar.Content!;
            return items.Children.Cast<Button>().Select(b => b.Content is TextBlock label ? label.Text : "more").ToArray();
        }

        [TestMethod]
        public void A_tap_places_the_caret_with_a_handle_and_no_selection()
        {
            Tap(X(8), LineY);

            Assert.IsTrue(_box.IsFocused);
            Assert.AreEqual(8, _box.CursorPosition);
            Assert.IsFalse(_box.HasSelection);
            Assert.IsTrue(_screen.ExistingTextSelection!.InsertionHandleShown);
            Assert.IsFalse(_screen.ExistingTextSelection.ToolbarShown);
        }

        [TestMethod]
        public void A_finger_dragged_over_the_text_selects_nothing()
        {
            Touch(TouchLocationState.Pressed, X(1), LineY);
            Touch(TouchLocationState.Moved, X(10), LineY);
            Touch(TouchLocationState.Released, X(10), LineY);
            Frame();

            Assert.IsFalse(_box.HasSelection);
        }

        [TestMethod]
        public void A_long_press_selects_the_word_and_shows_the_toolbar_on_release()
        {
            Touch(TouchLocationState.Pressed, X(7), LineY);
            Hold(X(7), LineY, 500);

            Assert.AreEqual("world", _box.SelectedText);
            Assert.IsFalse(_screen.ExistingTextSelection!.ToolbarShown, "the bar waits for the finger to lift");

            Touch(TouchLocationState.Released, X(7), LineY);
            Frame();

            Assert.IsTrue(_screen.ExistingTextSelection.ToolbarShown);
            CollectionAssert.AreEqual(new[] { "Cut", "Copy", "Select all" }, ToolbarLabels());
        }

        [TestMethod]
        public void Dragging_after_a_long_press_extends_the_selection_by_the_finger()
        {
            Touch(TouchLocationState.Pressed, X(7), LineY);
            Hold(X(7), LineY, 500);
            Touch(TouchLocationState.Moved, X(14), LineY);
            Touch(TouchLocationState.Released, X(14), LineY);
            Frame();

            Assert.AreEqual("world fo", _box.SelectedText);
        }

        [TestMethod]
        public void A_double_tap_selects_the_word()
        {
            Tap(X(2), LineY);
            Tap(X(2), LineY);

            Assert.AreEqual("hello", _box.SelectedText);
            Assert.IsTrue(_screen.ExistingTextSelection!.ToolbarShown);
        }

        [TestMethod]
        public void Dragging_the_end_handle_moves_the_selection_end()
        {
            LongPress(X(1), LineY);
            Assert.AreEqual("hello", _box.SelectedText);

            // The end handle hangs below the line at the end of "hello".
            _box.TryGetCaretPoint(5, out var tip, out _);
            Touch(TouchLocationState.Pressed, tip.X + 8, tip.Y + 10);
            Touch(TouchLocationState.Moved, X(11) + 8, tip.Y + 10);
            Assert.IsFalse(_screen.ExistingTextSelection!.ToolbarShown, "the bar hides while a handle moves");
            Touch(TouchLocationState.Released, X(11) + 8, tip.Y + 10);
            Frame();

            Assert.AreEqual("hello world", _box.SelectedText);
            Assert.IsTrue(_screen.ExistingTextSelection.ToolbarShown);
        }

        [TestMethod]
        public void A_handle_drag_does_not_reach_the_controls_under_it()
        {
            LongPress(X(1), LineY);
            _box.TryGetCaretPoint(5, out var tip, out _);
            var focusedBefore = _box.IsFocused;

            Touch(TouchLocationState.Pressed, tip.X + 8, tip.Y + 10);
            Touch(TouchLocationState.Released, tip.X + 8, tip.Y + 10);
            Frame();

            Assert.IsTrue(focusedBefore && _box.IsFocused);
            Assert.AreEqual("hello", _box.SelectedText);
        }

        [TestMethod]
        public void Copy_on_the_toolbar_copies_and_closes_it()
        {
            LongPress(X(7), LineY);
            var copy = ((StackPanel)((Border)_screen.ExistingTextSelection!.Content!).Content!).Children.Cast<Button>().ElementAt(1);
            var at = copy.ClippingRect;

            Tap(at.Left + at.Width / 2, at.Top + at.Height / 2);

            Assert.AreEqual("world", _clipboard.Text);
            Assert.IsFalse(_box.HasSelection);
            Assert.IsFalse(_screen.ExistingTextSelection.IsShown);
            Assert.AreEqual(0, _belowClicks);
        }

        [TestMethod]
        public void The_toolbar_offers_paste_when_the_clipboard_has_text()
        {
            _clipboard.Text = "x";
            LongPress(X(7), LineY);

            CollectionAssert.AreEqual(new[] { "Cut", "Copy", "Paste", "Select all" }, ToolbarLabels());
        }

        [TestMethod]
        public void Toolbar_labels_keep_their_color_while_pressed_and_swiped_over()
        {
            LongPress(X(7), LineY);
            var items = (StackPanel)((Border)_screen.ExistingTextSelection!.Content!).Content!;
            var copy = items.Children.Cast<Button>().ElementAt(1);
            var cut = items.Children.Cast<Button>().ElementAt(0).ClippingRect;
            var at = copy.ClippingRect;

            Touch(TouchLocationState.Pressed, at.Left + 5, at.Top + at.Height / 2);
            Hold(at.Left + 5, at.Top + at.Height / 2, 300);
            Touch(TouchLocationState.Moved, cut.Left + 5, cut.Top + cut.Height / 2);
            Touch(TouchLocationState.Moved, at.Right - 5, at.Top + at.Height / 2);

            var expected = PortableTheme.CreateDefault().TextSelectionToolbarTextColor;
            foreach (var button in items.Children.Cast<Button>())
                Assert.AreEqual(expected, ((TextBlock)button.Content!).TextColor);
        }

        [TestMethod]
        public void Entries_that_do_not_fit_go_behind_a_more_button()
        {
            _engine.SetScreenSize(250, 600);
            _screen.PerformLayoutIfDirty();
            _clipboard.Text = "x";
            LongPress(X(7), LineY);

            CollectionAssert.AreEqual(new[] { "Cut", "Copy", "more" }, ToolbarLabels());

            var items = (StackPanel)((Border)_screen.ExistingTextSelection!.Content!).Content!;
            var more = items.Children.Cast<Button>().Last().ClippingRect;
            Tap(more.Left + more.Width / 2, more.Top + more.Height / 2);

            CollectionAssert.AreEqual(new[] { "more", "Paste", "Select all" }, ToolbarLabels(), "a back arrow over the rest");
            Assert.AreEqual("world", _box.SelectedText);
        }

        [TestMethod]
        public void A_platform_menu_replaces_the_drawn_bar()
        {
            var menu = new RecordingMenu();
            _engine.Options.TextSelectionMenu = menu;
            LongPress(X(7), LineY);

            Assert.IsNotNull(menu.Shown);
            CollectionAssert.AreEqual(new[] { TextSelectionAction.Cut, TextSelectionAction.Copy, TextSelectionAction.SelectAll }, menu.Shown!.Actions.ToArray());
            Assert.AreEqual("world", menu.Shown.SelectedText);
            Assert.IsFalse(((Border)_screen.ExistingTextSelection!.Content!).IsVisible, "no drawn bar");
            _box.TryGetCaretPoint(6, out var start, out var lineHeight);
            Assert.AreEqual(start.X, menu.Shown.ContentRect.Left, 0.5f, "the menu stands over the word");
            Assert.AreEqual(start.Y - lineHeight, menu.Shown.ContentRect.Top, 0.5f);

            // Dragging a handle hides the menu for the moment and brings it back with the new selection.
            _box.TryGetCaretPoint(11, out var tip, out _);
            Touch(TouchLocationState.Pressed, tip.X + 8, tip.Y + 10);
            Touch(TouchLocationState.Moved, X(14) + 8, tip.Y + 10);
            Assert.IsTrue(menu.Suspends > 0);
            Assert.AreEqual(0, menu.Hides);
            Touch(TouchLocationState.Released, X(14) + 8, tip.Y + 10);
            Frame();
            Assert.AreEqual("world fo", menu.Shown!.SelectedText);

            // An entry chosen in the platform menu runs on the game thread.
            menu.Shown.Invoke(TextSelectionAction.Copy);
            _engine.Update(new GameTime(ScreenSystem.TotalTime, TimeSpan.FromMilliseconds(16)));
            Assert.AreEqual("world fo", _clipboard.Text);
            Assert.IsFalse(_box.HasSelection);
            Assert.IsTrue(menu.Hides > 0);
            Assert.IsNull(menu.Shown);
        }

        [TestMethod]
        public void A_tap_outside_closes_the_platform_menu()
        {
            var menu = new RecordingMenu();
            _engine.Options.TextSelectionMenu = menu;
            LongPress(X(7), LineY);
            var below = _below.ClippingRect;

            Tap(below.Left + 20, below.Top + 20);

            Assert.IsNull(menu.Shown);
            Assert.AreEqual(1, _belowClicks);
        }

        [TestMethod]
        public void Tapping_the_caret_again_shows_paste_and_select_all()
        {
            _clipboard.Text = "x";
            Tap(X(8), LineY);
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(400); // no double tap
            Tap(X(8), LineY);

            Assert.IsTrue(_screen.ExistingTextSelection!.ToolbarShown);
            CollectionAssert.AreEqual(new[] { "Paste", "Select all" }, ToolbarLabels());
        }

        [TestMethod]
        public void A_tap_outside_the_box_ends_the_selection_and_still_reaches_the_page()
        {
            LongPress(X(7), LineY);
            var below = _below.ClippingRect;

            Tap(below.Left + 20, below.Top + 20);

            Assert.IsFalse(_box.HasSelection);
            Assert.IsFalse(_screen.ExistingTextSelection!.IsShown);
            Assert.AreEqual(1, _belowClicks);
        }

        [TestMethod]
        public void The_caret_handle_hides_after_a_while()
        {
            Tap(X(8), LineY);
            Assert.IsTrue(_screen.ExistingTextSelection!.InsertionHandleShown);

            Frame(TextSelectionLayer.InsertionHandleTimeout.TotalMilliseconds + 300);

            Assert.IsFalse(_screen.ExistingTextSelection.IsShown);
        }

        [TestMethod]
        public void Without_touch_text_selection_a_finger_drag_selects_like_a_mouse()
        {
            _engine.Options.TouchTextSelection = false;
            Touch(TouchLocationState.Pressed, X(0), LineY);
            Touch(TouchLocationState.Moved, X(5), LineY);
            Touch(TouchLocationState.Released, X(5), LineY);
            Frame();

            Assert.AreEqual("hello", _box.SelectedText);
        }

        [TestMethod]
        public void An_adorner_takes_only_the_presses_it_hits()
        {
            var bar = new Bar();
            _screen.Adorners.Add(bar);
            Frame();
            var below = _below.ClippingRect;
            bar.Content!.Margin = new Thickness(below.Left, below.Top, 0, 0);
            _screen.PerformLayoutIfDirty();
            Frame();

            // On the bar: the button under it gets nothing.
            Tap(below.Left + 20, below.Top + 20);
            Assert.AreEqual(1, bar.Downs);
            Assert.AreEqual(0, _belowClicks);

            // Beside the bar: the page gets the press, the adorner hears about it.
            Tap(below.Left + 150, below.Top + 20);
            Assert.AreEqual(1, _belowClicks);
            Assert.AreEqual(1, bar.Outside);

            _screen.Adorners.Remove(bar);
            Tap(below.Left + 20, below.Top + 20);
            Assert.AreEqual(2, _belowClicks);
        }
    }
}
