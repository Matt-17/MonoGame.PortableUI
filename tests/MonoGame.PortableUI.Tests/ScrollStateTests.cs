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

        private static ScrollViewer Chips()
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            for (var i = 0; i < 20; i++)
                row.AddChild(new Border { Width = 80, Height = 30 });
            return new ScrollViewer { ScrollOrientation = Orientation.Horizontal, ScrollBarVisibility = ScrollBarVisibility.AutoHide, Content = row, Height = 40 };
        }

        private static StackPanel ChipPage(out ScrollViewer chips, bool extraFirst = false)
        {
            var page = new StackPanel();
            if (extraFirst)
                page.AddChild(new Border { Height = 10 });
            page.AddChild(new TextBlock { Text = "Prices" });
            page.AddChild(chips = Chips());
            return page;
        }

        [TestMethod]
        public void A_viewer_without_a_key_keeps_its_position_when_the_page_is_rebuilt()
        {
            var screen = new TestScreen { Content = ChipPage(out var chips) };
            _engine.NavigateToScreen(screen);
            Step();
            chips.ScrollTo(new PointF(240, 0));

            screen.Content = ChipPage(out var rebuilt);
            var changes = 0;
            rebuilt.ScrollChanged += (_, _) => changes++;
            Step();

            Assert.AreEqual(240, rebuilt.Offset.X, 0.01f);
            Assert.AreEqual(0, changes, "where it starts is no scroll");
            Assert.AreEqual(0f, rebuilt.ScrollBarOpacity);
        }

        [TestMethod]
        public void A_different_structure_or_an_opt_out_starts_at_the_beginning()
        {
            var screen = new TestScreen { Content = ChipPage(out var chips) };
            _engine.NavigateToScreen(screen);
            Step();
            chips.ScrollTo(new PointF(240, 0));

            screen.Content = ChipPage(out var moved, extraFirst: true);
            Step();
            Assert.AreEqual(0, moved.Offset.X, "another place");

            var page = ChipPage(out var optedOut);
            optedOut.RestoresScrollState = false;
            screen.Content = page;
            Step();
            Assert.AreEqual(0, optedOut.Offset.X);
        }

        [TestMethod]
        public void Tagged_items_keep_their_own_positions_when_reordered()
        {
            StackPanel List(string first, string second, out ScrollViewer a, out ScrollViewer b)
            {
                var list = new StackPanel();
                var one = Chips();
                var two = Chips();
                list.AddChild(new Border { Tag = first, Content = one });
                list.AddChild(new Border { Tag = second, Content = two });
                a = first == "a" ? one : two;
                b = first == "a" ? two : one;
                return list;
            }

            var screen = new TestScreen { Content = List("a", "b", out var a, out var b) };
            _engine.NavigateToScreen(screen);
            Step();
            a.ScrollTo(new PointF(80, 0));
            b.ScrollTo(new PointF(320, 0));

            screen.Content = List("b", "a", out var newA, out var newB);
            Step();
            Assert.AreEqual(80, newA.Offset.X, 0.01f);
            Assert.AreEqual(320, newB.Offset.X, 0.01f);
        }

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
            old.ScrollTo(new PointF(0, 500), revealScrollBar: false);
            ScrollViewer? fresh = null;

            _engine.TransitionTheme(() => screen.Content = fresh = Page("list"));
            _engine.TakeThemePictureForTests();
            Step();

            Assert.AreEqual(500, fresh!.Offset.Y, 0.01f);
            Assert.AreEqual(500, old.Offset.Y, 0.01f, "the ghost keeps its own");

            // The user scrolls the new tree during a long fade: the old look follows, so nothing shows doubled.
            fresh.ScrollTo(new PointF(0, 820));
            Step();
            Assert.AreEqual(820, old.Offset.Y, 0.01f);
            Assert.AreEqual(0f, old.ScrollBarOpacity, "following is no scroll of its own");
            _engine.EndThemeTransition();
        }

        [TestMethod]
        public void Every_ghost_of_stacked_changes_follows_the_keyed_scroll()
        {
            var screen = new TestScreen { Content = Page("list") };
            _engine.NavigateToScreen(screen);
            Step();
            var first = (ScrollViewer)screen.Content!;

            _engine.TransitionTheme(() => screen.Content = Page("list"), ThemeTransitionStyle.Reveal);
            _engine.TakeThemePictureForTests();
            Step();
            var second = (ScrollViewer)screen.Content!;
            _engine.TransitionTheme(() => screen.Content = Page("list"), duration: TimeSpan.FromSeconds(8));
            _engine.TakeThemePictureForTests();
            Step();
            var third = (ScrollViewer)screen.Content!;
            Assert.AreEqual(2, _engine.ThemeLayerCount);

            third.ScrollTo(new PointF(0, 700));
            Step();
            Assert.AreEqual(700, second.Offset.Y, 0.01f, "the newer ghost follows");
            Assert.AreEqual(700, first.Offset.Y, 0.01f, "the earlier ghost follows too");
            _engine.EndThemeTransition();
        }

        [TestMethod]
        public void A_navigation_during_a_long_fade_finishes_it_within_a_quarter_second()
        {
            var screen = new TestScreen { Content = Page("list") };
            _engine.NavigateToScreen(screen);
            Step();

            _engine.TransitionTheme(() => screen.Content = Page("list"), duration: TimeSpan.FromSeconds(8));
            _engine.TakeThemePictureForTests();
            Step();
            Assert.AreEqual(1, _engine.ThemeLayerCount);
            Assert.IsTrue(_engine.ThemeLayerProgress(0) < 0.01f);

            _engine.NavigateToScreen(new TestScreen { Content = new Border() }, ScreenTransition.None);
            Step();
            for (var i = 0; i < 16; i++)
                Step();
            Assert.AreEqual(1f, _engine.ThemeLayerProgress(0), 0.001f, "done 256 ms after the navigation, not after 8 s");
            _engine.EndThemeTransition();
        }
    }
}
