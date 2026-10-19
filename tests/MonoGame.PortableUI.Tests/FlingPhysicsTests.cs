using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class FlingPhysicsTests
    {
        // Reference values from a direct port of AOSP SplineOverScroller (density 1, dp units:
        // 160 units per inch, friction 0.015): speed (dp/s) -> distance (dp), duration (s).
        [TestMethod]
        [DataRow(500f, 58.32f, 0.3333)]
        [DataRow(3000f, 1308.92f, 1.2466)]
        [DataRow(8000f, 7186.36f, 2.5666)]
        public void Android_fling_matches_the_platform_scroller(float speed, float distance, double seconds)
        {
            var trajectory = FlingPhysics.Android().Start(speed);

            Assert.AreEqual(distance, trajectory.Distance, distance * 0.002f);
            Assert.AreEqual(seconds, trajectory.Duration.TotalSeconds, 0.002);
        }

        [TestMethod]
        public void Android_fling_starts_at_the_release_speed_and_eases_out_to_its_distance()
        {
            var trajectory = FlingPhysics.Android().Start(3000);
            var duration = trajectory.Duration.TotalSeconds;

            Assert.AreEqual(3000, trajectory.VelocityAt(0), 30, "the spline's initial slope is the release speed");
            Assert.AreEqual(trajectory.Distance, trajectory.PositionAt(duration), 0.01f);
            Assert.AreEqual(0, trajectory.VelocityAt(duration));
            var last = -1f;
            for (var t = 0.0; t <= duration; t += duration / 50)
            {
                var position = trajectory.PositionAt(t);
                Assert.IsTrue(position >= last, $"monotonic at {t}");
                last = position;
            }
            Assert.IsTrue(trajectory.VelocityAt(duration * 0.9) < 300, "the last tenth only creeps");
        }

        [TestMethod]
        public void Android_limits_come_from_the_view_configuration_defaults()
        {
            var physics = FlingPhysics.Android();

            Assert.AreEqual(0, physics.LimitSpeed(40), "below getScaledMinimumFlingVelocity no fling");
            Assert.AreEqual(8000, physics.LimitSpeed(20000), "capped at getScaledMaximumFlingVelocity");
            Assert.AreEqual(TimeSpan.FromMilliseconds(40), physics.RestTime);
        }

        [TestMethod]
        public void Android_scales_with_the_layout_units_per_inch()
        {
            // Layout units in pixels on a 2.75 density screen: 440 px per inch, speeds in px/s.
            var dp = FlingPhysics.Android().Start(1000).Distance;
            var px = FlingPhysics.Android(unitsPerInch: 440).Start(2750).Distance;

            Assert.AreEqual(dp * 2.75f, px, dp * 2.75f * 0.001f);
        }

        [TestMethod]
        public void IOS_uses_the_UIKit_normal_deceleration_rate()
        {
            var trajectory = FlingPhysics.IOS.Start(2000);
            const double k = -1000 * 0.0020020027; // ln(0.998) per ms -> per second

            // v(t) = v0 · 0.998^ms
            Assert.AreEqual(2000 * Math.Pow(0.998, 250), trajectory.VelocityAt(0.25), 0.5);
            // Distance until the stop speed: (v0 - 12) / k
            Assert.AreEqual((2000 - 12) / -k, trajectory.Distance, 1);
            Assert.AreEqual(0, FlingPhysics.IOS.MinimumVelocity);
        }

        [TestMethod]
        public void Exponential_keeps_the_previous_portable_ui_curve()
        {
            var trajectory = FlingPhysics.Exponential(3.5f).Start(1200);

            Assert.AreEqual((1200 - 12) / 3.5f, trajectory.Distance, 0.5f);
            Assert.AreEqual(Math.Log(100) / 3.5, trajectory.Duration.TotalSeconds, 1e-4);
        }

        [TestMethod]
        public void Velocity_tracker_fits_uneven_frames()
        {
            var tracker = new TouchVelocityTracker();
            var t = 0.0;
            // 1000 units/s with jittery 8..14 ms frames.
            var steps = new[] { 11, 8, 14, 11, 9, 13, 11, 12 };
            tracker.Add(TimeSpan.FromSeconds(t), new PointF(0, 0));
            foreach (var ms in steps)
            {
                t += ms / 1000.0;
                tracker.Add(TimeSpan.FromSeconds(t), new PointF(0, (float)(1000 * t)));
            }

            Assert.AreEqual(1000, tracker.GetVelocity().Y, 1);
            Assert.AreEqual(0, tracker.GetVelocity().X, 1e-3);
        }

        [TestMethod]
        public void Velocity_tracker_follows_a_decelerating_finger_and_ignores_old_samples()
        {
            var tracker = new TouchVelocityTracker();
            // Fast start long ago (outside the 100 ms horizon), then x = 2000t - 5000t² in the window.
            tracker.Add(TimeSpan.FromSeconds(-0.5), new PointF(-5000, 0));
            for (var i = 0; i <= 10; i++)
            {
                var t = i * 0.01;
                tracker.Add(TimeSpan.FromSeconds(t), new PointF((float)(2000 * t - 5000 * t * t), 0));
            }

            // Derivative at the newest sample (t = 0.1): 2000 - 10000·0.1 = 1000.
            Assert.AreEqual(1000, tracker.GetVelocity().X, 2);
        }

        [TestMethod]
        public void Velocity_tracker_keeps_one_sample_per_frame()
        {
            var tracker = new TouchVelocityTracker();
            tracker.Add(TimeSpan.FromMilliseconds(0), new PointF(0, 0));
            tracker.Add(TimeSpan.FromMilliseconds(10), new PointF(0, 5));
            tracker.Add(TimeSpan.FromMilliseconds(10), new PointF(0, 10)); // same frame: replaces

            Assert.AreEqual(1000, tracker.GetVelocity().Y, 1e-2);
        }

        private sealed class TestScreen : Screen
        {
        }

        private static int _frame;

        private static void Tick(ScreenEngine engine)
            => engine.Update(new GameTime(TimeSpan.FromMilliseconds(++_frame * 16), TimeSpan.FromMilliseconds(16)));

        private static void Touch(ScreenEngine engine, VirtualInputSource source, TouchLocationState state, float y)
        {
            source.SetTouches(new TouchCollection(new[] { new TouchLocation(1, state, new Vector2(100, y)) }));
            Tick(engine);
        }

        private static float FlingOnce(FlingPhysics physics, out float releaseOffset)
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 600);
            var viewer = new ScrollViewer { Content = new Border { Height = 50000 }, FlingPhysics = physics };
            var source = new VirtualInputSource();
            engine.NavigateToScreen(new TestScreen { Content = viewer, InputSource = source });
            Tick(engine);

            // Finger up at 20 units per 16 ms frame = 1250 units/s.
            Touch(engine, source, TouchLocationState.Pressed, 500);
            for (var i = 1; i <= 12; i++)
                Touch(engine, source, TouchLocationState.Moved, 500 - 20 * i);
            releaseOffset = viewer.Offset.Y;
            Touch(engine, source, TouchLocationState.Released, 500 - 20 * 12);
            source.SetTouches(new TouchCollection(Array.Empty<TouchLocation>()));
            for (var i = 0; i < 300; i++)
                Tick(engine);
            return viewer.Offset.Y;
        }

        [TestMethod]
        public void A_scroll_viewer_fling_coasts_the_trajectory_distance()
        {
            var android = FlingPhysics.Android();
            var end = FlingOnce(android, out var release);
            var expected = android.Start(1250).Distance;

            Assert.AreEqual(expected, end - release, expected * 0.01f, $"released at {release}, ended at {end}");
        }

        [TestMethod]
        public void IOS_and_Android_coast_differently_for_the_same_finger()
        {
            var androidEnd = FlingOnce(FlingPhysics.Android(), out var androidRelease);
            var iosEnd = FlingOnce(FlingPhysics.IOS, out var iosRelease);

            Assert.AreEqual(FlingPhysics.IOS.Start(1250).Distance, iosEnd - iosRelease, 8f);
            Assert.AreNotEqual(androidEnd - androidRelease, iosEnd - iosRelease, 50f);
        }
    }
}
