using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class DisplayPostEffectTests
    {
        private sealed class TestScreen : Screen
        {
        }

        [TestMethod]
        public void Display_effects_follow_the_theme_effects_and_survive_a_theme_change()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            var screen = new TestScreen();
            engine.NavigateToScreen(screen);

            var scanlines = new ScanlinePostEffect();
            var barrel = new CrtBarrelPostEffect();
            var theme = PortableTheme.CreateDefault();
            theme.PostEffects = new PostEffect[] { scanlines };
            engine.Options.Theme = theme;
            engine.Options.PostEffects = new PostEffect[] { barrel };

            var effects = screen.GetScreenPostEffects()!;
            CollectionAssert.AreEqual(new PostEffect[] { scanlines, barrel }, (System.Collections.ICollection)effects);
            Assert.AreSame(effects, screen.GetScreenPostEffects(), "cached, no per-frame allocation");

            engine.Options.Theme = PortableTheme.CreateDefault();
            CollectionAssert.AreEqual(new PostEffect[] { barrel }, (System.Collections.ICollection)screen.GetScreenPostEffects()!, "the monitor's curvature stays");
        }

        [TestMethod]
        public void Curvature_only_comes_from_the_display_never_from_the_theme()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            var screen = new TestScreen();
            engine.NavigateToScreen(screen);
            var scanlines = new ScanlinePostEffect();
            var theme = PortableTheme.CreateDefault();
            theme.PostEffects = new PostEffect[] { scanlines, new CrtBarrelPostEffect { Distortion = 0.2f } };
            engine.Options.Theme = theme;

            CollectionAssert.AreEqual(new PostEffect[] { scanlines }, (System.Collections.ICollection)screen.GetScreenPostEffects()!, "the theme keeps its look, not the curve");

            using var surface = new UISurface(game, new TestScreen(), 400, 300, theme);
            var corner = new MonoGame.PortableUI.Common.PointF(3, 3);
            Assert.IsTrue(surface.IsPointOnDisplay(corner), "a theme barrel does not bend the surface's picture or input");

            surface.PostEffects = new PostEffect[] { new CrtBarrelPostEffect { Distortion = 0.1f } };
            Assert.IsFalse(surface.IsPointOnDisplay(corner), "the display's barrel does");
        }
    }
}
