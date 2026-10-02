using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class FocusVisibleTests
    {
        private sealed class TestScreen : Screen
        {
        }

        [TestMethod]
        public void Focus_ring_shows_for_keyboard_navigation_but_not_after_a_click()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 300);
            var screen = new TestScreen();
            var button = new Button { Text = "Go", Width = 80, Height = 30, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, ShowFocusVisual = true, IsFocusable = true };
            var field = new TextBox { Width = 80, Height = 30, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 60, 0, 0), ShowFocusVisual = true };
            var panel = new Grid();
            panel.AddChild(button);
            panel.AddChild(field);
            screen.Content = panel;
            var input = new VirtualInputSource();
            screen.InputSource = input;
            engine.NavigateToScreen(screen);
            void Tick() { ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16); engine.Update(new GameTime(ScreenSystem.TotalTime, TimeSpan.FromMilliseconds(16))); }
            Tick();

            input.SetPointer(new PointF(20, 10));
            Tick();
            input.SetPointer(new PointF(20, 10), leftDown: true);
            Tick();
            input.SetPointer(new PointF(20, 10));
            Tick();
            Assert.IsTrue(button.IsFocused);
            Assert.IsFalse(button.NeedsOverlayPass, "no ring after a click");

            input.SetKeyboardState(new KeyboardState(Keys.Tab));
            Tick();
            input.SetKeyboardState(new KeyboardState());
            Tick();
            Assert.IsTrue(engine.KeyboardNavigationActive);
            var focused = engine.FocusedControl;
            Assert.IsNotNull(focused);
            Assert.IsTrue(focused!.NeedsOverlayPass, "keyboard focus shows the ring");

            field.Focus();
            engine.KeyboardNavigationActive = false;
            Assert.IsTrue(field.NeedsOverlayPass, "text entry keeps its ring after a click");
        }
    }
}
