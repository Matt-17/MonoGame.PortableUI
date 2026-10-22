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
