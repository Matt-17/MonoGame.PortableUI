using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>#161: built-in screen transitions, the screen under the moving one, and custom transitions.</summary>
    [TestClass]
    [DoNotParallelize] // drives the global ScreenSystem.TotalTime clock
    public class ScreenTransitionAnimationTests
    {
        private sealed class TestScreen : Screen
        {
            public TestScreen()
            {
                Content = new Border();
                InputSource = NullInputSource.Instance;
            }
        }

        private sealed class Host : IDisposable
        {
            private readonly Game _game = new Game();
            private TimeSpan _time = TimeSpan.FromSeconds(1);
            public readonly ScreenEngine Engine;

            public Host()
            {
                Engine = ScreenEngine.CreateSurfaceEngine(_game, new ScreenEngineOptions { AddComponentToGame = false, ScreenSizeMode = ScreenSizeMode.Manual, DebugOverlayKey = null });
                Engine.SetScreenSize(400, 300);
                Engine.TransitionDuration = TimeSpan.FromMilliseconds(160);
            }

            public void Step(int frames = 1)
            {
                for (var i = 0; i < frames; i++)
                {
                    _time += TimeSpan.FromMilliseconds(16);
                    ScreenSystem.TotalTime = _time;
                    Engine.Update(new GameTime(_time, TimeSpan.FromMilliseconds(16)));
                }
            }

            public void Dispose()
            {
                Engine.Dispose();
                _game.Dispose();
            }
        }

        /// <summary>Records the progress values and which direction it was called for.</summary>
        private sealed class Recording : ScreenTransitionAnimation
        {
            public readonly List<(float Progress, bool Push)> Calls = new List<(float, bool)>();

            public override void Apply(float progress, ScreenTransitionFrame frame)
            {
                Calls.Add((progress, frame.IsPush));
                frame.Top.Translation = new Vector2(100 * (1 - progress), 0);
                frame.Below.Opacity = 1 - progress;
            }
        }

        [TestMethod]
        public void Push_moves_both_screens_and_draws_the_covered_one_until_it_ends()
        {
            using var host = new Host();
            var first = new TestScreen();
            var second = new TestScreen();
            host.Engine.NavigateToScreen(first);
            host.Step();

            host.Engine.NavigateToScreen(second, ScreenTransition.Push);
            Assert.AreSame(first, host.Engine.CoveredScreen, "the old screen is drawn under the new one");
            host.Step(3);
            Assert.IsTrue(second.RootGrid.ShownTranslation.X > 0, "the new screen comes in from the right");
            Assert.IsTrue(first.RootGrid.ShownTranslation.X < 0, "the old one moves left (parallax)");
            Assert.IsTrue(first.RootGrid.ShownTranslation.X > -0.3f * 400, "slower than the new one");

            host.Step(12);
            Assert.IsFalse(host.Engine.IsTransitioning);
            Assert.IsNull(host.Engine.CoveredScreen);
            Assert.AreEqual(Vector2.Zero, first.RootGrid.ShownTranslation, "the covered screen is reset");
            Assert.AreEqual(1, first.RootGrid.ShownOpacity);
            Assert.AreEqual(Vector2.Zero, second.RootGrid.ShownTranslation);
        }

        [TestMethod]
        public void A_custom_transition_runs_forwards_on_push_and_backwards_on_pop()
        {
            using var host = new Host();
            var recording = new Recording { Duration = TimeSpan.FromMilliseconds(64), Easing = Easings.Linear };
            var first = new TestScreen();
            var second = new TestScreen();
            host.Engine.NavigateToScreen(first);
            host.Step();

            host.Engine.NavigateToScreen(second, recording);
            host.Step(2);
            Assert.AreEqual(0, recording.Calls[0].Progress, "starts with only the screen below shown");
            Assert.IsTrue(recording.Calls[0].Push);
            Assert.AreEqual(0.5f, recording.Calls[^1].Progress, 0.01f, "own duration and easing");
            Assert.AreEqual(50, second.RootGrid.ShownTranslation.X, 0.5f);
            Assert.AreEqual(0.5, first.RootGrid.ShownOpacity, 0.01);
            host.Step(4);
            Assert.IsFalse(host.Engine.IsTransitioning);

            recording.Calls.Clear();
            host.Engine.NavigateBack();
            host.Step();
            Assert.AreEqual(1, recording.Calls[0].Progress, "popping runs backwards");
            Assert.IsFalse(recording.Calls[0].Push);
            Assert.AreSame(second, host.Engine.LeavingScreen);
            Assert.IsTrue(first.RootGrid.ShownOpacity < 0.9, "the revealed screen is moved too");
            host.Step(4);
            Assert.IsNull(host.Engine.LeavingScreen);
            Assert.AreEqual(1, first.RootGrid.ShownOpacity);
        }

        [TestMethod]
        public void Reveal_draws_the_old_screen_on_top()
        {
            using var host = new Host();
            var first = new TestScreen();
            var second = new TestScreen();
            host.Engine.NavigateToScreen(first);
            host.Step();

            host.Engine.NavigateToScreen(second, ScreenTransition.Reveal);
            host.Step(2);
            Assert.IsTrue(host.Engine.TransitionBelowOnTop);
            Assert.IsTrue(first.RootGrid.ShownTranslation.X < 0, "the old screen slides away to the left");
            Assert.AreEqual(Vector2.Zero, second.RootGrid.ShownTranslation, "the new one lies still under it");
        }

        [TestMethod]
        public void Every_built_in_transition_ends_with_both_screens_at_rest()
        {
            foreach (ScreenTransition kind in Enum.GetValues(typeof(ScreenTransition)))
            {
                using var host = new Host();
                var first = new TestScreen();
                var second = new TestScreen();
                host.Engine.NavigateToScreen(first);
                host.Step();
                host.Engine.NavigateToScreen(second, kind);
                host.Step(3);
                Assert.AreEqual(kind != ScreenTransition.None, host.Engine.IsTransitioning, kind.ToString());
                host.Step(12);
                host.Engine.NavigateBack();
                host.Step(15);
                foreach (var screen in new[] { first, second })
                {
                    var root = screen.RootGrid;
                    Assert.AreEqual(1, root.ShownOpacity, kind + " opacity");
                    Assert.AreEqual(Vector2.Zero, root.ShownTranslation, kind + " translation");
                    Assert.AreEqual(Vector2.One, root.ShownScale, kind + " scale");
                }
                Assert.IsFalse(host.Engine.IsTransitioning, kind.ToString());
            }
        }

        [TestMethod]
        public void Flip_shows_one_screen_at_a_time()
        {
            using var host = new Host();
            var first = new TestScreen();
            var second = new TestScreen();
            host.Engine.NavigateToScreen(first);
            host.Step();
            host.Engine.NavigateToScreen(second, ScreenTransition.Flip);
            host.Step(2);
            Assert.AreEqual(0, second.RootGrid.ShownOpacity, "first half: the new screen is not shown yet");
            Assert.IsTrue(first.RootGrid.ShownScale.X < 1, "the old one narrows");
            host.Step(6);
            Assert.AreEqual(0, first.RootGrid.ShownOpacity, "second half: the old one is gone");
            Assert.IsTrue(second.RootGrid.ShownScale.X < 1 && second.RootGrid.ShownOpacity == 1);
        }
    }
}
