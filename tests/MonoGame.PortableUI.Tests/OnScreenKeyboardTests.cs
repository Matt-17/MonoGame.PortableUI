using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class OnScreenKeyboardTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private sealed class FakeKeyboard : IOnScreenKeyboard
        {
            public List<OnScreenKeyboardRequest> Shown { get; } = new List<OnScreenKeyboardRequest>();
            public int Hidden { get; private set; }
            public float Height { get; set; } = 300;
            public bool IsVisible { get; private set; }
            public event EventHandler<OnScreenKeyboardEventArgs>? VisibilityChanged;

            public void Show(OnScreenKeyboardRequest request)
            {
                Shown.Add(request);
                IsVisible = true;
                VisibilityChanged?.Invoke(this, new OnScreenKeyboardEventArgs(true, Height));
            }

            public void Hide()
            {
                Hidden++;
                IsVisible = false;
                VisibilityChanged?.Invoke(this, new OnScreenKeyboardEventArgs(false, 0));
            }
        }

        [TestInitialize]
        public void Reset() { if (ScreenEngine.Instance != null) ScreenEngine.Instance.FocusedControl = null; }

        [TestMethod]
        public void Focusing_a_text_box_shows_the_keyboard_with_its_purpose_and_blur_hides_it()
        {
            using var game = new Game();
            var keyboard = new FakeKeyboard();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, OnScreenKeyboard = keyboard });
            engine.SetScreenSize(400, 800);
            var box = new TextBox { InputPurpose = TextInputPurpose.Email, Text = "a@b.c", Height = 40 };
            var screen = new TestScreen { Content = new StackPanel { Children = { box } } };
            engine.NavigateToScreen(screen);
            engine.Update(new GameTime());

            box.Focus();
            Assert.AreEqual(1, keyboard.Shown.Count);
            Assert.AreEqual(TextInputPurpose.Email, keyboard.Shown[0].Purpose);
            Assert.AreEqual("a@b.c", keyboard.Shown[0].Text);

            keyboard.Shown[0].Commit!("typed@steam.deck");
            Assert.AreEqual("typed@steam.deck", box.Text, "full-text keyboards commit into the field");

            if (ScreenEngine.Instance != null) ScreenEngine.Instance.FocusedControl = null;
            Assert.AreEqual(1, keyboard.Hidden);
        }

        [TestMethod]
        public void Password_fields_request_the_password_purpose_without_their_text()
        {
            using var game = new Game();
            var keyboard = new FakeKeyboard();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, OnScreenKeyboard = keyboard });
            var box = new TextBox { PasswordChar = '*', Text = "secret" };
            var screen = new TestScreen { Content = box };
            engine.NavigateToScreen(screen);

            box.Focus();
            Assert.AreEqual(TextInputPurpose.Password, keyboard.Shown[0].Purpose);
            Assert.AreEqual("", keyboard.Shown[0].Text);
        }

        [TestMethod]
        public void Keyboard_height_scrolls_the_focused_field_into_view()
        {
            using var game = new Game();
            var keyboard = new FakeKeyboard { Height = 400 };
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, OnScreenKeyboard = keyboard });
            engine.SetScreenSize(400, 800);
            var box = new TextBox { Height = 40, Margin = new Thickness(0, 700, 0, 0) };
            var scroll = new ScrollViewer { Content = new StackPanel { Children = { box, new Border { Height = 600 } } } };
            var screen = new TestScreen { Content = new SafeAreaPanel(scroll) };
            engine.NavigateToScreen(screen);
            engine.Update(new GameTime());

            box.Focus();
            engine.Update(new GameTime());

            Assert.AreEqual(400, engine.KeyboardInset);
            Assert.IsTrue(box.ClippingRect.Bottom <= 400, $"field bottom {box.ClippingRect.Bottom} is above the keyboard");

            if (ScreenEngine.Instance != null) ScreenEngine.Instance.FocusedControl = null;
            engine.Update(new GameTime());
            Assert.AreEqual(0, engine.KeyboardInset);
        }

        [TestMethod]
        public void Delegate_keyboard_reports_visibility_without_any_sdk()
        {
            var shown = 0;
            var hidden = 0;
            var keyboard = new DelegateOnScreenKeyboard(_ => { shown++; return true; }, () => hidden++);
            var changes = new List<bool>();
            keyboard.VisibilityChanged += (s, e) => changes.Add(e.IsVisible);

            keyboard.Show(new OnScreenKeyboardRequest());
            keyboard.Show(new OnScreenKeyboardRequest());
            keyboard.NotifyClosed();
            keyboard.Hide();

            Assert.AreEqual(2, shown);
            Assert.AreEqual(0, hidden, "already closed by the platform");
            CollectionAssert.AreEqual(new[] { true, false }, changes);
        }
    }
}
