using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ImageCropTests
    {
        [TestMethod]
        public void Fitting_image_is_drawn_whole()
        {
            var source = new Rectangle(0, 0, 100, 50);
            var destination = new Rect(10, 10, 200, 100);
            Assert.IsTrue(Image.CropToBox(ref source, ref destination, new Rect(0, 0, 300, 300)));
            Assert.AreEqual(new Rectangle(0, 0, 100, 50), source);
            Assert.AreEqual(200f, destination.Width);
        }

        [TestMethod]
        public void UniformToFill_centred_crops_the_middle_band_of_a_sheet_cell()
        {
            // A 200x100 cell at (400, 0) in a sheet, filled into a 100x100 box: drawn 200x100, centred.
            var source = new Rectangle(400, 0, 200, 100);
            var destination = new Rect(-50, 0, 200, 100);
            Assert.IsTrue(Image.CropToBox(ref source, ref destination, new Rect(0, 0, 100, 100)));
            Assert.AreEqual(new Rectangle(450, 0, 100, 100), source);
            Assert.AreEqual(0f, destination.Left);
            Assert.AreEqual(100f, destination.Width);
        }

        [TestMethod]
        public void Top_aligned_crop_keeps_the_top_part()
        {
            // A coin drawn 2x (64x64 texels -> 128x128) in a 128x40 box aligned top: only the top 20 texel rows.
            var source = new Rectangle(0, 0, 64, 64);
            var destination = new Rect(0, 0, 128, 128);
            Assert.IsTrue(Image.CropToBox(ref source, ref destination, new Rect(0, 0, 128, 40)));
            Assert.AreEqual(new Rectangle(0, 0, 64, 20), source);
            Assert.AreEqual(40f, destination.Height);
        }

        [TestMethod]
        public void Image_outside_the_box_draws_nothing()
        {
            var source = new Rectangle(0, 0, 10, 10);
            var destination = new Rect(200, 200, 10, 10);
            Assert.IsFalse(Image.CropToBox(ref source, ref destination, new Rect(0, 0, 100, 100)));
        }
    }
}
