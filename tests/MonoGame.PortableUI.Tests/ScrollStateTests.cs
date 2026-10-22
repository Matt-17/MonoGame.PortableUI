using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class ScrollStateTests
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

        private void Step()
        {
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
            _engine.Update(new GameTime(ScreenSystem.TotalTime, TimeSpan.FromMilliseconds(16)));
        }

        private static ScrollViewer Page(string? key)
            => new ScrollViewer { StateKey = key, ScrollBarVisibility = ScrollBarVisibility.AutoHide, Content = new Border { Height = 2000 } };

        [TestMethod]
        public void A_rebuilt_page_with_the_same_key_starts_where_the_last_one_was_and_quietly()
        {
            var screen = new TestScreen { Content = Page("list") };
            _engine.NavigateToScreen(screen);
            Step();
            ((ScrollViewer)screen.Content!).ScrollTo(new PointF(0, 640));

            var rebuilt = Page("list");
            var changes = 0;
            rebuilt.ScrollChanged += (_, _) => changes++;
            screen.Content = rebuilt;
            Step();

            Assert.AreEqual(640, rebuilt.Offset.Y, 0.01f);
            Assert.AreEqual(0, changes, "where it starts is no scroll");
            Assert.AreEqual(0f, rebuilt.ScrollBarOpacity, "the auto-hiding bar stays hidden");
        }

        [TestMethod]
        public void Without_a_key_or_with_another_key_a_page_starts_at_the_top()
        {
            var screen = new TestScreen { Content = Page("list") };
            _engine.NavigateToScreen(screen);
            Step();
            ((ScrollViewer)screen.Content!).ScrollTo(new PointF(0, 300));

            var other = Page("other");
            screen.Content = other;
            Step();
            Assert.AreEqual(0, other.Offset.Y);

            var none = Page(null);
            screen.Content = none;
            Step();
            Assert.AreEqual(0, none.Offset.Y);
        }

        [TestMethod]
        public void A_live_theme_change_starts_the_new_tree_at_the_old_offset()
        {
            var screen = new TestScreen { Content = Page("list") };
            _engine.NavigateToScreen(screen);
            Step();
            var old = (ScrollViewer)screen.Content!;
            old.ScrollTo(new PointF(0, 500));
            ScrollViewer? fresh = null;

            _engine.TransitionTheme(() => screen.Content = fresh = Page("list"));
            _engine.TakeThemePictureForTests();
            Step();

            Assert.AreEqual(500, fresh!.Offset.Y, 0.01f);
            Assert.AreEqual(500, old.Offset.Y, 0.01f, "the ghost keeps its own");
            _engine.EndThemeTransition();
        }
    }
}
