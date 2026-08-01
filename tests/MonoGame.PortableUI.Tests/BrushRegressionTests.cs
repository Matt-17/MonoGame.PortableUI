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
    }
}
