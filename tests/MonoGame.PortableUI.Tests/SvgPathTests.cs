using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class SvgPathTests
    {
        // Material Symbols Rounded "add" as Google ships it.
        private const string Add = "M440-440H240q-17 0-28.5-11.5T200-480q0-17 11.5-28.5T240-520h200v-200q0-17 11.5-28.5T480-760q17 0 28.5 11.5T520-720v200h200q17 0 28.5 11.5T760-480q0 17-11.5 28.5T720-440H520v200q0 17-11.5 28.5T480-200q-17 0-28.5-11.5T440-240v-200Z";

        private static float Alpha(Color[] pixels, int size, int x, int y) => pixels[y * size + x].A / 255f;

        [TestMethod]
        public void A_material_symbol_fills_its_shape_in_a_shifted_view_box()
        {
            var plus = PathGeometry.FromSvg(Add, new RectangleF(0, -960, 960, 960));

            var pixels = PathRasterizer.Rasterize(plus, 24, 24, Color.White);

            Assert.AreEqual(1, Alpha(pixels, 24, 12, 12), 0.01f, "the middle of the plus");
            Assert.AreEqual(1, Alpha(pixels, 24, 6, 12), 0.01f, "its left arm");
            Assert.AreEqual(0, Alpha(pixels, 24, 7, 7), 0.01f, "between the arms");
            Assert.AreEqual(0, Alpha(pixels, 24, 1, 1), 0.01f, "the corner");
        }

        [TestMethod]
        public void A_figure_wound_the_other_way_cuts_a_hole_with_the_non_zero_rule()
        {
            // Outer square clockwise, inner square counter-clockwise: the inside of the inner one stays empty.
            var frame = PathGeometry.FromSvg("M0 0H24V24H0Z M6 6V18H18V6Z", 24, 24);

            var pixels = PathRasterizer.Rasterize(frame, 24, 24, Color.White);

            Assert.AreEqual(1, Alpha(pixels, 24, 2, 12), 0.01f, "the frame");
            Assert.AreEqual(0, Alpha(pixels, 24, 12, 12), 0.01f, "the hole");
        }

        [TestMethod]
        public void Small_curves_are_flattened_with_few_points()
        {
            var plus = PathGeometry.FromSvg(Add, new RectangleF(0, -960, 960, 960));

            var points = 0;
            foreach (var figure in plus.Figures)
                points += figure.Count;
            Assert.IsTrue(points < 120, $"{points} points");
        }

        [TestMethod]
        public void Relative_lines_and_closing_build_a_square()
        {
            var square = PathGeometry.FromSvg("m2 2h20v20H2z", 24, 24);

            var figure = square.Figures[0];
            Assert.AreEqual(4, figure.Count);
            Assert.AreEqual(new Vector2(22, 22), figure[2]);
            Assert.IsTrue(square.IsClosed(0));
        }

        [TestMethod]
        public void Pairs_after_a_move_are_lines()
        {
            var path = PathGeometry.FromSvg("M0,0 10,0 10,10", 10, 10);

            Assert.AreEqual(1, path.Figures.Count);
            Assert.AreEqual(3, path.Figures[0].Count);
        }

        [TestMethod]
        public void An_arc_passes_through_the_far_side_of_its_circle()
        {
            // Half a circle of radius 10 from the left to the right point, sweeping through the top (y = 0).
            var arc = PathGeometry.FromSvg("M2 12A10 10 0 0 1 22 12", 24, 24);

            var points = arc.Figures[0];
            var top = float.MaxValue;
            foreach (var p in points)
                top = Math.Min(top, p.Y);
            Assert.AreEqual(2, top, 0.05f);
            Assert.AreEqual(new Vector2(22, 12), points[points.Count - 1]);
        }

        [TestMethod]
        public void Packed_arc_flags_and_numbers_are_read()
        {
            var arc = PathGeometry.FromSvg("M2 12a10 10 0 0120 0", 24, 24);

            var last = arc.Figures[0][arc.Figures[0].Count - 1];
            Assert.AreEqual(22, last.X, 0.001f);
            Assert.AreEqual(12, last.Y, 0.001f);
        }

        [TestMethod]
        public void Data_without_a_command_is_rejected()
        {
            Assert.ThrowsExactly<FormatException>(() => PathGeometry.FromSvg("10 10", 24, 24));
        }
    }
}
