using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>#120: game-menu focus - pointer selects, hover merges into focus, arrows wrap, Space is a key.</summary>
    [TestClass]
    [DoNotParallelize] // drives the global ScreenSystem.TotalTime clock
    public class MenuFocusTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private sealed class Entry : Button
        {
            public int Clicks;

            public Entry()
            {
                Height = 40;
                Width = 200;
                HorizontalAlignment = HorizontalAlignment.Left;
                Click += (_, _) => Clicks++;
            }

            public ControlVisualState State => GetVisualState();
        }

        private sealed class Menu : IDisposable
        {
            private readonly Game _game = new Game();
            private TimeSpan _time = TimeSpan.FromSeconds(1);
            public readonly VirtualInputSource Input = new VirtualInputSource();
            public readonly ScreenEngine Engine;
            public readonly TestScreen Screen;
            public readonly Entry[] Entries = { new Entry(), new Entry(), new Entry() };

            public Menu(bool followPointer = true, bool wrap = true)
            {
                Engine = ScreenEngine.CreateSurfaceEngine(_game, new ScreenEngineOptions { AddComponentToGame = false, ScreenSizeMode = ScreenSizeMode.Manual, DebugOverlayKey = null });
                Engine.SetScreenSize(400, 300);
                var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
                foreach (var entry in Entries)
                    panel.AddChild(entry);
                Screen = new TestScreen { Content = panel, InputSource = Input, FocusFollowsPointer = followPointer, WrapFocusNavigation = wrap };
                Input.SetPointer(new PointF(390, 290));
                Engine.NavigateToScreen(Screen);
                Step();
            }

            public void Step()
            {
                _time += TimeSpan.FromMilliseconds(16);
                ScreenSystem.TotalTime = _time;
                Engine.Update(new GameTime(_time, TimeSpan.FromMilliseconds(16)));
            }

            public void Press(params Keys[] keys)
            {
                Input.SetKeyboardState(new KeyboardState(keys));
                Step();
                Input.SetKeyboardState(new KeyboardState());
                Step();
            }

            public void PointAt(int entry)
            {
                var rect = Entries[entry].ClippingRect;
                Input.SetPointer(new PointF(rect.Left + 10, rect.Top + rect.Height / 2));
                Step();
            }

            public void Dispose()
            {
                Engine.Dispose();
                _game.Dispose();
            }
        }

        [TestMethod]
        public void Pointer_movement_focuses_the_entry_and_shows_one_highlight()
        {
            using var menu = new Menu();
            menu.Entries[0].Focus();

            menu.PointAt(1);

            Assert.AreSame(menu.Entries[1], menu.Engine.FocusedControl);
            Assert.AreEqual(ControlVisualState.Focused, menu.Entries[1].State, "hover merges into focus");
            Assert.AreEqual(ControlVisualState.Normal, menu.Entries[0].State);
            Assert.IsTrue(menu.Entries[1].IsFocusVisualShown, "pointer focus shows the focus look");

            // Keyboard moves the selection away; moving the pointer within the entry takes it back.
            menu.Press(Keys.Down);
            Assert.AreSame(menu.Entries[2], menu.Engine.FocusedControl);
            Assert.AreEqual(ControlVisualState.Normal, menu.Entries[1].State, "the hovered entry shows no second highlight");
            var rect = menu.Entries[1].ClippingRect;
            menu.Input.SetPointer(new PointF(rect.Left + 30, rect.Top + rect.Height / 2));
            menu.Step();
            Assert.AreSame(menu.Entries[1], menu.Engine.FocusedControl);
        }

        [TestMethod]
        public void Without_the_option_hover_neither_focuses_nor_merges()
        {
            using var menu = new Menu(followPointer: false);
            menu.Entries[0].Focus();
            menu.PointAt(1);

            Assert.AreSame(menu.Entries[0], menu.Engine.FocusedControl);
            Assert.AreEqual(ControlVisualState.Hover, menu.Entries[1].State);
        }

        [TestMethod]
        public void Arrows_wrap_around_the_ends()
        {
            using var menu = new Menu();
            menu.Entries[2].Focus();
            menu.Press(Keys.Down);
            Assert.AreSame(menu.Entries[0], menu.Engine.FocusedControl);
            menu.Press(Keys.Up);
            Assert.AreSame(menu.Entries[2], menu.Engine.FocusedControl);
        }

        [TestMethod]
        public void Arrows_stop_at_the_ends_without_wrap()
        {
            using var menu = new Menu(wrap: false);
            menu.Entries[2].Focus();
            menu.Press(Keys.Down);
            Assert.AreSame(menu.Entries[2], menu.Engine.FocusedControl);
        }

        [TestMethod]
        public void Space_key_activates_without_text_input_once()
        {
            using var menu = new Menu();
            menu.Entries[1].Focus();

            menu.Press(Keys.Space);
            Assert.AreEqual(1, menu.Entries[1].Clicks);

            // The window's typed space arrives while the key is still down: no second click.
            menu.Input.SetKeyboardState(new KeyboardState(Keys.Space));
            menu.Step();
            menu.Engine.HandleTextInput(' ');
            Assert.AreEqual(2, menu.Entries[1].Clicks, "key down clicks once, the typed space is ignored");

            // An on-screen keyboard's space (no key down) still activates.
            menu.Input.SetKeyboardState(new KeyboardState());
            menu.Step();
            menu.Engine.HandleTextInput(' ');
            Assert.AreEqual(3, menu.Entries[1].Clicks);
        }
    }
}
