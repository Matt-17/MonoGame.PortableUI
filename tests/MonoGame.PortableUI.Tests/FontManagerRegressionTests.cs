using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

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

        [TestMethod]
        public void Characters_outside_an_ascii_font_measure_as_the_fallback_glyph_instead_of_throwing()
        {
            var font = CreateAsciiFont();
            Assert.ThrowsExactly<ArgumentException>(() => font.MeasureString("ä"));

            var block = new TextBlock { FontOverride = font };
            block.Text = "Grüße €";

            Assert.AreEqual('?', font.DefaultCharacter);
            Assert.AreEqual(font.MeasureString("Gr??e ?"), font.MeasureString("Grüße €"));
            Assert.AreEqual("Grüße €", block.Text);
        }

        [TestMethod]
        public void Existing_default_character_is_kept()
        {
            var font = CreateAsciiFont(defaultCharacter: '*');

            FontManager.EnsureFallbackCharacter(font);

            Assert.AreEqual('*', font.DefaultCharacter);
        }

        private static SpriteFont CreateAsciiFont(char? defaultCharacter = null)
        {
            var glyphs = new List<Rectangle>();
            var cropping = new List<Rectangle>();
            var characters = new List<char>();
            var kerning = new List<Vector3>();
            for (var c = (char)32; c <= 126; c++)
            {
                glyphs.Add(new Rectangle(0, 0, 8, 14));
                cropping.Add(new Rectangle(0, 0, 8, 14));
                characters.Add(c);
                kerning.Add(new Vector3(0, 8, 0));
            }

            return new SpriteFont(null, glyphs, cropping, characters, 14, 0, kerning, defaultCharacter);
        }

        [TestMethod]
        public void Missing_fonts_degrade_to_the_default_font_instead_of_throwing()
        {
            Assert.IsFalse(FontManager.TryGetFont("definitely-not-a-font", out var missing));
            Assert.IsNull(missing);

            // No exception; themes with unbuilt fonts keep rendering with the default font.
            Assert.AreSame(FontManager.DefaultFont, FontManager.GetFontOrDefault("definitely-not-a-font"));
        }
    }
}
