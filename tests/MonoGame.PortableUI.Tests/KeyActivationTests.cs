using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>#137: an app's own focusable control takes the Enter/Space activation of buttons (ActivatesOnKeys).</summary>
    [TestClass]
    [DoNotParallelize] // drives the global ScreenSystem.TotalTime clock
    public class KeyActivationTests
    {
        private sealed class TestScreen : Screen
        {
        }

        /// <summary>A menu row as games build them: a plain content control, not a Button with its chrome.</summary>
        private sealed class Row : ContentControl
        {
            public int Clicks;

            public Row()
            {
                Height = 40;
                Width = 200;
                HorizontalAlignment = HorizontalAlignment.Left;
                Click += (_, _) => Clicks++;
            }
        }

        private sealed class Host : IDisposable
        {
            private readonly Game _game = new Game();
            private TimeSpan _time = TimeSpan.FromSeconds(1);
            public readonly VirtualInputSource Input = new VirtualInputSource();
            public readonly ScreenEngine Engine;

            public Host(Control content)
            {
                Engine = ScreenEngine.CreateSurfaceEngine(_game, new ScreenEngineOptions { AddComponentToGame = false, ScreenSizeMode = ScreenSizeMode.Manual, DebugOverlayKey = null });
                Engine.SetScreenSize(400, 300);
                var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
                panel.AddChild(content);
                Input.SetPointer(new PointF(390, 290));
                Engine.NavigateToScreen(new TestScreen { Content = panel, InputSource = Input });
                Step();
            }

            public void Step()
            {
                _time += TimeSpan.FromMilliseconds(16);
                ScreenSystem.TotalTime = _time;
                Engine.Update(new GameTime(_time, TimeSpan.FromMilliseconds(16)));
            }

            public void Hold(params Keys[] keys)
            {
                Input.SetKeyboardState(new KeyboardState(keys));
                Step();
            }

            public void Press(params Keys[] keys)
            {
                Hold(keys);
                Hold();
            }

            public void Dispose()
            {
                Engine.Dispose();
                _game.Dispose();
            }
        }

        [TestMethod]
        public void An_app_control_is_not_activated_by_keys_unless_it_asks()
        {
            var row = new Row();
            using var host = new Host(row);
            row.Focus();

            host.Press(Keys.Space);
            host.Press(Keys.Enter);

            Assert.IsFalse(row.ActivatesOnKeys);
            Assert.AreEqual(0, row.Clicks);
        }

        [TestMethod]
        public void An_app_control_with_ActivatesOnKeys_clicks_like_a_button()
        {
            var row = new Row { ActivatesOnKeys = true };
            using var host = new Host(row);
            row.Focus();

            host.Press(Keys.Enter);
            Assert.AreEqual(1, row.Clicks, "Enter");

            host.Press(Keys.Space);
            Assert.AreEqual(2, row.Clicks, "Space as a key, without window text input");

            // Holding Space does not repeat; the window's typed space while the key is down adds nothing.
            host.Hold(Keys.Space);
            for (var i = 0; i < 60; i++)
                host.Step();
            host.Engine.HandleTextInput(' ');
            host.Hold();
            Assert.AreEqual(3, row.Clicks, "one click per Space press");

            // With a modifier it is someone else's shortcut.
            host.Press(Keys.LeftControl, Keys.Space);
            Assert.AreEqual(3, row.Clicks, "Ctrl+Space");

            // An on-screen keyboard's space (no key down) activates.
            host.Engine.HandleTextInput(' ');
            Assert.AreEqual(4, row.Clicks, "typed space");

            row.ActivatesOnKeys = false;
            host.Press(Keys.Space);
            host.Press(Keys.Enter);
            Assert.AreEqual(4, row.Clicks, "switched off again");
        }

        [TestMethod]
        public void Clickable_controls_activate_on_keys_and_a_toggle_switch_takes_the_space_key()
        {
            Assert.IsTrue(new Button().ActivatesOnKeys);
            Assert.IsTrue(new CheckBox().ActivatesOnKeys);
            var toggle = new ToggleSwitch();
            Assert.IsTrue(toggle.ActivatesOnKeys);

            // Space as a key now toggles a switch too (before, only the typed space did, so surfaces could not).
            using var host = new Host(toggle);
            toggle.Focus();
            host.Press(Keys.Space);
            Assert.IsTrue(toggle.IsOn);
            host.Press(Keys.Enter);
            Assert.IsFalse(toggle.IsOn);
        }
    }
}
