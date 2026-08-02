using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Tests.Common
{
    [TestClass]
    public class CornerRadiusTests
    {
        [TestMethod]
        public void Uniform_corner_radius_populates_all_corners()
        {
            CornerRadius radius = 6f;

            Assert.AreEqual(6, radius.TopLeft);
            Assert.AreEqual(6, radius.TopRight);
            Assert.AreEqual(6, radius.BottomRight);
            Assert.AreEqual(6, radius.BottomLeft);
            Assert.IsTrue(radius.IsUniform);
        }

        [TestMethod]
        public void Corner_radius_clamps_negative_values_to_zero()
        {
            var radius = new CornerRadius(-1, 2, -3, 4);

            Assert.AreEqual(0, radius.TopLeft);
            Assert.AreEqual(2, radius.TopRight);
            Assert.AreEqual(0, radius.BottomRight);
            Assert.AreEqual(4, radius.BottomLeft);
            Assert.IsFalse(radius.IsEmpty);
        }
    }
}
