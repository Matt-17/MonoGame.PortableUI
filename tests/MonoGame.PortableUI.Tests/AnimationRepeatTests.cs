using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>Repeat, Loop, Yoyo, DelayStart, StartAsync and Stagger.</summary>
    [TestClass]
    public class AnimationRepeatTests
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
        public void Repeat_jumps_back_to_the_first_start_each_time_and_completes_once()
        {
            var control = new TestControl();
            var completed = 0;
            control.Animate()
                .TranslateTo(new Vector2(100, 0)).Duration(Ms(100)).Ease(Easings.Linear)
                .Repeat(3)
                .OnCompleted(() => completed++)
                .Start();

            At(control, 150);
            Assert.AreEqual(50, control.Translation.X, 0.01f);
            At(control, 250);
            Assert.AreEqual(50, control.Translation.X, 0.01f);
            Assert.AreEqual(0, completed);
            At(control, 300);
            Assert.AreEqual(100, control.Translation.X, 0.01f);
            At(control, 400);
            Assert.AreEqual(1, completed);
        }

        [TestMethod]
        public void Yoyo_plays_back_along_the_reversed_curve()
        {
            var control = new TestControl();
            control.Animate()
                .TranslateTo(new Vector2(100, 0)).Duration(Ms(100)).Ease(Easings.QuadOut)
                .Repeat(2).Yoyo()
                .Start();

            At(control, 100);
            Assert.AreEqual(100, control.Translation.X, 0.01f);
            // Backwards in time: 25 ms into the return = the forward curve at 75 %.
            At(control, 125);
            Assert.AreEqual(100 * Easings.QuadOut(0.75), control.Translation.X, 0.01f);
            At(control, 200);
            Assert.AreEqual(0, control.Translation.X, 0.01f);
        }

        [TestMethod]
        public void Yoyo_reverses_the_order_of_steps()
        {
            var control = new TestControl();
            control.Animate()
                .Scale(2).Duration(Ms(100)).Ease(Easings.Linear)
                .Then()
                .TranslateTo(new Vector2(100, 0)).Duration(Ms(100)).Ease(Easings.Linear)
                .Repeat(2).Yoyo()
                .Start();

            At(control, 250);
            Assert.AreEqual(50, control.Translation.X, 0.01f, "translation undone first");
            Assert.AreEqual(2f, control.Scale.X, 0.001f);
            At(control, 350);
            Assert.AreEqual(0, control.Translation.X, 0.01f);
            Assert.AreEqual(1.5f, control.Scale.X, 0.001f, "then the scale");
            At(control, 400);
            Assert.AreEqual(1f, control.Scale.X, 0.001f);
        }

        [TestMethod]
        public void Loop_runs_until_cancelled()
        {
            var control = new TestControl();
            var completed = 0;
            var pulse = control.Animate()
                .Scale(1.2).Duration(Ms(100)).Ease(Easings.Linear)
                .Loop().Yoyo()
                .OnCompleted(() => completed++);
            pulse.Start();

            At(control, 10_050);
            Assert.IsTrue(pulse.IsRunning);
            Assert.AreEqual(1.1f, control.Scale.X, 0.001f);
            pulse.Cancel();
            Assert.IsFalse(pulse.IsRunning);
            Assert.AreEqual(0, completed);
        }

        [TestMethod]
        public void A_spring_loop_swings_to_and_fro()
        {
            var control = new TestControl();
            control.Animate().TranslateTo(new Vector2(100, 0)).Spring(SpringSpec.Snappy).Loop().Yoyo().Start();

            var reachedFar = 0;
            var reachedHome = 0;
            var wasFar = false;
            for (var t = 0.0; t < 6000; t += 1000 / 60.0)
            {
                At(control, t);
                var x = control.Translation.X;
                if (!wasFar && x > 99) { reachedFar++; wasFar = true; }
                if (wasFar && x < 1) { reachedHome++; wasFar = false; }
            }

            Assert.IsTrue(reachedFar >= 2 && reachedHome >= 2, $"far {reachedFar}, home {reachedHome}");
        }

        [TestMethod]
        public void A_zero_length_loop_does_not_hang_an_update()
        {
            var control = new TestControl();
            var loop = control.Animate().FadeTo(0).Duration(TimeSpan.Zero).Loop().Start();
            At(control, 100);
            At(control, 200);
            Assert.IsTrue(loop.IsRunning);
            loop.Cancel();
        }

        [TestMethod]
        public void DelayStart_waits_and_captures_when_the_motion_begins()
        {
            var control = new TestControl();
            control.Animate().FadeTo(0).Duration(Ms(100)).Ease(Easings.Linear).DelayStart(Ms(200)).Start();

            At(control, 100);
            Assert.AreEqual(1, control.Opacity, 0.001);
            control.Opacity = 0.5;
            At(control, 250);
            Assert.AreEqual(0.25, control.Opacity, 0.001);
        }

        [TestMethod]
        public void DelayStart_applies_once_not_per_repetition()
        {
            var control = new TestControl();
            control.Animate()
                .TranslateTo(new Vector2(100, 0)).Duration(Ms(100)).Ease(Easings.Linear)
                .DelayStart(Ms(100)).Repeat(2)
                .Start();

            At(control, 250);
            Assert.AreEqual(50, control.Translation.X, 0.01f);
        }

        [TestMethod]
        public void StartAsync_completes_true_at_the_end()
        {
            var control = new TestControl();
            var task = control.Animate().FadeTo(0).Duration(Ms(100)).StartAsync();

            At(control, 50);
            Assert.IsFalse(task.IsCompleted);
            At(control, 100);
            Assert.IsTrue(task.IsCompletedSuccessfully);
            Assert.IsTrue(task.Result);
        }

        [TestMethod]
        public void StartAsync_completes_false_when_cancelled_or_replaced()
        {
            var control = new TestControl();
            var cancelled = control.Animate().FadeTo(0).Duration(Ms(100));
            var first = cancelled.StartAsync();
            cancelled.Cancel();
            Assert.IsTrue(first.IsCompleted);
            Assert.IsFalse(first.Result);

            var replaced = control.Animate().FadeTo(0).Duration(Ms(100)).StartAsync();
            control.Animate().FadeTo(1).Duration(Ms(100)).Start();
            Assert.IsTrue(replaced.IsCompleted);
            Assert.IsFalse(replaced.Result);
        }

        [TestMethod]
        public void StartAsync_continuation_runs_in_the_update_that_ends_the_animation()
        {
            var control = new TestControl();
            var continued = false;
            async void Run()
            {
                await control.Animate().Scale(2).Duration(Ms(100)).StartAsync();
                continued = true;
            }

            Run();
            At(control, 50);
            Assert.IsFalse(continued);
            At(control, 100);
            Assert.IsTrue(continued);
        }

        [TestMethod]
        public void Stagger_starts_each_control_one_interval_later()
        {
            var controls = new Control[] { new TestControl(), new TestControl(), new TestControl() };
            foreach (var control in controls)
                control.Opacity = 0;
            var animations = controls.Stagger(Ms(100), c => c.Animate().FadeTo(1).Duration(Ms(100)).Ease(Easings.Linear));
            Assert.AreEqual(3, animations.Count);

            ScreenSystem.TotalTime = Ms(150);
            foreach (var control in controls)
                control.UpdateTimers();

            Assert.AreEqual(1, controls[0].Opacity, 0.001);
            Assert.AreEqual(0.5, controls[1].Opacity, 0.001);
            Assert.AreEqual(0, controls[2].Opacity, 0.001);
        }

        [TestMethod]
        public void A_looping_yoyo_allocates_nothing_per_frame()
        {
            var control = new TestControl();
            var loop = control.Animate()
                .Scale(1.2).Duration(Ms(120))
                .Then().TranslateTo(new Vector2(10, 0)).Spring(SpringSpec.Snappy)
                .Loop().Yoyo()
                .Start();
            At(control, 1);

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var t = 2.0; t < 4000; t += 1000 / 60.0)
                At(control, t);
            Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before);
            loop.Cancel();
        }
    }
}
