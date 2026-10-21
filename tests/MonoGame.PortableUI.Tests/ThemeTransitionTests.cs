using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class ThemeTransitionTests
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
            _engine.SetScreenSize(400, 600);
        }

        [TestCleanup]
        public void TearDown()
        {
            _engine.Dispose();
            _game.Dispose();
        }

        [TestMethod]
        public void Without_a_screen_the_theme_changes_at_once()
        {
            var applied = 0;

            _engine.TransitionTheme(() => applied++);

            Assert.AreEqual(1, applied);
            Assert.IsFalse(_engine.IsThemeTransitioning);
        }

        [TestMethod]
        public void A_zero_duration_changes_at_once()
        {
            _engine.NavigateToScreen(new TestScreen());
            var applied = 0;

            _engine.TransitionTheme(() => applied++, duration: TimeSpan.Zero);

            Assert.AreEqual(1, applied);
        }

        [TestMethod]
        public void The_change_waits_for_the_picture_of_the_old_look()
        {
            _engine.NavigateToScreen(new TestScreen());
            var applied = 0;

            _engine.TransitionTheme(() => applied++, ThemeTransitionStyle.Reveal, new PointF(20, 30));

            Assert.AreEqual(0, applied);
            Assert.IsTrue(_engine.IsThemeTransitioning);
        }

        [TestMethod]
        public void A_lost_frame_still_applies_the_change_on_the_next_update()
        {
            _engine.NavigateToScreen(new TestScreen());
            var applied = 0;
            _engine.TransitionTheme(() => applied++);

            // Android reports it on its UI thread: nothing happens there, the game thread cleans up.
            _engine.ThemeFrameLost();
            Assert.AreEqual(0, applied);
            _engine.Update(new GameTime(TimeSpan.FromSeconds(101), TimeSpan.FromMilliseconds(16)));

            Assert.AreEqual(1, applied);
            Assert.IsFalse(_engine.IsThemeTransitioning);
        }
    }
}
