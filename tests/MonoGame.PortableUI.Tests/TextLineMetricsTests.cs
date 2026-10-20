using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class TextLineMetricsTests
    {
        private static FontStashUIFont Load(string file, TextLineMetrics metrics)
            => FontStashUIFont.FromData(16, metrics, File.ReadAllBytes(Path.Combine(System.AppContext.BaseDirectory, "Fonts", file)));

        [TestInitialize]
        public void NoEngine() => ScreenEngine.Instance?.Dispose();

        [TestMethod]
        public void Android_lines_round_ascent_and_descent_up_to_whole_pixels()
        {
            // Atkinson Hyperlegible: hhea 950/-290 per 1000 em is larger than its glyph box (888/-250).
            // Size 12 pt = 16 px em: top 15.2 -> 16, bottom 4.64 -> 5.
            var android = Load("AtkinsonHyperlegible-Regular.ttf", TextLineMetrics.Android);
            var font = Load("AtkinsonHyperlegible-Regular.ttf", TextLineMetrics.Font);

            Assert.AreEqual(21f, android.GetLineHeight(12), 0.001f);
            var unrounded = font.GetLineHeight(12);
            Assert.AreNotEqual(System.MathF.Round(unrounded), unrounded, "the default keeps FontStashSharp's unrounded line");
        }

        [TestMethod]
        public void Android_lines_include_the_glyph_box_when_it_is_taller_than_hhea()
        {
            // Roboto: hhea 1900/-500 per 2048 em, glyph box 2163/-555 (includeFontPadding uses the box).
            // 16 px em: top 16.9 -> 17, bottom 4.34 -> 5.
            var android = Load("Roboto-Variable.ttf", TextLineMetrics.Android);

            Assert.AreEqual(22f, android.GetLineHeight(12), 0.001f);
        }

        [TestMethod]
        public void A_size_scaled_font_keeps_its_line_metrics()
        {
            var android = Load("AtkinsonHyperlegible-Regular.ttf", TextLineMetrics.Android).WithSizeScale(0.75f);

            // 12 pt * 0.75 = 12 px em: top 11.4 -> 12, bottom 3.48 -> 4.
            Assert.AreEqual(16f, android.GetLineHeight(12), 0.001f);
        }

        [TestMethod]
        public void Wrapped_android_lines_are_spaced_by_the_rounded_hhea_metrics()
        {
            // 16 px em: round(15.2) + round(4.64) = 15 + 5 = 20 between baselines; the single line's box is 21.
            var android = Load("AtkinsonHyperlegible-Regular.ttf", TextLineMetrics.Android);

            Assert.AreEqual(20f, android.GetLineSpacing(12), 0.001f);
            Assert.AreEqual(21f, android.GetLineHeight(12), 0.001f);
        }
    }
}
