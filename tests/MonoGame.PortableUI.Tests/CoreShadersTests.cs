using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Effects;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class CoreShadersTests
    {
        [TestMethod]
        [DataRow(CoreShaders.ProgressiveBlur)]
        [DataRow(CoreShaders.Blur)]
        [DataRow(CoreShaders.LiquidGlass)]
        public void Shaders_of_core_visuals_ship_inside_the_core(string name)
        {
            // Core visuals must not depend on an optional package being loaded.
            using var stream = typeof(CoreShaders).Assembly.GetManifestResourceStream($"MonoGame.PortableUI.Shaders.{name}.ogl.mgfxo");

            Assert.IsNotNull(stream);
            Assert.IsTrue(stream.Length > 0);
        }

        [TestMethod]
        public void Glass_brushes_live_in_the_core_assembly()
        {
            Assert.AreSame(typeof(CoreShaders).Assembly, typeof(Media.FrostedGlassBrush).Assembly);
            Assert.AreSame(typeof(CoreShaders).Assembly, typeof(BackdropRenderer).Assembly);
        }
    }
}
