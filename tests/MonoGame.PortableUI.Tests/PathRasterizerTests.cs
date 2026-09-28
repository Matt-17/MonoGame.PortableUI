using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class PathRasterizerTests
    {
        private static PathGeometry Square() => new PathGeometry(10, 10)
            .MoveTo(2, 2).LineTo(8, 2).LineTo(8, 8).LineTo(2, 8).Close();

        [TestMethod]
        public void Fill_covers_the_inside_and_leaves_the_outside_transparent()
        {
            var pixels = PathRasterizer.Rasterize(Square(), 10, 10, Color.White);

            Assert.AreEqual(255, pixels[5 * 10 + 5].A, "centre is fully covered");
            Assert.AreEqual(0, pixels[0].A, "corner outside the square is empty");
            Assert.AreEqual(0, pixels[9 * 10 + 9].A);
        }

        [TestMethod]
        public void Rasterizing_at_a_larger_size_keeps_edges_one_pixel_soft()
        {
            var small = PathRasterizer.Rasterize(Square(), 10, 10, Color.White);
            var large = PathRasterizer.Rasterize(Square(), 100, 100, Color.White);

            // The square edge sits at 20 % of the width at both sizes: pixel 19 empty, 21 full.
            Assert.AreEqual(0, large[50 * 100 + 19].A);
            Assert.AreEqual(255, large[50 * 100 + 21].A);
            Assert.AreEqual(255, small[5 * 10 + 2].A);
        }

        [TestMethod]
        public void Even_odd_fill_leaves_a_hole_where_figures_overlap()
        {
            var ring = new PathGeometry(20, 20) { EvenOddFill = true }
                .AddEllipse(10, 10, 9, 9)
                .AddEllipse(10, 10, 4, 4);
            var nonZero = new PathGeometry(20, 20)
                .AddEllipse(10, 10, 9, 9)
                .AddEllipse(10, 10, 4, 4);

            Assert.AreEqual(0, PathRasterizer.Rasterize(ring, 20, 20, Color.White)[10 * 20 + 10].A);
            Assert.AreEqual(255, PathRasterizer.Rasterize(ring, 20, 20, Color.White)[10 * 20 + 16].A);
            Assert.AreEqual(255, PathRasterizer.Rasterize(nonZero, 20, 20, Color.White)[10 * 20 + 10].A);
        }

        [TestMethod]
        public void Stroke_draws_the_outline_without_filling()
        {
            var pixels = PathRasterizer.Rasterize(Square(), 10, 10, fill: null, stroke: Color.Red, strokeWidth: 1);

            Assert.IsTrue(pixels[2 * 10 + 5].A > 0, "top edge is stroked");
            Assert.AreEqual(0, pixels[5 * 10 + 5].A, "inside stays empty");
        }

        [TestMethod]
        public void Colours_are_premultiplied()
        {
            var pixels = PathRasterizer.Rasterize(Square(), 10, 10, new Color(255, 0, 0, 128));

            var centre = pixels[5 * 10 + 5];
            Assert.AreEqual(128, centre.A);
            Assert.IsTrue(centre.R <= 129, $"red premultiplied by alpha, was {centre.R}");
        }
    }
}
