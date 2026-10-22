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

        private void Step(double seconds)
        {
            ScreenSystem.TotalTime += TimeSpan.FromSeconds(seconds);
            _engine.Update(new GameTime(ScreenSystem.TotalTime, TimeSpan.FromSeconds(seconds)));
        }

        [TestMethod]
        public void Replaced_content_goes_on_live_in_the_old_theme()
        {
            var oldTheme = _engine.Options.Theme;
            var newTheme = PortableTheme.CreateDefault();
            var oldSwitch = new Controls.ToggleSwitch { KnobMotion = Animation.PartMotion.Linear };
            var screen = new TestScreen { Content = new Controls.StackPanel { Children = { oldSwitch } } };
            _engine.NavigateToScreen(screen);
            Step(0.016);
            var oldContent = screen.Content;

            _engine.TransitionTheme(() =>
            {
                _engine.Options.Theme = newTheme;
                screen.Content = new Controls.StackPanel { Children = { new Controls.ToggleSwitch() } };
            });
            _engine.TakeThemePictureForTests();
            Step(0.016);

            Assert.AreEqual(1, _engine.ThemeGhosts.Count);
            var ghost = _engine.ThemeGhosts[0];
            Assert.AreNotSame(screen, ghost);
            Assert.AreSame(oldContent, ghost.Content);
            Assert.AreSame(oldTheme, oldSwitch.Knob.Screen!.ThemeOverride);

            // Its animations run on: the knob of the old switch slides.
            oldSwitch.IsOn = true;
            Step(0.016);
            Step(0.05);
            Assert.IsTrue(oldSwitch.Knob.ShownTranslation.X > 0, "the old tree is updated every frame");

            _engine.EndThemeTransition();
            Assert.AreEqual(0, _engine.ThemeGhosts.Count);
            Assert.IsNull(oldContent!.Parent, "the old tree is let go");
        }

        [TestMethod]
        public void A_change_during_a_change_stacks_on_it_and_both_old_looks_run_on()
        {
            var screen = new TestScreen { Content = new Controls.Border() };
            _engine.NavigateToScreen(screen);
            Step(0.016);
            var first = screen.Content;

            _engine.TransitionTheme(() => screen.Content = new Controls.Border(), ThemeTransitionStyle.Reveal);
            _engine.TakeThemePictureForTests();
            Step(0.016);
            var second = screen.Content;
            _engine.TransitionTheme(() => screen.Content = new Controls.Border(), ThemeTransitionStyle.Reveal);
            _engine.TakeThemePictureForTests();
            Step(0.016);

            Assert.AreEqual(2, _engine.ThemeLayerCount, "the first change goes on inside the second");
            Assert.AreSame(second, _engine.ThemeGhosts[0].Content);
            Assert.IsNotNull(first!.Parent, "the first old tree is still alive");

            _engine.EndThemeTransition();
            Assert.AreEqual(0, _engine.ThemeLayerCount);
            Assert.IsNull(first.Parent);
            Assert.IsNull(second!.Parent);
        }

        [TestMethod]
        public void A_change_in_place_keeps_the_picture()
        {
            var block = new Controls.TextBlock { Text = "x" };
            _engine.NavigateToScreen(new TestScreen { Content = block });
            Step(0.016);

            _engine.TransitionTheme(() => block.TextColor = Color.Red);
            _engine.TakeThemePictureForTests();
            Step(0.016);

            Assert.AreEqual(0, _engine.ThemeGhosts.Count);
            Assert.IsTrue(_engine.IsThemeTransitioning);
        }

        [TestMethod]
        public void A_replaced_screen_is_its_own_ghost_and_handed_back_after()
        {
            var old = new TestScreen { Content = new Controls.Border() };
            _engine.NavigateToScreen(old);
            Step(0.016);
            var input = old.InputSource;

            _engine.TransitionTheme(() => _engine.NavigateToScreen(new TestScreen { Content = new Controls.Border() }));
            _engine.TakeThemePictureForTests();
            Step(0.016);

            Assert.AreSame(old, _engine.ThemeGhosts[0]);
            Assert.AreSame(Input.NullInputSource.Instance, old.InputSource, "a ghost takes no input");

            _engine.EndThemeTransition();
            Assert.AreSame(input, old.InputSource);
            Assert.IsNull(old.ThemeOverride);
        }

        [TestMethod]
        public void Without_live_the_picture_is_used()
        {
            var screen = new TestScreen { Content = new Controls.Border() };
            _engine.NavigateToScreen(screen);
            Step(0.016);

            _engine.TransitionTheme(() => screen.Content = new Controls.Border(), live: false);
            _engine.TakeThemePictureForTests();
            Step(0.016);

            Assert.AreEqual(0, _engine.ThemeGhosts.Count);
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
