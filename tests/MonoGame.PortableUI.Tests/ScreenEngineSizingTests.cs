using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ScreenEngineSizingTests
    {
        [TestMethod]
        public void Viewport_mode_applies_initial_size_and_invalidates_active_screen()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            var screen = new CountingScreen();
            engine.NavigateToScreen(screen);

            var changed = engine.ApplyViewportSize(1180, 760);

            Assert.IsTrue(changed);
            Assert.AreEqual(1180, engine.ScreenRect.Width);
            Assert.AreEqual(760, engine.ScreenRect.Height);
            Assert.AreEqual(1, screen.LayoutInvalidations);
        }

        [TestMethod]
        public void Viewport_mode_skips_redundant_same_size_updates()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            var screen = new CountingScreen();
            engine.NavigateToScreen(screen);
            engine.ApplyViewportSize(1180, 760);
            screen.ResetInvalidations();

            var changed = engine.ApplyViewportSize(1180, 760);

            Assert.IsFalse(changed);
            Assert.AreEqual(0, screen.LayoutInvalidations);
        }

        [TestMethod]
        public void Viewport_mode_invalidates_when_size_changes()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            var screen = new CountingScreen();
            engine.NavigateToScreen(screen);
            engine.ApplyViewportSize(1180, 760);
            screen.ResetInvalidations();

            var changed = engine.ApplyViewportSize(1200, 800);

            Assert.IsTrue(changed);
            Assert.AreEqual(1200, engine.ScreenRect.Width);
            Assert.AreEqual(800, engine.ScreenRect.Height);
            Assert.AreEqual(1, screen.LayoutInvalidations);
        }

        [TestMethod]
        public void Manual_mode_ignores_automatic_viewport_updates()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions
            {
                AddComponentToGame = false,
                ScreenSizeMode = ScreenSizeMode.Manual
            });
            var screen = new CountingScreen();
            engine.NavigateToScreen(screen);
            engine.SetScreenSize(320, 200);
            screen.ResetInvalidations();

            var changed = engine.ApplyViewportSize(1180, 760);

            Assert.IsFalse(changed);
            Assert.AreEqual(320, engine.ScreenRect.Width);
            Assert.AreEqual(200, engine.ScreenRect.Height);
            Assert.AreEqual(0, screen.LayoutInvalidations);
        }

        [TestMethod]
        public void Debug_overlay_toggle_flips_overlay_state()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });

            Assert.IsFalse(engine.DebugOverlayEnabled);

            engine.ToggleDebugOverlay();

            Assert.IsTrue(engine.DebugOverlayEnabled);

            engine.ToggleDebugOverlay();

            Assert.IsFalse(engine.DebugOverlayEnabled);
        }

        private sealed class CountingScreen : Screen
        {
            public int LayoutInvalidations { get; private set; }

            public void ResetInvalidations()
            {
                LayoutInvalidations = 0;
            }

            public override void InvalidateLayout(bool boundsChanged)
            {
                LayoutInvalidations++;
            }
        }
    }
}
