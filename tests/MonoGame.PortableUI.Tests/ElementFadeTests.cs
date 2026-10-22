using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class ElementFadeTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private Game _game = null!;
        private ScreenEngine _engine = null!;

        [TestInitialize]
        public void SetUp()
        {
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(100);
            _game = new Game();
            _engine = ScreenEngine.Initialize(_game, new ScreenEngineOptions { AddComponentToGame = false });
            _engine.SetScreenSize(400, 300);
        }

        [TestCleanup]
        public void TearDown()
        {
            _engine.Dispose();
            _game.Dispose();
        }

        private void Step(double seconds = 0.016)
        {
            ScreenSystem.TotalTime += TimeSpan.FromSeconds(seconds);
            _engine.Update(new GameTime(ScreenSystem.TotalTime, TimeSpan.FromSeconds(seconds)));
        }

        private static StackPanel Page(bool dark, out TextBlock plain, out TextBlock flipping)
        {
            plain = new TextBlock { Text = "plain", TextColor = dark ? Color.White : Color.Black };
            flipping = new TextBlock { Text = "flip", TextColor = dark ? Color.White : Color.Black, ThemeFade = ThemeFadeMode.Flip };
            return new StackPanel
            {
                BackgroundBrush = new SolidColorBrush(dark ? Color.Black : Color.White),
                Children = { plain, new Border { Content = flipping } }
            };
        }

        private Screen StartFade(out TextBlock oldPlain, out TextBlock newPlain, out TextBlock newFlip)
        {
            var screen = new TestScreen { Content = Page(false, out oldPlain, out _) };
            _engine.NavigateToScreen(screen);
            Step();
            TextBlock? plain = null, flip = null;
            _engine.TransitionTheme(() => screen.Content = Page(true, out plain, out flip), ThemeTransitionStyle.ElementFade, duration: TimeSpan.FromSeconds(8));
            _engine.TakeThemePictureForTests();
            Step();
            newPlain = plain!;
            newFlip = flip!;
            return screen;
        }

        [TestMethod]
        public void The_new_tree_is_paired_with_the_old_one_element_by_element()
        {
            var screen = StartFade(out var oldPlain, out var newPlain, out _);

            Assert.IsNotNull(screen.ThemeTwins);
            Assert.AreSame(oldPlain, screen.ThemeTwins![newPlain].Old);
            Assert.IsTrue(screen.ThemeTwins[(Control)screen.Content!].Surface, "a background makes a surface");
            _engine.EndThemeTransition();
            Assert.IsNull(screen.ThemeTwins);
        }

        [TestMethod]
        public void Slow_elements_blend_evenly_and_flip_elements_cross_quickly_in_the_middle()
        {
            var screen = StartFade(out _, out var newPlain, out var newFlip);
            var plain = screen.ThemeTwins![newPlain];
            var flip = screen.ThemeTwins[newFlip];
            var eight = TimeSpan.FromSeconds(8);

            Assert.AreEqual(0.5f, plain.Mix(0.5f, eight), 0.001f);
            Assert.IsTrue(plain.Mix(0.35f, eight) > 0.25f, "slow: well on its way before the middle");
            Assert.IsTrue(flip.Mix(0.45f, eight) < 0.2f, "flip: still near the old look shortly before the middle");
            Assert.IsTrue(flip.Mix(0.55f, eight) > 0.8f, "and near the new one shortly after");
            Assert.AreEqual(1f, flip.Mix(0.9f, eight), 0.001f, "done within 80 % of the parent's time");
            _engine.EndThemeTransition();
        }

        [TestMethod]
        public void Without_a_rebuilt_tree_an_element_fade_cross_fades()
        {
            var block = new TextBlock { Text = "x" };
            var screen = new TestScreen { Content = block };
            _engine.NavigateToScreen(screen);
            Step();

            _engine.TransitionTheme(() => block.TextColor = Color.Red, ThemeTransitionStyle.ElementFade, duration: TimeSpan.FromSeconds(8));
            _engine.TakeThemePictureForTests();
            Step();

            Assert.IsNull(screen.ThemeTwins);
            Assert.IsTrue(_engine.IsThemeTransitioning);
            _engine.EndThemeTransition();
        }
    }
}
