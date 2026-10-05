using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Effects;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>#96: post effects and glass live in the optional effects package; the core only keeps
    /// the PostEffect base, the effect lists and the renderer hooks.</summary>
    [TestClass]
    public class EffectsPackageTests
    {
        [TestMethod]
        public void Using_anything_from_the_package_installs_its_renderers()
        {
            _ = new ScanlinePostEffect();

            Assert.IsNotNull(EffectRenderers.PostEffects);
            Assert.IsNotNull(EffectRenderers.Backdrop);
        }

        [TestMethod]
        public void The_core_maps_the_pointer_through_display_effects_without_knowing_their_types()
        {
            var screen = new Rect(0, 0, 400, 300);
            var barrel = new CrtBarrelPostEffect { Distortion = 0.1f };
            PostEffect[] effects = { new ScanlinePostEffect(), barrel };
            var corner = new PointF(20, 20);

            Assert.IsTrue(barrel.IsDisplayOnly);
            Assert.IsFalse(new ScanlinePostEffect().IsDisplayOnly);
            Assert.IsTrue(PostEffectGeometry.MovesPixels(effects));

            var ui = PostEffectGeometry.DisplayToUi(effects, corner, screen);
            Assert.AreEqual(PostProcessManager.InverseBarrel(corner, screen, 0.1f), ui);
            var back = PostEffectGeometry.UiToDisplay(effects, ui, screen);
            Assert.AreEqual(corner.X, back.X, 0.01f);
            Assert.AreEqual(corner.Y, back.Y, 0.01f);

            barrel.Enabled = false;
            Assert.IsFalse(PostEffectGeometry.MovesPixels(effects));
            Assert.AreEqual(corner, PostEffectGeometry.DisplayToUi(effects, corner, screen));
        }

        [TestMethod]
        public void A_theme_cannot_curve_the_picture()
        {
            PostEffect[] themeEffects = { new ScanlinePostEffect(), new CrtBarrelPostEffect() };

            var look = Screen.LookEffects(themeEffects);

            Assert.IsNotNull(look);
            Assert.AreEqual(1, look.Count);
            Assert.IsInstanceOfType(look[0], typeof(ScanlinePostEffect));
        }
    }
}
