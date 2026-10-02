using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class RenderQualityTests
    {
        private sealed class TestScreen : Screen
        {
        }

        [TestInitialize]
        public void Reset()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
        }

        private static ScreenEngine CreateEngine(Game game, RenderQuality quality, int maxFrameRate = 0)
        {
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions
            {
                AddComponentToGame = false,
                RenderQuality = quality,
                MaxFrameRate = maxFrameRate
            });
            engine.SetScreenSize(300, 300);
            engine.NavigateToScreen(new TestScreen());
            engine.UpdateRenderQuality();
            return engine;
        }

        [TestMethod]
        public void Auto_is_high_quality_without_power_saving()
        {
            using var game = new Game();
            var engine = CreateEngine(game, RenderQuality.Auto);

            Assert.AreEqual(RenderQuality.High, engine.EffectiveRenderQuality);
            Assert.AreEqual(0, engine.EffectiveMaxFrameRate, "no cap at High");
        }

        [TestMethod]
        public void Low_quality_caps_at_30_fps_unless_a_frame_rate_is_set()
        {
            using var game = new Game();
            Assert.AreEqual(30, CreateEngine(game, RenderQuality.Low).EffectiveMaxFrameRate);

            using var game2 = new Game();
            Assert.AreEqual(45, CreateEngine(game2, RenderQuality.Low, maxFrameRate: 45).EffectiveMaxFrameRate);

            using var game3 = new Game();
            Assert.AreEqual(0, CreateEngine(game3, RenderQuality.Balanced).EffectiveMaxFrameRate);
        }

        [TestMethod]
        public void Switching_the_quality_at_runtime_takes_effect_and_asks_for_a_frame()
        {
            using var game = new Game();
            var engine = CreateEngine(game, RenderQuality.High);
            engine.ConsumeRedrawRequest();
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(10);
            Assert.IsFalse(engine.ConsumeRedrawRequest());

            engine.Options.RenderQuality = RenderQuality.Low;
            engine.UpdateRenderQuality();

            Assert.AreEqual(RenderQuality.Low, engine.EffectiveRenderQuality);
            Assert.IsTrue(engine.ConsumeRedrawRequest());
        }

        [TestMethod]
        public void Low_quality_drops_theme_effects_but_keeps_the_display_effects()
        {
            using var game = new Game();
            var engine = CreateEngine(game, RenderQuality.High);
            var screen = (Screen)engine.ActiveScreen!;
            var scanlines = new ScanlinePostEffect();
            var barrel = new CrtBarrelPostEffect();
            var theme = PortableTheme.CreateDefault();
            theme.PostEffects = new PostEffect[] { scanlines };
            engine.Options.Theme = theme;
            engine.Options.PostEffects = new PostEffect[] { barrel };
            CollectionAssert.AreEqual(new PostEffect[] { scanlines, barrel }, (System.Collections.ICollection)screen.GetScreenPostEffects()!);

            engine.Options.RenderQuality = RenderQuality.Low;
            engine.UpdateRenderQuality();

            CollectionAssert.AreEqual(new PostEffect[] { barrel }, (System.Collections.ICollection)screen.GetScreenPostEffects()!,
                "the monitor's curvature (and the input mapping that follows it) stays");
        }

        [TestMethod]
        public void Low_quality_shadows_use_fewer_layers()
        {
            var shadow = new ShadowStyle { Color = Color.Black, Opacity = 0.5f, Blur = 24 };
            Span<ShadowRenderer.ShadowLayer> layers = stackalloc ShadowRenderer.ShadowLayer[ShadowRenderer.MaxLayers];

            var full = ShadowRenderer.FillShadowLayers(new Rect(0, 0, 100, 50), shadow, layers);
            var low = ShadowRenderer.FillShadowLayers(new Rect(0, 0, 100, 50), shadow, layers, 1f, ShadowRenderer.LowQualityLayers);

            Assert.AreEqual(ShadowRenderer.MaxLayers, full);
            Assert.AreEqual(ShadowRenderer.LowQualityLayers, low);
        }

        [TestMethod]
        public void Decorations_only_animate_at_high_quality_and_outside_a_draw_count_as_high()
        {
            Assert.IsTrue(ScreenEngine.AnimatesDecorations, "outside a draw nothing restricts brushes");
            Assert.AreEqual(RenderQuality.High, ScreenEngine.DrawingQuality);
        }
    }
}
