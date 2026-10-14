using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class SpringAnimationTests
    {
        private sealed class TestControl : Control
        {
        }

        [TestInitialize]
        public void ResetTime()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
        }

        private static void StepTo(Control control, double seconds)
        {
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(seconds);
            control.UpdateTimers();
        }

        // Reference: fine semi-implicit Euler integration of m x'' = -k x - c x'.
        private static double Integrate(SpringSpec spring, double x, double v, double seconds)
        {
            const double dt = 1e-5;
            for (var t = 0.0; t < seconds; t += dt)
            {
                v += (-spring.Stiffness * x - spring.Damping * v) / spring.Mass * dt;
                x += v * dt;
            }
            return x;
        }

        [DataTestMethod]
        [DataRow(0.35)]
        [DataRow(1.0)]
        [DataRow(1.8)]
        public void Exact_solution_matches_integration_for_every_damping_regime(double dampingRatio)
        {
            var spring = SpringSpec.FromResponse(0.5, dampingRatio);
            spring.Evaluate(1, -3, 0, out var d0, out var v0);
            Assert.AreEqual(1, d0, 1e-9);
            Assert.AreEqual(-3, v0, 1e-9);

            spring.Evaluate(1, -3, 0.3, out var d, out _);
            Assert.AreEqual(Integrate(spring, 1, -3, 0.3), d, 1e-3);
        }

        [TestMethod]
        public void Bouncy_spring_overshoots_and_settles_exactly_on_the_target()
        {
            var control = new TestControl();
            var completed = 0;
            control.Animate().Scale(2).Spring(SpringSpec.Bouncy).OnCompleted(() => completed++).Start();

            var max = 1f;
            for (var t = 0.0; t < 5 && completed == 0; t += 1 / 60.0)
            {
                StepTo(control, t);
                max = Math.Max(max, control.Scale.X);
            }

            Assert.IsTrue(max > 2.05f, $"expected an overshoot, peak {max}");
            Assert.AreEqual(1, completed);
            Assert.AreEqual(new Vector2(2, 2), control.Scale);
        }

        [TestMethod]
        public void Gentle_spring_does_not_overshoot()
        {
            var control = new TestControl();
            control.Animate().TranslateTo(new Vector2(100, 0)).Spring(SpringSpec.Gentle).Start();

            for (var t = 0.0; t < 3; t += 1 / 60.0)
            {
                StepTo(control, t);
                Assert.IsTrue(control.Translation.X <= 100.0001f, $"overshoot at {t}: {control.Translation.X}");
            }

            Assert.AreEqual(100, control.Translation.X);
        }

        [TestMethod]
        public void Spring_taking_over_a_running_tween_keeps_its_velocity()
        {
            var control = new TestControl();
            control.Animate().TranslateTo(new Vector2(100, 0)).Duration(TimeSpan.FromMilliseconds(400)).Ease(Easings.Linear).Start();
            StepTo(control, 0.2);
            Assert.AreEqual(50, control.Translation.X, 0.01f);

            // Retarget backwards: a spring from rest would move toward 0 at once, an inherited
            // velocity of +250 px/s carries the motion forward first.
            control.Animate().TranslateTo(Vector2.Zero).Spring(SpringSpec.Snappy).Start();
            StepTo(control, 0.2 + 1 / 60.0);
            Assert.IsTrue(control.Translation.X > 50, $"expected continued motion, got {control.Translation.X}");

            StepTo(control, 3);
            Assert.AreEqual(0, control.Translation.X);
        }

        [TestMethod]
        public void Per_axis_springs_move_x_and_y_out_of_phase()
        {
            var control = new TestControl();
            control.Animate().Scale(new Vector2(1.5f, 1.5f)).Spring(SpringSpec.Wobbly, SpringSpec.Bouncy).Start();

            StepTo(control, 0.15);
            Assert.AreNotEqual(control.Scale.X, control.Scale.Y);
        }

        [TestMethod]
        public void Default_spring_spec_is_rejected()
        {
            var control = new TestControl();
            Assert.ThrowsExactly<ArgumentException>(() => control.Animate().Spring(default(SpringSpec)));
        }

        [TestMethod]
        public void Spring_updates_allocate_nothing()
        {
            var control = new TestControl();
            control.Animate().Scale(1.2).TranslateTo(new Vector2(10, 0)).FadeTo(0.5).Spring(SpringSpec.Wobbly).Start();
            StepTo(control, 0.01);

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 2; i < 60; i++)
                StepTo(control, i / 60.0);
            Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before);
        }
    }
}
