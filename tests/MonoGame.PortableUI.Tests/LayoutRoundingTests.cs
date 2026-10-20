using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class LayoutRoundingTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private const float Density = 2.75f;

        private static (Border Line, Border Below) Layout(Game game, bool rounding)
        {
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, UseLayoutRounding = rounding });
            engine.SetNativeRenderScale(Density);
            engine.SetScreenSize(392, 800);
            var line = new Border { Height = 1 };
            var below = new Border { Height = 20 };
            var screen = new TestScreen
            {
                Content = new StackPanel { Margin = new Thickness(0, 163.3f, 0, 0), Children = { line, below } }
            };
            engine.NavigateToScreen(screen);
            screen.PerformLayoutIfDirty();
            return (line, below);
        }

        private static bool IsWholePixel(float layoutUnits) => Math.Abs(layoutUnits * Density - MathF.Round(layoutUnits * Density)) < 0.001f;

        [TestMethod]
        public void A_one_dp_line_is_three_pixels_at_density_2_75()
        {
            using var game = new Game();
            var (line, below) = Layout(game, rounding: true);

            Assert.AreEqual(3f, line.ClippingRect.Height * Density, 0.001f);
            Assert.IsTrue(IsWholePixel(line.ClippingRect.Top) && IsWholePixel(below.ClippingRect.Top), "edges on whole pixels");
            Assert.AreEqual(line.ClippingRect.Bottom, below.ClippingRect.Top, 0.001f, "neighbours keep sharing their edge");
            Assert.AreEqual(55f, below.ClippingRect.Height * Density, 0.001f);
        }

        [TestMethod]
        public void Without_rounding_layout_stays_fractional()
        {
            using var game = new Game();
            var (line, _) = Layout(game, rounding: false);

            Assert.AreEqual(2.75f, line.ClippingRect.Height * Density, 0.001f);
        }

        [TestMethod]
        public void A_rect_a_hair_below_a_whole_pixel_converts_to_that_pixel()
        {
            Rectangle pixels = new Rect(448.9999f, 10.25f, 2.99997f, 2.5f);

            Assert.AreEqual(new Rectangle(449, 10, 3, 2), pixels);
        }
    }
}
