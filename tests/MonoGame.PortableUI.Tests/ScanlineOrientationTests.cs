using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Effects;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ScanlineOrientationTests
    {
        [TestMethod]
        public void Scanlines_are_horizontal_by_default()
        {
            Assert.AreEqual(ScanlineOrientation.Horizontal, new ScanlinePostEffect().Orientation);
        }

        [TestMethod]
        public void Overlay_tile_runs_along_the_chosen_direction()
        {
            var horizontal = PostProcessManager.CreateScanlinePattern(3, false, out var hw, out var hh);
            Assert.AreEqual((1, 3), (hw, hh), "one dark row every three pixels");
            var vertical = PostProcessManager.CreateScanlinePattern(3, true, out var vw, out var vh);
            Assert.AreEqual((3, 1), (vw, vh), "one dark column every three pixels");

            foreach (var pattern in new[] { horizontal, vertical })
            {
                CollectionAssert.AreEqual(new[] { Color.Transparent, Color.Transparent, new Color(0, 0, 0, 255) }, pattern);
            }
        }
    }
}
