using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class SafeAreaTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private static (ScreenEngine Engine, TestScreen Screen, SafeAreaPanel Panel, Border Inner) Create(Game game)
        {
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 800);
            var inner = new Border();
            var panel = new SafeAreaPanel(inner);
            var screen = new TestScreen { Content = panel };
            engine.NavigateToScreen(screen);
            engine.Update(new GameTime());
            return (engine, screen, panel, inner);
        }

        [TestMethod]
        public void Desktop_reports_zero_and_nothing_shifts()
        {
            using var game = new Game();
            var (engine, _, _, inner) = Create(game);

            Assert.AreEqual(default(Thickness), engine.SafeAreaInsets);
            Assert.AreEqual(new Rect(0, 0, 400, 800), inner.ClippingRect);
        }

        [TestMethod]
        public void Panel_pads_by_the_system_insets_after_the_next_update()
        {
            using var game = new Game();
            var (engine, _, panel, inner) = Create(game);
            var raised = 0;
            engine.SafeAreaChanged += (s, e) => raised++;

            engine.SetSystemInsets(new Thickness(0, 40, 0, 20));
            Assert.AreEqual(0, raised, "applied on the game thread, not in the setter");
            engine.Update(new GameTime());

            Assert.AreEqual(1, raised);
            Assert.AreEqual(new Rect(0, 40, 400, 740), inner.ClippingRect);
            Assert.AreEqual(40, panel.AppliedInsets.Top);
        }

        [TestMethod]
        public void Keyboard_raises_the_bottom_inset_and_the_layout_follows()
        {
            using var game = new Game();
            var (engine, _, _, inner) = Create(game);
            engine.SetSystemInsets(new Thickness(0, 0, 0, 20));
            engine.Update(new GameTime());

            engine.SetKeyboardInset(300);
            engine.Update(new GameTime());
            Assert.AreEqual(300, engine.KeyboardInset);
            Assert.AreEqual(500, inner.ClippingRect.Bottom);

            engine.SetKeyboardInset(0);
            engine.Update(new GameTime());
            Assert.AreEqual(780, inner.ClippingRect.Bottom, "back to the navigation bar inset");
        }

        [TestMethod]
        public void Only_the_overlapping_part_of_an_inset_counts_and_edges_can_be_excluded()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 800);
            var inner = new Border();
            var panel = new SafeAreaPanel(inner) { Margin = new Thickness(0, 30, 0, 0), Edges = SafeAreaEdges.Top | SafeAreaEdges.Left };
            var screen = new TestScreen { Content = panel };
            engine.NavigateToScreen(screen);
            engine.SetSystemInsets(new Thickness(0, 40, 0, 20));
            engine.Update(new GameTime());

            Assert.AreEqual(10, panel.AppliedInsets.Top, "the margin already clears 30 of the 40");
            Assert.AreEqual(0, panel.AppliedInsets.Bottom, "bottom edge excluded");
        }
    }
}
