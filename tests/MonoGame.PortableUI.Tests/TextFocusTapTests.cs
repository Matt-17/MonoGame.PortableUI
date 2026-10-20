using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class TextFocusTapTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private sealed class FakeKeyboard : IOnScreenKeyboard
        {
            public int Shown { get; private set; }
            public bool IsVisible { get; set; }
            public event EventHandler<OnScreenKeyboardEventArgs>? VisibilityChanged;

            public void Show(OnScreenKeyboardRequest request)
            {
                Shown++;
                IsVisible = true;
                VisibilityChanged?.Invoke(this, new OnScreenKeyboardEventArgs(true, 300));
            }

            public void Hide()
            {
                IsVisible = false;
                VisibilityChanged?.Invoke(this, new OnScreenKeyboardEventArgs(false, 0));
            }
        }

        private Game _game = null!;
        private ScreenEngine _engine = null!;
        private TestScreen _screen = null!;
        private VirtualInputSource _input = null!;
        private FakeKeyboard _keyboard = null!;
        private TextBox _box = null!;
        private Button _button = null!;
        private ScrollViewer _list = null!;

        [TestInitialize]
        public void SetUp()
        {
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(90);
            _game = new Game();
            _keyboard = new FakeKeyboard();
            _engine = ScreenEngine.Initialize(_game, new ScreenEngineOptions { AddComponentToGame = false, OnScreenKeyboard = _keyboard });
            _engine.SetScreenSize(400, 600);
            _box = new TextBox { Height = 40 };
            _button = new Button { Text = "Send", Height = 40 };
            var tall = new StackPanel();
            for (var i = 0; i < 30; i++)
                tall.AddChild(new Border { Height = 50 });
            _list = new ScrollViewer { Content = tall, Height = 400 };
            var page = new StackPanel { Children = { _box, _button, _list } };
            _input = new VirtualInputSource();
            _screen = new TestScreen { Content = page, InputSource = _input };
            _engine.NavigateToScreen(_screen);
            Frame();
            _box.Focus();
            // The engine takes the keyboard's height over as an inset on its own update.
            _engine.Update(new GameTime());
            Frame();
        }

        [TestCleanup]
        public void TearDown()
        {
            _engine.Dispose();
            _game.Dispose();
        }

        private void Frame()
        {
            _input.SetTouches(new TouchCollection(Array.Empty<TouchLocation>()));
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
            _screen.Update();
        }

        private void Touch(TouchLocationState state, float x, float y)
        {
            _input.SetTouches(new TouchCollection(new[] { new TouchLocation(1, state, new Vector2(x, y)) }));
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
            _screen.Update();
        }

        private void Tap(float x, float y)
        {
            Touch(TouchLocationState.Pressed, x, y);
            Touch(TouchLocationState.Released, x, y);
            Frame();
        }

        [TestMethod]
        public void A_tap_on_empty_space_unfocuses_the_text_field()
        {
            Tap(200, 300);

            Assert.IsFalse(_box.IsFocused);
            Assert.IsFalse(_keyboard.IsVisible, "the keyboard went away with the focus");
        }

        [TestMethod]
        public void A_scroll_that_starts_outside_keeps_the_focus()
        {
            Touch(TouchLocationState.Pressed, 200, 500);
            for (var y = 480; y >= 200; y -= 20)
                Touch(TouchLocationState.Moved, 200, y);
            Touch(TouchLocationState.Released, 200, 200);
            Frame();

            Assert.IsTrue(_box.IsFocused);
        }

        [TestMethod]
        public void The_option_off_keeps_the_focus()
        {
            _engine.Options.TapOutsideUnfocusesText = false;

            Tap(200, 300);

            Assert.IsTrue(_box.IsFocused);
        }

        [TestMethod]
        public void Tapping_the_focused_field_brings_a_closed_keyboard_back()
        {
            var shown = _keyboard.Shown;
            // Android back closes the keyboard: only the window inset drops, the field keeps its focus.
            _engine.SetKeyboardInset(0);
            _engine.Update(new GameTime());

            Tap(100, 20);
            Frame();

            Assert.IsTrue(_box.IsFocused);
            Assert.AreEqual(shown + 1, _keyboard.Shown);
        }

        [TestMethod]
        public void Tapping_the_focused_field_with_the_keyboard_up_does_not_show_it_again()
        {
            var shown = _keyboard.Shown;

            Tap(100, 20);
            Frame();

            Assert.AreEqual(shown, _keyboard.Shown);
        }
    }
}
