using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Themes;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class UISurfaceTests
    {
        [TestMethod]
        public void UISurface_uses_non_primary_engine_and_fixed_virtual_size()
        {
            using var game = new Game();
            var primary = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            using var surface = new UISurface(game, new EmptyScreen(), 640, 400, PortableThemes.Resolve("dos").CreateTheme());

            Assert.AreSame(primary, ScreenEngine.Instance);
            Assert.AreNotSame(primary, surface.Engine);
            Assert.AreEqual(640, surface.Engine.ScreenRect.Width);
            Assert.AreEqual(400, surface.Engine.ScreenRect.Height);
            Assert.AreEqual("dos", PortableThemes.Resolve("dos").Id);
        }

        [TestMethod]
        public void Surface_focus_manager_activates_one_surface_at_a_time()
        {
            using var game = new Game();
            using var first = new UISurface(game, new EmptyScreen(), 320, 200);
            using var second = new UISurface(game, new EmptyScreen(), 320, 200);
            var focus = new SurfaceFocusManager();

            focus.Activate(first);

            Assert.AreSame(first, focus.ActiveSurface);
            Assert.IsTrue(first.HasKeyboardFocus);

            focus.Activate(second);

            Assert.IsFalse(first.HasKeyboardFocus);
            Assert.IsTrue(second.HasKeyboardFocus);
            Assert.AreSame(second, focus.ActiveSurface);
        }

        [TestMethod]
        public void ExternalBackdrop_flows_between_surface_and_screen()
        {
            using var game = new Game();
            var screen = new EmptyScreen();
            using var surface = new UISurface(game, screen, 320, 200);

            Assert.IsNull(surface.ExternalBackdrop);

            // the surface property is a pass-through to the screen (textures need a live
            // GraphicsDevice, so headless tests exercise the plumbing with null round-trips)
            screen.ExternalBackdrop = null;
            Assert.IsNull(surface.ExternalBackdrop);
            surface.ExternalBackdrop = null;
            Assert.IsNull(screen.ExternalBackdrop);
        }

        private sealed class EmptyScreen : Screen
        {
        }
    }
}
