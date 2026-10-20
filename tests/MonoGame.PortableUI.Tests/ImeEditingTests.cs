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
    [DoNotParallelize]
    public class ImeEditingTests
    {
        private sealed class TestScreen : Screen
        {
        }

        // An input method that edits the field like Android's: it gets an editor and hears about outside changes.
        private sealed class FakeIme : IOnScreenKeyboard, ITextEditKeyboard
        {
            public OnScreenKeyboardRequest? Request { get; private set; }
            public List<TextEditState> Told { get; } = new List<TextEditState>();
            public bool IsVisible { get; private set; }
            public event EventHandler<OnScreenKeyboardEventArgs>? VisibilityChanged;

            public void Show(OnScreenKeyboardRequest request)
            {
                Request = request;
                IsVisible = true;
                VisibilityChanged?.Invoke(this, new OnScreenKeyboardEventArgs(true, 0));
            }

            public void Hide()
            {
                IsVisible = false;
                VisibilityChanged?.Invoke(this, new OnScreenKeyboardEventArgs(false, 0));
            }

            public void TargetChanged(TextEditState state) => Told.Add(state);
        }

        private Game _game = null!;
        private ScreenEngine _engine = null!;
        private TestScreen _screen = null!;
        private FakeIme _ime = null!;
        private TextBox _box = null!;

        [TestInitialize]
        public void SetUp()
        {
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(150);
            _game = new Game();
            _ime = new FakeIme();
            _engine = ScreenEngine.Initialize(_game, new ScreenEngineOptions { AddComponentToGame = false, OnScreenKeyboard = _ime });
            _engine.SetScreenSize(400, 600);
            _box = new TextBox { Text = "Ladder", Height = 40 };
            _screen = new TestScreen { Content = new StackPanel { Children = { _box } } };
            _engine.NavigateToScreen(_screen);
            Frame();
            _box.Focus();
            _box.Select(6, 0);
            Frame();
            _ime.Told.Clear();
        }

        [TestCleanup]
        public void TearDown()
        {
            _engine.Dispose();
            _game.Dispose();
        }

        private void Frame()
        {
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
            _screen.Update();
        }

        private ITextEditTarget Editor => _ime.Request!.Editor!;

        [TestMethod]
        public void The_keyboard_gets_an_editor_with_the_field_as_it_is()
        {
            Assert.IsNotNull(_ime.Request?.Editor);
            Assert.AreEqual("Ladder", Editor.State.Text);
        }

        [TestMethod]
        public void A_composed_word_becomes_text_like_typing_and_can_be_undone()
        {
            var changes = 0;
            _box.TextChanged += (_, _) => changes++;

            Editor.Apply(new TextEditState("Ladder dri", 10, 10, 7, 10));
            Editor.Apply(new TextEditState("Ladder drill", 12, 12, 7, 12));
            Frame();

            Assert.AreEqual("Ladder drill", _box.Text);
            Assert.AreEqual(12, _box.CursorPosition);
            Assert.AreEqual(2, changes);
            Assert.AreEqual(0, _ime.Told.Count, "its own edits are not echoed back");
            Assert.AreEqual(7, Editor.State.ComposingStart);
            _box.Undo();
            Assert.AreEqual("Ladder dri", _box.Text);
        }

        [TestMethod]
        public void A_caret_moved_here_is_told_to_the_keyboard_and_ends_the_composed_word()
        {
            Editor.Apply(new TextEditState("Ladder ab", 9, 9, 7, 9));
            Frame();

            _box.Select(2, 0);
            Frame();

            Assert.AreEqual(1, _ime.Told.Count);
            Assert.AreEqual(2, _ime.Told[0].SelectionStart);
            Assert.AreEqual(-1, _ime.Told[0].ComposingStart);
        }

        [TestMethod]
        public void Text_the_field_refused_is_told_back()
        {
            _box.MaxLength = 8;

            Editor.Apply(new TextEditState("Ladder drill", 12, 12));
            Frame();

            Assert.AreEqual("Ladder d", _box.Text);
            Assert.AreEqual("Ladder d", _ime.Told[^1].Text, "the keyboard must not keep editing text the field does not have");
        }

        [TestMethod]
        public void Done_presses_enter_and_closes_the_keyboard_of_a_one_line_field()
        {
            var entered = 0;
            _box.EnterPressed += (_, _) => entered++;

            Editor.Submit();

            Assert.AreEqual(1, entered);
            Assert.IsFalse(_ime.IsVisible);
            Assert.IsTrue(_box.IsFocused);
        }
    }
}
