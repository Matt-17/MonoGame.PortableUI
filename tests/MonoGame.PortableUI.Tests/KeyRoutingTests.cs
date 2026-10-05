using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>#90: every key - F1-F12, Alt and Ctrl chords - reaches the focused control as KeyDown,
    /// bubbles to its parents and the screen, and claimed keys skip the screen's own handling.</summary>
    [TestClass]
    [DoNotParallelize] // drives the global ScreenSystem.TotalTime clock
    public class KeyRoutingTests
    {
        private sealed class TestScreen : Screen
        {
        }

        /// <summary>A focusable control that records its keys and claims the ones it is told to.</summary>
        private sealed class Editor : Control
        {
            public readonly List<string> Log = new List<string>();
            public readonly HashSet<Keys> Claims = new HashSet<Keys>();

            public Editor()
            {
                IsFocusable = true;
                Width = 100;
                Height = 30;
                KeyDown += (_, args) =>
                {
                    Log.Add(Describe(args));
                    if (Claims.Contains(args.Key))
                        args.Handled = true;
                };
                KeyUp += (_, args) => Log.Add("up " + args.Key);
                KeyPressed += (_, args) => Log.Add("command " + args.Command);
            }
        }

        private static string Describe(KeyEventArgs args)
            => (args.Modifiers != KeyboardModifiers.None ? args.Modifiers + "+" : "") + args.Key + (args.IsRepeat ? " (repeat)" : "");

        private sealed class Ui : IDisposable
        {
            private readonly Game _game = new Game();
            public readonly VirtualInputSource Input = new VirtualInputSource();
            public readonly ScreenEngine Engine;
            public readonly TestScreen Screen;
            public readonly StackPanel Panel = new StackPanel();
            public readonly Editor Editor = new Editor();
            public readonly Editor Other = new Editor();
            public readonly List<string> PanelLog = new List<string>();
            public readonly List<string> ScreenLog = new List<string>();
            private TimeSpan _time = TimeSpan.FromSeconds(1);

            public Ui(bool surface = false, Keys? debugKey = Keys.F3)
            {
                Engine = ScreenEngine.CreateSurfaceEngine(_game, new ScreenEngineOptions { AddComponentToGame = false, ScreenSizeMode = ScreenSizeMode.Manual, DebugOverlayKey = debugKey });
                Engine.SetScreenSize(400, 300);
                Panel.AddChild(Editor);
                Panel.AddChild(Other);
                Panel.KeyDown += (_, args) => PanelLog.Add(Describe(args));
                Screen = new TestScreen { Content = Panel, InputSource = Input };
                Screen.KeyDown += (_, args) => ScreenLog.Add(Describe(args));
                Engine.NavigateToScreen(Screen);
                Step(16);
            }

            public void Step(double milliseconds)
            {
                _time += TimeSpan.FromMilliseconds(milliseconds);
                ScreenSystem.TotalTime = _time;
                Engine.Update(new GameTime(_time, TimeSpan.FromMilliseconds(milliseconds)));
            }

            public void Press(params Keys[] keys)
            {
                Input.SetKeyboardState(new KeyboardState(keys));
                Step(16);
            }

            public void ReleaseAll() => Press();

            public void Dispose()
            {
                Engine.Dispose();
                _game.Dispose();
            }
        }

        [TestMethod]
        public void Function_keys_and_alt_chords_reach_the_focused_control()
        {
            using var ui = new Ui();
            ui.Editor.Focus();

            ui.Press(Keys.F2);
            ui.ReleaseAll();
            ui.Press(Keys.LeftAlt);
            ui.Press(Keys.LeftAlt, Keys.F);
            ui.ReleaseAll();
            ui.Press(Keys.LeftControl, Keys.F9);
            ui.ReleaseAll();

            CollectionAssert.Contains(ui.Editor.Log, "F2");
            CollectionAssert.Contains(ui.Editor.Log, "Alt+F");
            CollectionAssert.Contains(ui.Editor.Log, "Control+F9");
            CollectionAssert.Contains(ui.Editor.Log, "up F2");
        }

        [TestMethod]
        public void Unclaimed_keys_bubble_to_the_parents_and_the_screen()
        {
            using var ui = new Ui();
            ui.Editor.Focus();

            ui.Press(Keys.LeftAlt, Keys.X);

            CollectionAssert.Contains(ui.Editor.Log, "Alt+X");
            CollectionAssert.Contains(ui.PanelLog, "Alt+X");
            CollectionAssert.Contains(ui.ScreenLog, "Alt+X");
        }

        [TestMethod]
        public void A_claimed_key_stops_bubbling_and_skips_the_editing_command()
        {
            using var ui = new Ui();
            ui.Editor.Focus();
            ui.Editor.Claims.Add(Keys.Back);

            ui.Press(Keys.Back);

            CollectionAssert.Contains(ui.Editor.Log, "Back");
            CollectionAssert.DoesNotContain(ui.Editor.Log, "command Backspace");
            Assert.AreEqual(0, ui.PanelLog.Count);
            Assert.AreEqual(0, ui.ScreenLog.Count);

            ui.ReleaseAll();
            ui.Editor.Claims.Clear();
            ui.Press(Keys.Back);
            CollectionAssert.Contains(ui.Editor.Log, "command Backspace", "unclaimed keys keep their editing command");
        }

        [TestMethod]
        public void A_control_can_claim_tab_instead_of_moving_focus()
        {
            using var ui = new Ui();
            ui.Editor.Focus();

            ui.Press(Keys.Tab);
            ui.ReleaseAll();
            Assert.IsTrue(ui.Other.IsFocused, "unclaimed Tab moves focus");

            ui.Other.Claims.Add(Keys.Tab);
            ui.Press(Keys.Tab);
            Assert.IsTrue(ui.Other.IsFocused, "a claimed Tab (an editor inserting a tab) keeps focus");
        }

        [TestMethod]
        public void Screen_shortcuts_work_with_nothing_focused()
        {
            using var ui = new Ui();

            ui.Press(Keys.F10);

            CollectionAssert.Contains(ui.ScreenLog, "F10");
        }

        [TestMethod]
        public void A_held_key_repeats_after_the_typematic_delay()
        {
            using var ui = new Ui();
            ui.Editor.Focus();

            ui.Press(Keys.F8);
            for (var i = 0; i < 40; i++)
                ui.Step(16); // ~640 ms held

            var repeats = ui.Editor.Log.FindAll(entry => entry == "F8 (repeat)").Count;
            Assert.IsTrue(repeats >= 2, $"repeats after 500 ms, every 45 ms ({repeats})");

            ui.Press(Keys.LeftShift);
            for (var i = 0; i < 40; i++)
                ui.Step(16);
            CollectionAssert.DoesNotContain(ui.Editor.Log, "Shift+LeftShift (repeat)", "modifier keys do not repeat");
        }

        [TestMethod]
        public void The_debug_overlay_key_is_configurable_and_off_for_surfaces()
        {
            using (var ui = new Ui(debugKey: Keys.F3))
            {
                ui.Press(Keys.F3);
                Assert.IsTrue(ui.Engine.DebugOverlayEnabled, "F3 toggles by default");
            }

            using (var ui = new Ui(debugKey: Keys.F3))
            {
                ui.Editor.Focus();
                ui.Editor.Claims.Add(Keys.F3);
                ui.Press(Keys.F3);
                Assert.IsFalse(ui.Engine.DebugOverlayEnabled, "a control that claims F3 (Turbo Pascal: open file) keeps it");
            }

            using (var ui = new Ui(debugKey: null))
            {
                ui.Editor.Focus();
                ui.Press(Keys.F3);
                Assert.IsFalse(ui.Engine.DebugOverlayEnabled);
                CollectionAssert.Contains(ui.Editor.Log, "F3");
            }

            using var game = new Game();
            using var surface = new UISurface(game, new TestScreen(), 320, 200);
            Assert.IsNull(surface.Engine.Options.DebugOverlayKey, "surface applications get F3");
        }
    }
}
