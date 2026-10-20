using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class RippleEffectTests
    {
        [TestMethod]
        public void Track_reports_only_the_edges_of_the_pressed_look()
        {
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(5);
            var ripple = new RippleEffect();
            var bounds = new Rect(0, 0, 100, 40);

            Assert.IsFalse(ripple.Track(false, bounds, new PointF(10, 10)), "not pressed before");
            Assert.IsTrue(ripple.Track(true, bounds, new PointF(10, 10)), "press starts the ripple");
            Assert.IsFalse(ripple.Track(true, bounds, new PointF(10, 10)), "a held press changes nothing");
            Assert.IsTrue(ripple.Track(false, bounds, new PointF(10, 10)), "release starts the fade");
        }

        [TestMethod]
        public void Toggle_ripples_are_off_by_default_with_a_material_radius()
        {
            Assert.IsNull(PortableTheme.CreateDefault().ToggleRipple);
            var style = new RippleStyle();
            Assert.AreEqual(20f, style.Radius);
            Assert.IsNull(style.CheckedColor);
        }
    }
}
