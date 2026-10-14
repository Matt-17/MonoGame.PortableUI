using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>Runtime TTF fonts for themes: the font registry, em sizing and crisp pixel fonts.</summary>
    [TestClass]
    public class PixelFontTests
    {
        private static string FontPath(string file)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "samples", "MonoGame.PortableUI.Demo", "Content", "Fonts", file)))
                directory = directory.Parent;
            if (directory == null)
                Assert.Inconclusive("bundled fonts not found");
            return Path.Combine(directory!.FullName, "samples", "MonoGame.PortableUI.Demo", "Content", "Fonts", file);
        }

        private static byte[] Font(string file) => File.ReadAllBytes(FontPath(file));

        [TestCleanup]
        public void Cleanup()
        {
            FontManager.UnregisterDynamicFont("testfont");
            FontManager.UnregisterDynamicFont("missingfont");
        }

        [TestMethod]
        public void Pixel_fonts_are_recognized_by_their_outline_grid()
        {
            Assert.AreEqual(8f, PixelFontDetector.DetectGrid(Font("PressStart2P-Regular.ttf")), 0.001f);
            Assert.AreEqual(16f, PixelFontDetector.DetectGrid(Font("Px437_IBM_VGA_8x16.ttf")), 0.001f);
            Assert.AreEqual(20f, PixelFontDetector.DetectGrid(Font("Jersey10-Regular.ttf")), 0.001f);
            Assert.AreEqual(10.24f, PixelFontDetector.DetectGrid(Font("Silkscreen-Regular.ttf")), 0.001f);
            // Outline fonts, and VT323 with its smoothed diagonals, are not.
            Assert.AreEqual(0f, PixelFontDetector.DetectGrid(Font("VT323-Regular.ttf")));
            Assert.AreEqual(0f, PixelFontDetector.DetectGrid(Font("Selawik-Regular.ttf")));
            Assert.AreEqual(0f, PixelFontDetector.DetectGrid(Font("Roboto-Variable.ttf")));
            Assert.AreEqual(0f, PixelFontDetector.DetectGrid(new byte[] { 1, 2, 3 }), "garbage is not a font");
        }

        [TestMethod]
        public void Em_sizes_convert_to_the_full_height_FontStash_sizes_by()
        {
            Assert.AreEqual(1.2002f, PixelFontDetector.HeightPerEm(Font("Selawik-Regular.ttf")), 0.001f);
            Assert.AreEqual(1f, PixelFontDetector.HeightPerEm(Font("PressStart2P-Regular.ttf")), 0.001f);
            Assert.AreEqual(1f, PixelFontDetector.HeightPerEm(new byte[] { 1, 2, 3 }));
        }

        [TestMethod]
        public void A_pixel_font_measures_only_at_whole_multiples_of_its_grid()
        {
            var font = FontStashUIFont.FromData(14, Font("PressStart2P-Regular.ttf"));
            Assert.AreEqual(8f, font.PixelGrid, 0.001f);
            foreach (var size in new[] { 6f, 11f, 14f, 17f, 30f })
            {
                var lineHeight = font.GetLineHeight(size);
                Assert.AreEqual(0, lineHeight % 8, 0.001, $"size {size}: line height {lineHeight}");
                Assert.IsTrue(lineHeight >= 8);
            }
        }

        [TestMethod]
        public void A_regular_font_is_sized_like_a_SpriteFont_em()
        {
            var font = FontStashUIFont.FromData(14, Font("Selawik-Regular.ttf"));
            Assert.AreEqual(0f, font.PixelGrid);
            // 14 pt em = 18.67 px; the full height (ascent to descent) is 1.2 em.
            Assert.AreEqual(14 * 96f / 72f * 1.2002f, font.GetLineHeight(14), 0.6f);
        }

        [TestMethod]
        public void A_theme_draws_with_the_runtime_font_registered_for_its_font_name()
        {
            var typography = new Typography { FontName = "testfont" };
            Assert.IsNull(typography.ResolveDynamicFont(), "nothing registered: the SpriteFont path");

            FontManager.RegisterDynamicFont("testfont", () => File.OpenRead(FontPath("Selawik-Regular.ttf")));
            var font = typography.ResolveDynamicFont();
            Assert.IsInstanceOfType(font, typeof(FontStashUIFont));
            Assert.AreSame(font, new Typography { FontName = "TESTFONT" }.ResolveDynamicFont(), "names ignore case; loaded once");

            var own = FontStashUIFont.FromData(14, Font("Roboto-Variable.ttf"));
            typography.DynamicFont = own;
            Assert.AreSame(own, typography.ResolveDynamicFont(), "the theme's own font wins");

            typography.DynamicFont = null;
            FontManager.UnregisterDynamicFont("testfont");
            Assert.IsNull(typography.ResolveDynamicFont(), "unregistered: back to the SpriteFont");
        }

        [TestMethod]
        public void A_font_that_cannot_be_loaded_falls_back_to_the_SpriteFont()
        {
            FontManager.RegisterDynamicFont("missingfont", () => throw new FileNotFoundException("gone"));
            Assert.IsNull(FontManager.GetDynamicFont("missingfont"));
            Assert.IsNull(new Typography { FontName = "missingfont" }.ResolveDynamicFont());
        }

        [TestMethod]
        public void A_size_scale_matches_a_SpriteFont_baked_at_another_size()
        {
            FontManager.RegisterDynamicFont("testfont", () => File.OpenRead(FontPath("Selawik-Regular.ttf")), sizeScale: 2);
            var scaled = FontManager.GetDynamicFont("testfont")!;
            var plain = FontStashUIFont.FromData(14, Font("Selawik-Regular.ttf"));
            Assert.AreEqual(plain.GetLineHeight(28), scaled.GetLineHeight(14), 0.6f);
        }
    }
}
