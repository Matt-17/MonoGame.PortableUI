using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class TextFillTests
    {
        [TestMethod]
        public void ColorAt_interpolates_unsorted_stops_and_keeps_hard_steps()
        {
            var chrome = new LinearGradientBrush(
                new GradientStop(1, Color.Black),
                new GradientStop(0, Color.White),
                new GradientStop(0.5f, Color.Red),
                new GradientStop(0.5f, Color.Blue));

            Assert.AreEqual(Color.White, chrome.ColorAt(0));
            Assert.AreEqual(Color.Lerp(Color.White, Color.Red, 0.5f), chrome.ColorAt(0.25f));
            Assert.AreEqual(Color.Red, chrome.ColorAt(0.4999f), "just above the step: the upper colour band");
            Assert.AreEqual(Color.Blue, chrome.ColorAt(0.5f), "from the step on: the lower colour band");
            Assert.AreEqual(Color.Black, chrome.ColorAt(2f));
        }

        [TestMethod]
        public void Bands_cover_the_clip_contiguously_with_one_pixel_steps_on_small_text()
        {
            // A 40 px fill span starting at y = 110 inside a scissor of 100..160.
            const int bands = 40;
            var expectedLow = 100;
            for (var band = -1; band <= bands; band++)
            {
                if (!TextBlock.FillBand(band, bands, 110, 40, 100, 160, out var low, out var high, out var t))
                    continue;
                Assert.AreEqual(expectedLow, low, $"band {band} starts where the previous ended");
                expectedLow = high;
                if (band >= 0 && band < bands)
                {
                    Assert.AreEqual(1, high - low);
                    Assert.AreEqual((band + 0.5f) / bands, t, 1e-6f);
                }
            }
            Assert.AreEqual(160, expectedLow);
        }

        [TestMethod]
        public void Bands_outside_the_scissor_are_skipped()
        {
            Assert.IsFalse(TextBlock.FillBand(0, 10, 0, 10, 50, 100, out _, out _, out _));
            Assert.IsTrue(TextBlock.FillBand(10, 10, 0, 10, 50, 100, out var low, out var high, out var t));
            Assert.AreEqual((50, 100, 1f), (low, high, t));
        }
    }
}
