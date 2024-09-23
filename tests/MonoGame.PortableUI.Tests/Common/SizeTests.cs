using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Tests.Common
{
    [TestClass]
    public class SizeTests
    {
        [TestMethod]
        public void ConstructorTest()
        {
            Assert.AreEqual(new Size().Height, 0);
            Assert.AreEqual(new Size().Width, 0);
            Assert.AreEqual(new Size(10, 19).Height, 19);
            Assert.AreEqual(new Size(10, 19).Width, 10);
            var size = new Size(7, 7);
            Assert.AreEqual(size.Height, size.Width);
        }

        [TestMethod]
        public void ComparisonTest()
        {
            Assert.AreEqual(new Size(), Size.Empty);
            Assert.AreEqual(new Size(), new Size(0, 0));
            var size1 = new Size(1, 2);
            var size2 = new Size(1, 2);
            Assert.AreEqual(size1, size2);
        }

        [TestMethod]
        public void AdditionTest()
        {
            var size1 = new Size(5, 7);
            var size2 = new Size(13, 17);
            var addedSize = size1 + size2;
            var subtractedSize1 = size1 - size2;
            var subtractedSize2 = size2 - size1;
            Assert.AreEqual(addedSize, new Size(18, 24));
            Assert.AreEqual(subtractedSize1, new Size(0, 0));
            Assert.AreEqual(subtractedSize2, new Size(8, 10));
        }

        [TestMethod]
        public void ScalingTest()
        {
            var size1 = new Size(25, 15);
            var multipliedSize1 = size1 * 3;
            var multipliedSize2 = 5 * size1;
            var dividedSize1 = size1 / 5;
            //var dividedSize2 = 5 / size1; // May not work
            Assert.AreEqual(multipliedSize1, new Size(75, 45));
            Assert.AreEqual(multipliedSize2, new Size(125, 75));
            Assert.AreEqual(dividedSize1, new Size(5, 3));
        }

        [TestMethod]
        public void RectConverterTest()
        {
            var rect = new Rect(25, 15, 25, 15);
            Size size = (Size)rect;
            Assert.AreEqual(size, new Size(25, 15)); // explicit
            var expected = (Rect)size;
            Assert.AreEqual(expected, new Size(25, 15)); // implicit / explicit
            
        }
    }
}
