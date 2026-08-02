using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class FontManagerRegressionTests
    {
        [TestMethod]
        public void Content_asset_probe_finds_compiled_xnb_without_loading_missing_assets()
        {
            var contentRoot = Path.Combine(Path.GetTempPath(), "MonoGame.PortableUI.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(contentRoot, "Fonts"));

            try
            {
                File.WriteAllBytes(Path.Combine(contentRoot, "Fonts", "Segoe-regular-14.xnb"), Array.Empty<byte>());

                Assert.IsTrue(FontManager.ContentAssetExists(contentRoot, "Fonts/Segoe-regular-14"));
                Assert.IsFalse(FontManager.ContentAssetExists(contentRoot, "Fonts/Segoe-bold-14"));
            }
            finally
            {
                Directory.Delete(contentRoot, recursive: true);
            }
        }

        [TestMethod]
        public void Content_root_resolution_uses_base_directory_for_relative_roots()
        {
            var baseDirectory = Path.Combine(Path.GetTempPath(), "MonoGame.PortableUI.Tests", Guid.NewGuid().ToString("N"));
            var contentRoot = FontManager.ResolveContentRoot("Content", baseDirectory);

            Assert.AreEqual(Path.GetFullPath(Path.Combine(baseDirectory, "Content")), contentRoot);
        }

        [TestMethod]
        public void Font_asset_keys_keep_registered_family_name_and_lowercase_style()
        {
            var key = FontManager.CreateFontKey("Segoe", FontStyle.BoldItalic, 16);

            Assert.AreEqual("Segoe-bolditalic-16", key);
        }
    }
}
