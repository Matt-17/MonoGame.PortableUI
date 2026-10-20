using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>#162: shared-element (hero) transitions - a keyed control flies from its twin on the screen left behind.</summary>
    [TestClass]
    [DoNotParallelize] // drives the global ScreenSystem.TotalTime clock
    public class SharedElementTransitionTests
    {
        private sealed class TestScreen : Screen
        {
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
                Engine.SharedElementTransition = new Transition(TimeSpan.FromMilliseconds(100), Easings.Linear);
            }

            public void Step(int frames = 1, int milliseconds = 16)
            {
                for (var i = 0; i < frames; i++)
                {
                    _time += TimeSpan.FromMilliseconds(milliseconds);
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

        private static (TestScreen Screen, Border Hero) CreateScreen(string? key, Rect place)
        {
            var hero = new Border
            {
                Width = place.Width,
                Height = place.Height,
                Margin = new Thickness(place.Left, place.Top, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                SharedElementKey = key
            };
            var screen = new TestScreen { Content = hero, InputSource = NullInputSource.Instance };
            return (screen, hero);
        }

        private static Rect Drawn(Control control) => Screen.TransformRectForSharedElement(control.ClippingRect, control.GetRenderTransform());

        private static void AssertRect(Rect expected, Rect actual, string message)
        {
            Assert.AreEqual(expected.Left, actual.Left, 0.01f, message + " (left)");
            Assert.AreEqual(expected.Top, actual.Top, 0.01f, message + " (top)");
            Assert.AreEqual(expected.Width, actual.Width, 0.01f, message + " (width)");
            Assert.AreEqual(expected.Height, actual.Height, 0.01f, message + " (height)");
        }

        [TestMethod]
        public void The_keyed_control_flies_from_where_its_twin_was_drawn()
        {
            using var host = new Host();
            var (list, thumb) = CreateScreen("hero", new Rect(10, 20, 40, 30));
            var (detail, hero) = CreateScreen("hero", new Rect(100, 80, 200, 150));
            host.Engine.NavigateToScreen(list);
            host.Step();

            host.Engine.NavigateToScreen(detail, ScreenTransition.Fade);
            host.Step();

            Assert.AreEqual(SharedFlightRole.Flying, hero.SharedFlight);
            Assert.AreEqual(SharedFlightRole.Hidden, thumb.SharedFlight);
            Assert.AreEqual(100, hero.ClippingRect.Left, "layout (and hit testing) are at the new place");
            // One frame of the 100 ms flight has passed (16 ms of 100): close to the thumbnail's rect.
            var start = Drawn(hero);
            Assert.IsTrue(start.Width < 70 && start.Left < 30, $"starts at the thumbnail: {start}");

            host.Step(10);
            AssertRect(new Rect(100, 80, 200, 150), Drawn(hero), "arrived");
            Assert.AreEqual(SharedFlightRole.None, hero.SharedFlight);
            Assert.AreEqual(SharedFlightRole.None, thumb.SharedFlight);
        }

        [TestMethod]
        public void The_flight_starts_exactly_on_the_old_rect()
        {
            using var host = new Host();
            host.Engine.SharedElementTransition = new Transition(TimeSpan.FromSeconds(100), Easings.Linear);
            var (list, _) = CreateScreen("hero", new Rect(10, 20, 40, 30));
            var (detail, hero) = CreateScreen("hero", new Rect(100, 80, 200, 150));
            hero.TransformOrigin = new Vector2(0.2f, 0.9f);
            host.Engine.NavigateToScreen(list);
            host.Step();

            host.Engine.NavigateToScreen(detail, ScreenTransition.None);
            host.Step();
            AssertRect(new Rect(10, 20, 40, 30), Drawn(hero), "any origin");
        }

        [TestMethod]
        public void Navigating_back_flies_the_other_way()
        {
            using var host = new Host();
            var (list, thumb) = CreateScreen("hero", new Rect(10, 20, 40, 30));
            var (detail, hero) = CreateScreen("hero", new Rect(100, 80, 200, 150));
            host.Engine.NavigateToScreen(list);
            host.Step();
            host.Engine.NavigateToScreen(detail, ScreenTransition.SlideFromRight);
            host.Step(12);

            host.Engine.NavigateBack();
            host.Step();
            Assert.AreEqual(SharedFlightRole.Flying, thumb.SharedFlight);
            Assert.AreEqual(SharedFlightRole.Hidden, hero.SharedFlight);
            Assert.IsTrue(Drawn(thumb).Width > 150, "starts large");
            host.Step(12);
            AssertRect(new Rect(10, 20, 40, 30), Drawn(thumb), "back home");
            Assert.AreEqual(SharedFlightRole.None, hero.SharedFlight);
        }

        [TestMethod]
        public void A_slow_first_frame_does_not_eat_the_flight_or_the_screen_transition()
        {
            using var host = new Host();
            host.Engine.TransitionDuration = TimeSpan.FromMilliseconds(250);
            host.Engine.SharedElementTransition = new Transition(TimeSpan.FromMilliseconds(300), Easings.Linear);
            var (list, _) = CreateScreen("hero", new Rect(10, 20, 40, 30));
            var (detail, hero) = CreateScreen("hero", new Rect(100, 80, 200, 150));
            host.Engine.NavigateToScreen(list);
            host.Step();

            host.Engine.NavigateToScreen(detail, ScreenTransition.Fade);
            host.Step();
            // The new screen's first frame stalls for 800 ms (fonts, jit).
            host.Step(milliseconds: 800);

            Assert.AreEqual(SharedFlightRole.Flying, hero.SharedFlight, "still flying");
            Assert.IsTrue(Drawn(hero).Width < 150, $"not yet home: {Drawn(hero)}");
            Assert.IsTrue(host.Engine.IsTransitioning, "the fade still plays");
            host.Step(20);
            Assert.AreEqual(SharedFlightRole.None, hero.SharedFlight);
            Assert.IsFalse(host.Engine.IsTransitioning);
        }

        [TestMethod]
        public void Different_keys_do_not_fly()
        {
            using var host = new Host();
            var (list, _) = CreateScreen("a", new Rect(10, 20, 40, 30));
            var (detail, hero) = CreateScreen("b", new Rect(100, 80, 200, 150));
            host.Engine.NavigateToScreen(list);
            host.Step();
            host.Engine.NavigateToScreen(detail, ScreenTransition.None);
            host.Step();
            Assert.AreEqual(SharedFlightRole.None, hero.SharedFlight);
            Assert.AreEqual(Matrix.Identity, hero.GetRenderTransform());
        }

        [TestMethod]
        public void A_new_navigation_lands_a_running_flight()
        {
            using var host = new Host();
            host.Engine.SharedElementTransition = new Transition(TimeSpan.FromSeconds(10), Easings.Linear);
            var (list, thumb) = CreateScreen("hero", new Rect(10, 20, 40, 30));
            var (detail, hero) = CreateScreen("hero", new Rect(100, 80, 200, 150));
            var (other, _) = CreateScreen(null, new Rect(0, 0, 10, 10));
            host.Engine.NavigateToScreen(list);
            host.Step();
            host.Engine.NavigateToScreen(detail, ScreenTransition.None);
            host.Step();
            Assert.AreEqual(SharedFlightRole.Flying, hero.SharedFlight);

            host.Engine.NavigateToScreen(other, ScreenTransition.None);
            Assert.AreEqual(SharedFlightRole.None, hero.SharedFlight);
            Assert.AreEqual(SharedFlightRole.None, thumb.SharedFlight);
            Assert.AreEqual(Matrix.Identity, hero.GetRenderTransform(), "landed");
        }
    }
}
