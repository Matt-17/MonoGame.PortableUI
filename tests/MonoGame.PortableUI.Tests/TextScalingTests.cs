using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class TextScalingTests
    {
        private sealed class TestScreen : Screen
        {
        }

        [TestCleanup]
        public void Cleanup() => TextScaling.Reset();

        [TestMethod]
        public void Source_selects_the_factor_and_the_clamp_bounds_it()
        {
            TextScaling.AppScale = 1.25f;
            TextScaling.SystemScale = 3f;
            Assert.AreEqual(1.25f, TextScaling.Factor);

            TextScaling.Source = TextScaleSource.System;
            Assert.AreEqual(2f, TextScaling.Factor, "clamped to MaxFactor");

            TextScaling.SystemScale = 0.5f;
            Assert.AreEqual(0.8f, TextScaling.Factor, "clamped to MinFactor");

            TextScaling.Source = TextScaleSource.Fixed;
            Assert.AreEqual(1f, TextScaling.Factor);

            TextScaling.Source = TextScaleSource.App;
            TextScaling.AppScale = float.NaN;
            Assert.AreEqual(1f, TextScaling.Factor, "invalid values fall back to 1");
        }

        [TestMethod]
        public void Changed_fires_only_when_the_factor_changes()
        {
            var raised = 0;
            EventHandler handler = (s, e) => raised++;
            TextScaling.Changed += handler;
            try
            {
                TextScaling.SystemScale = 1.5f; // App source: no effect
                TextScaling.AppScale = 1.5f;
                TextScaling.AppScale = 1.5f;
                Assert.AreEqual(1, raised);
            }
            finally
            {
                TextScaling.Changed -= handler;
            }
        }

        [TestMethod]
        [DataRow(1f)]
        [DataRow(1.5f)]
        [DataRow(2f)]
        public void Text_is_measured_at_the_scaled_size(float factor)
        {
            var block = new TextBlock { Text = "Hello world" };
            var baseline = block.MeasureLayout();

            TextScaling.AppScale = factor;
            block.OnTextScaleChanged();
            var scaled = block.MeasureLayout();

            Assert.AreEqual(baseline.Width * factor, scaled.Width, 0.01f);
            Assert.AreEqual(baseline.Height * factor, scaled.Height, 0.01f);
        }

        [TestMethod]
        public void A_running_screen_relayouts_when_the_factor_changes()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 300);
            var label = new TextBlock { Text = "Label", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var screen = new TestScreen { Content = label };
            engine.NavigateToScreen(screen);
            screen.Update();
            var before = label.ClippingRect.Width;

            TextScaling.AppScale = 2f;
            screen.Update();

            Assert.AreEqual(before * 2, label.ClippingRect.Width, 0.01f);
        }
    }
}
