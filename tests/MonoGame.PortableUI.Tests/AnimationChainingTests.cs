using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>Animation steps: Then(), With(), Delay(), Restart().</summary>
    [TestClass]
    public class AnimationChainingTests
    {
        private sealed class TestControl : Control
        {
        }

        [TestInitialize]
        public void ResetTime()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
        }

        private static TimeSpan Ms(double milliseconds) => TimeSpan.FromMilliseconds(milliseconds);

        private static void At(Control control, double milliseconds)
        {
            ScreenSystem.TotalTime = Ms(milliseconds);
            control.UpdateTimers();
        }

        [TestMethod]
        public void Then_runs_the_next_step_from_where_the_previous_ended()
        {
            var control = new TestControl();
            control.Animate()
                .Scale(2).Duration(Ms(100)).Ease(Easings.Linear)
                .Then()
                .Scale(1).Duration(Ms(100)).Ease(Easings.Linear)
                .Start();

            At(control, 50);
            Assert.AreEqual(1.5f, control.Scale.X, 0.001f);
            At(control, 100);
            Assert.AreEqual(2f, control.Scale.X, 0.001f);
            At(control, 150);
            Assert.AreEqual(1.5f, control.Scale.X, 0.001f);
            At(control, 200);
            Assert.AreEqual(1f, control.Scale.X, 0.001f);
        }

        [TestMethod]
        public void A_frame_spanning_two_steps_keeps_the_leftover_time()
        {
            var control = new TestControl();
            control.Animate()
                .TranslateTo(new Vector2(100, 0)).Duration(Ms(100)).Ease(Easings.Linear)
                .Then()
                .TranslateTo(new Vector2(100, 100)).Duration(Ms(100)).Ease(Easings.Linear)
                .Start();

            // One long frame: the second step has already run for 30 ms.
            At(control, 130);
            Assert.AreEqual(100, control.Translation.X, 0.001f);
            Assert.AreEqual(30, control.Translation.Y, 0.001f);
        }

        [TestMethod]
        public void With_runs_a_parallel_track_and_the_step_waits_for_the_slowest()
        {
            var control = new TestControl();
            control.Animate()
                .Scale(2).Duration(Ms(100)).Ease(Easings.Linear)
                .With()
                .FadeTo(0).Duration(Ms(300)).Ease(Easings.Linear)
                .Then()
                .Scale(1).Duration(Ms(100)).Ease(Easings.Linear)
                .Start();

            At(control, 100);
            Assert.AreEqual(2f, control.Scale.X, 0.001f);
            Assert.AreEqual(2 / 3.0, control.Opacity, 0.001);

            At(control, 200);
            Assert.AreEqual(2f, control.Scale.X, 0.001f, "next step waits for the fade");
            Assert.AreEqual(1 / 3.0, control.Opacity, 0.001);

            At(control, 350);
            Assert.AreEqual(0, control.Opacity, 0.001);
            Assert.AreEqual(1.5f, control.Scale.X, 0.001f);
        }

        [TestMethod]
        public void Delay_holds_the_track_and_captures_its_start_when_it_moves()
        {
            var control = new TestControl();
            control.Animate()
                .FadeTo(0).Delay(Ms(100)).Duration(Ms(100)).Ease(Easings.Linear)
                .Start();

            At(control, 50);
            Assert.AreEqual(1, control.Opacity, 0.001);
            control.Opacity = 0.8; // changed during the delay: the tween starts from here
            At(control, 100);
            Assert.AreEqual(0.8, control.Opacity, 0.001);
            At(control, 150);
            Assert.AreEqual(0.4, control.Opacity, 0.001);
        }

        [TestMethod]
        public void Then_with_delay_pauses_between_steps()
        {
            var control = new TestControl();
            control.Animate()
                .Scale(2).Duration(Ms(100)).Ease(Easings.Linear)
                .Then(Ms(200))
                .Scale(1).Duration(Ms(100)).Ease(Easings.Linear)
                .Start();

            At(control, 250);
            Assert.AreEqual(2f, control.Scale.X, 0.001f);
            At(control, 350);
            Assert.AreEqual(1.5f, control.Scale.X, 0.001f);
        }

        [TestMethod]
        public void Completion_runs_once_after_the_last_step()
        {
            var control = new TestControl();
            var completed = 0;
            control.Animate()
                .Scale(2).Duration(Ms(100))
                .Then()
                .Scale(1).Duration(Ms(100))
                .OnCompleted(() => completed++)
                .Start();

            At(control, 100);
            Assert.AreEqual(0, completed);
            At(control, 200);
            At(control, 300);
            Assert.AreEqual(1, completed);
        }

        [TestMethod]
        public void Cancel_with_complete_jumps_to_the_last_targets()
        {
            var control = new TestControl();
            var animation = control.Animate()
                .Scale(2).With().FadeTo(0.5)
                .Then()
                .Scale(3)
                .Start();

            At(control, 10);
            animation.Cancel(complete: true);
            Assert.AreEqual(3f, control.Scale.X, 0.001f);
            Assert.AreEqual(0.5, control.Opacity, 0.001);
            Assert.IsFalse(animation.IsRunning);
        }

        [TestMethod]
        public void Restart_reuses_the_animation_from_the_current_values()
        {
            var control = new TestControl();
            var pulse = control.Animate()
                .Scale(1.2).Duration(Ms(100)).Ease(Easings.Linear)
                .Then()
                .Scale(1).Duration(Ms(100)).Ease(Easings.Linear);

            pulse.Start();
            At(control, 200);
            Assert.AreEqual(1f, control.Scale.X, 0.001f);
            Assert.IsFalse(pulse.IsRunning);

            pulse.Start();
            At(control, 250);
            Assert.AreEqual(1.1f, control.Scale.X, 0.001f);

            // Restart mid-run: from 1.1 again towards 1.2.
            pulse.Restart();
            At(control, 300);
            Assert.AreEqual(1.15f, control.Scale.X, 0.001f);
        }

        [TestMethod]
        public void A_spring_step_inherits_the_velocity_of_the_eased_step_before()
        {
            var control = new TestControl();
            control.Animate()
                .TranslateTo(new Vector2(100, 0)).Duration(Ms(200)).Ease(Easings.Linear)
                .Then()
                .TranslateTo(new Vector2(100, 0)).Spring(SpringSpec.Gentle)
                .Start();

            // Arrives at 500 units per second: even a non-bouncing spring carries past 100 first.
            var max = 0f;
            for (var t = 0.0; t < 2000; t += 1000 / 60.0)
            {
                At(control, t);
                max = Math.Max(max, control.Translation.X);
            }

            Assert.IsTrue(max > 101, $"peak {max}");
            Assert.AreEqual(100, control.Translation.X);
        }

        [TestMethod]
        public void A_delay_alone_is_a_timer()
        {
            var control = new TestControl();
            var fired = 0;
            control.Animate().Delay(Ms(500)).OnCompleted(() => fired++).Start();

            At(control, 499);
            Assert.AreEqual(0, fired);
            At(control, 500);
            Assert.AreEqual(1, fired);
        }

        [TestMethod]
        public void A_new_animation_takes_its_property_out_of_later_steps_of_a_running_chain()
        {
            var control = new TestControl();
            control.Animate()
                .FadeTo(0.5).Duration(Ms(100)).Ease(Easings.Linear)
                .Then()
                .Scale(3).Duration(Ms(100)).Ease(Easings.Linear)
                .Start();

            control.Animate().Scale(2).Duration(Ms(100)).Ease(Easings.Linear).Start();
            At(control, 200);
            Assert.AreEqual(2f, control.Scale.X, 0.001f);
            Assert.AreEqual(0.5, control.Opacity, 0.001);
        }

        [TestMethod]
        public void A_chained_run_allocates_nothing_per_frame()
        {
            var control = new TestControl();
            var animation = control.Animate()
                .Scale(1.2).Duration(Ms(100))
                .With().FadeTo(0.5).Spring(SpringSpec.Bouncy)
                .Then(Ms(50))
                .Scale(1).Spring(SpringSpec.Wobbly);
            animation.Start();
            At(control, 1);

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var t = 2.0; t < 3000; t += 1000 / 60.0)
                At(control, t);
            Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before);
            Assert.IsFalse(animation.IsRunning);
        }
    }
}
