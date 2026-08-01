using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
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
    }
}
