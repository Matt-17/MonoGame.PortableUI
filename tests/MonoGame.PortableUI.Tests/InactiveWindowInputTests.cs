using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class InactiveWindowInputTests
    {
        [TestMethod]
        public void Device_input_is_not_read_while_the_window_is_inactive()
        {
            using var game = new Game();
            if (game.IsActive)
                Assert.Inconclusive("Needs a game whose window is not active.");
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(200, 120);
            var screen = new TestScreen { InputSource = DeviceInputSource.Instance };
            screen.Content = new Button { Text = "Run" };
            engine.NavigateToScreen(screen);

            screen.Update();

            // Whatever the real mouse does, the screen sees a pointer that has left the window.
            Assert.IsTrue(screen.LastMousePosition.X < -1000 && screen.LastMousePosition.Y < -1000);
            Assert.IsFalse(engine.ReadsDeviceInput);
            engine.Options.InputWhenInactive = true;
            Assert.IsTrue(engine.ReadsDeviceInput);
        }

        [TestMethod]
        public void Custom_input_sources_work_while_the_window_is_inactive()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(200, 120);
            var source = new VirtualInputSource();
            var button = new Button { Width = 80, Height = 40, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Text = "Run" };
            var clicks = 0;
            button.Click += (_, _) => clicks++;
            var screen = new TestScreen { InputSource = source, Content = button };
            engine.NavigateToScreen(screen);
            screen.InvalidateLayout(true);

            var at = new PointF(20, 20);
            source.SetPointer(at);
            screen.Update();
            source.SetPointer(at, leftDown: true);
            screen.Update();
            source.SetPointer(at);
            screen.Update();

            Assert.AreEqual(1, clicks);
        }

        private sealed class TestScreen : Screen
        {
        }
    }
}
