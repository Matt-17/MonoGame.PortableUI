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
    }
}
