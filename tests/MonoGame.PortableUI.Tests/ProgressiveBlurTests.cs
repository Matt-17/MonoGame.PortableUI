using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ProgressiveBlurTests
    {
        [TestMethod]
        public void Band_heights_and_radius_never_go_negative_or_invalid()
        {
            var blur = new ProgressiveBlur { TopHeight = -5, BottomHeight = float.NaN, MaxRadius = float.PositiveInfinity };

            Assert.AreEqual(0f, blur.TopHeight);
            Assert.AreEqual(0f, blur.BottomHeight);
            Assert.AreEqual(0f, blur.MaxRadius);
        }

        [TestMethod]
        public void Tint_opacity_is_clamped_to_one_and_defaults_to_none()
        {
            var blur = new ProgressiveBlur();
            Assert.AreEqual(0f, blur.TintOpacity);

            blur.TintOpacity = 3;
            Assert.AreEqual(1f, blur.TintOpacity);
        }

        [TestMethod]
        public void The_tint_follows_the_bands_until_it_gets_its_own_height()
        {
            var blur = new ProgressiveBlur();
            Assert.IsTrue(float.IsNaN(blur.TopTintHeight));
            Assert.IsTrue(float.IsNaN(blur.BottomTintHeight));
            Assert.IsFalse(blur.HasOwnTint);

            blur.TopTintHeight = 120;
            blur.BottomTintHeight = -4;
            blur.TintSolidHeight = -1;
            Assert.IsTrue(blur.HasOwnTint);
            Assert.AreEqual(120f, blur.TopTintHeight);
            Assert.AreEqual(0f, blur.BottomTintHeight);
            Assert.AreEqual(0f, blur.TintSolidHeight);

            blur.TopTintHeight = float.NaN;
            blur.BottomTintHeight = float.NaN;
            Assert.IsFalse(blur.HasOwnTint);
        }

        [TestMethod]
        public void It_lays_out_like_a_plain_content_control()
        {
            var content = new Border();
            var blur = new ProgressiveBlur { TopHeight = 20, BottomHeight = 20, Content = content };
            blur.UpdateLayout(new Rect(0, 0, 200, 300));

            Assert.AreEqual(blur.ClippingRect, content.BoundingRect);
            Assert.IsFalse(blur.IsFocusable);
        }
    }
}
