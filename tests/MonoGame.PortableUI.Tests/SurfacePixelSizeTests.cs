using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class SurfacePixelSizeTests
    {
        private sealed class TestScreen : Screen
        {
        }

        [TestMethod]
        public void SetPixelSize_matches_the_window_and_lays_out_at_the_design_height()
        {
            using var game = new Game();
            var content = new Border();
            using var surface = new UISurface(game, new TestScreen { Content = content }, 100, 100);

            surface.SetPixelSize(1366, 768, 768 / 1080f);
            surface.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(16)));

            Assert.AreEqual(new Point(1366, 768), surface.PixelSize);
            Assert.AreEqual(1080f, surface.LayoutSize.Y, 0.001f);
            Assert.AreEqual(1366 * 1080 / 768f, surface.LayoutSize.X, 0.01f);
            Assert.AreEqual(768 / 1080f, surface.LayoutScale, 1e-6f);
            Assert.AreEqual(surface.LayoutSize.X, content.BoundingRect.Width, 0.01f, "content fills the fractional width");
        }

        [TestMethod]
        public void Resize_and_LayoutScale_return_to_derived_pixel_sizes()
        {
            using var game = new Game();
            using var surface = new UISurface(game, new TestScreen(), 100, 100);
            surface.SetPixelSize(1366, 768, 0.5f);
            Assert.AreEqual(new Point(1366, 768), surface.PixelSize);

            surface.Resize(300, 200);
            Assert.AreEqual(new Point(150, 100), surface.PixelSize);

            surface.SetPixelSize(1366, 768, 0.5f);
            surface.LayoutScale = 2f;
            Assert.AreEqual(new Point(5464, 3072), surface.PixelSize, "layout size kept, pixels derived again");
        }
    }
}
