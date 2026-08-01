using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class BrushRegressionTests
    {
        [TestMethod]
        public void Gradient_brush_defaults_to_vertical_direction()
        {
            var brush = new GradientBrush(Color.Red, Color.Blue);

            Assert.AreEqual(Color.Red, brush.StartColor);
            Assert.AreEqual(Color.Blue, brush.EndColor);
            Assert.AreEqual(GradientDirection.Vertical, brush.Direction);
        }

        [TestMethod]
        public void Gradient_brush_direction_can_be_configured()
        {
            var brush = new GradientBrush(Color.Red, Color.Blue, GradientDirection.DiagonalUp);

            Assert.AreEqual(GradientDirection.DiagonalUp, brush.Direction);

            brush.Direction = GradientDirection.Horizontal;

            Assert.AreEqual(GradientDirection.Horizontal, brush.Direction);
        }

        [TestMethod]
        public void Frosted_glass_brush_defaults_to_translucent_tint()
        {
            var brush = new FrostedGlassBrush();

            Assert.IsTrue(brush.TintColor.A < byte.MaxValue);
            Assert.IsTrue(brush.SheenColor.A < byte.MaxValue);
            Assert.IsTrue(brush.BlurRadius > 0);
            Assert.IsTrue(brush.GrainOpacity > 0);
        }

        [TestMethod]
        public void Frosted_glass_brush_clamps_blur_and_grain()
        {
            var brush = new FrostedGlassBrush(Color.White, Color.White, -4, 2);

            Assert.AreEqual(0f, brush.BlurRadius, 0.001f);
            Assert.AreEqual(1f, brush.GrainOpacity, 0.001f);

            brush.BlurRadius = 40;
            brush.GrainOpacity = -0.5f;

            Assert.AreEqual(24f, brush.BlurRadius, 0.001f);
            Assert.AreEqual(0f, brush.GrainOpacity, 0.001f);
        }

        [TestMethod]
        public void Tile_brush_defaults_are_safe_for_unassigned_source()
        {
            var brush = new TileBrush();

            Assert.IsNull(brush.Source);
            Assert.AreEqual(1, brush.Scale);
            Assert.AreEqual(Color.White, brush.TintColor);
        }

        [TestMethod]
        public void Tile_brush_calculates_partial_edge_tiles()
        {
            var tiles = TileBrush.GetTileRects(new Rect(0, 0, 25, 15), 10, 10, 1).ToArray();

            Assert.AreEqual(6, tiles.Length);
            Assert.AreEqual(new Rect(0, 0, 10, 10), tiles[0]);
            Assert.AreEqual(new Rect(10, 0, 10, 10), tiles[1]);
            Assert.AreEqual(new Rect(20, 0, 5, 10), tiles[2]);
            Assert.AreEqual(new Rect(0, 10, 10, 5), tiles[3]);
            Assert.AreEqual(new Rect(10, 10, 10, 5), tiles[4]);
            Assert.AreEqual(new Rect(20, 10, 5, 5), tiles[5]);
        }

        [TestMethod]
        public void Tile_brush_clips_source_rectangles_for_partial_edge_tiles()
        {
            var tiles = TileBrush.GetTileSegments(new Rect(0, 0, 25, 15), 10, 10, 1).ToArray();

            Assert.AreEqual(new Rectangle(0, 0, 10, 10), tiles[0].SourceRectangle);
            Assert.AreEqual(new Rectangle(0, 0, 5, 10), tiles[2].SourceRectangle);
            Assert.AreEqual(new Rectangle(0, 0, 10, 5), tiles[3].SourceRectangle);
            Assert.AreEqual(new Rectangle(0, 0, 5, 5), tiles[5].SourceRectangle);
        }

        [TestMethod]
        public void Tile_brush_scale_changes_tile_size()
        {
            var tiles = TileBrush.GetTileRects(new Rect(0, 0, 30, 12), 10, 10, 0.5f).ToArray();

            Assert.AreEqual(new Rect(0, 0, 5, 5), tiles[0]);
            Assert.AreEqual(new Rect(25, 10, 5, 2), tiles[17]);
        }

        [TestMethod]
        public void Nine_tile_brush_defaults_to_center_stretch()
        {
            var brush = new NineTileBrush();
            var segments = NineTileBrush.GetSegments(new Rect(0, 0, 30, 20), 12, 8, brush.SliceMargins).ToArray();

            Assert.IsNull(brush.Source);
            Assert.AreEqual(Color.White, brush.TintColor);
            Assert.AreEqual(1, segments.Length);
            Assert.AreEqual(new Rectangle(0, 0, 12, 8), segments[0].SourceRectangle);
            Assert.AreEqual(new Rect(0, 0, 30, 20), segments[0].DestinationRect);
        }

        [TestMethod]
        public void Nine_tile_brush_calculates_stretched_center_segment()
        {
            var segments = NineTileBrush.GetSegments(new Rect(0, 0, 30, 20), 20, 10, new Thickness(4, 2, 6, 3)).ToArray();

            Assert.AreEqual(9, segments.Length);
            Assert.AreEqual(new Rectangle(0, 0, 4, 2), segments[0].SourceRectangle);
            Assert.AreEqual(new Rect(0, 0, 4, 2), segments[0].DestinationRect);
            Assert.AreEqual(new Rectangle(4, 2, 10, 5), segments[4].SourceRectangle);
            Assert.AreEqual(new Rect(4, 2, 20, 15), segments[4].DestinationRect);
        }

        [TestMethod]
        public void Nine_tile_brush_clamps_edge_slices_for_small_destinations()
        {
            var segments = NineTileBrush.GetSegments(new Rect(0, 0, 5, 4), 20, 10, new Thickness(4, 2, 6, 3)).ToArray();

            Assert.AreEqual(4, segments.Length);
            AssertRect(new Rect(0, 0, 2, 1.6f), segments[0].DestinationRect);
            AssertRect(new Rect(2, 0, 3, 1.6f), segments[1].DestinationRect);
            AssertRect(new Rect(0, 1.6f, 2, 2.4f), segments[2].DestinationRect);
            AssertRect(new Rect(2, 1.6f, 3, 2.4f), segments[3].DestinationRect);
        }

        [TestMethod]
        public void Brush_opacity_multiplies_alpha()
        {
            var color = Brush.ApplyOpacity(new Color(10, 20, 30, 200), 0.5f);

            Assert.AreEqual(new Color(10, 20, 30, 100), color);
        }

        private static void AssertRect(Rect expected, Rect actual)
        {
            Assert.AreEqual(expected.Left, actual.Left, 0.001f);
            Assert.AreEqual(expected.Top, actual.Top, 0.001f);
            Assert.AreEqual(expected.Width, actual.Width, 0.001f);
            Assert.AreEqual(expected.Height, actual.Height, 0.001f);
        }
    }
}
