using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Effects;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class CoreShadersTests
    {
        [TestMethod]
        public void The_progressive_blur_shader_ships_inside_the_core()
        {
            // Core visuals must not depend on an optional package being loaded.
            using var stream = typeof(CoreShaders).Assembly.GetManifestResourceStream($"MonoGame.PortableUI.Shaders.{CoreShaders.ProgressiveBlur}.ogl.mgfxo");

            Assert.IsNotNull(stream);
            Assert.IsTrue(stream.Length > 0);
        }
    }
}
