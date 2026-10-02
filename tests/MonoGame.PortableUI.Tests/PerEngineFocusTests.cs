using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class PerEngineFocusTests
    {
        private sealed class TestScreen : Screen
        {
        }

        [TestMethod]
        public void Each_engine_keeps_its_own_focused_control()
        {
            using var game = new Game();
            var first = ScreenEngine.CreateSurfaceEngine(game, new ScreenEngineOptions { AddComponentToGame = false });
            var second = ScreenEngine.CreateSurfaceEngine(game, new ScreenEngineOptions { AddComponentToGame = false });
            var a = new Button { Text = "A" };
            var b = new Button { Text = "B" };
            first.NavigateToScreen(new TestScreen { Content = a });
            second.NavigateToScreen(new TestScreen { Content = b });

            a.Focus();
            b.Focus();

            Assert.AreSame(a, first.FocusedControl, "focusing on the second computer does not steal the first one's focus");
            Assert.AreSame(b, second.FocusedControl);
            Assert.IsTrue(a.IsFocused);
            Assert.IsTrue(b.IsFocused);

            second.FocusedControl = null;
            Assert.AreSame(a, first.FocusedControl);
            first.Dispose();
            second.Dispose();
        }
    }
}
