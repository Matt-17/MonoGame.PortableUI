using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using ControlAnimation = MonoGame.PortableUI.Animation.Animation;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class MotionPresetTests
    {
        private sealed class TestControl : Control
        {
        }

        [TestInitialize]
        public void ResetTime()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
        }

        private static void Run(Control control, ControlAnimation animation, Action<double> perFrame)
        {
            for (var t = 0.0; t < 5 && animation.IsRunning; t += 1 / 60.0)
            {
                ScreenSystem.TotalTime = TimeSpan.FromSeconds(t);
                control.UpdateTimers();
                perFrame(t);
            }

            Assert.IsFalse(animation.IsRunning, "settles");
        }

        [TestMethod]
        public void Pop_swells_and_settles_at_scale_one()
        {
            var control = new TestControl();
            var peak = 1f;
            var animation = control.Pop();
            Run(control, animation, _ => peak = Math.Max(peak, control.Scale.X));

            Assert.IsTrue(peak > 1.12f, $"peak {peak}");
            Assert.AreEqual(Vector2.One, control.Scale);
        }

        [TestMethod]
        public void Jelly_swings_the_axes_against_each_other_and_settles()
        {
            var control = new TestControl();
            var wideAndFlat = false;
            var narrowAndTall = false;
            var animation = control.Jelly();
            Run(control, animation, _ =>
            {
                wideAndFlat |= control.Scale.X > 1.05f && control.Scale.Y < 0.95f;
                narrowAndTall |= control.Scale.X < 0.99f && control.Scale.Y > 1.01f;
            });

            Assert.IsTrue(wideAndFlat, "squeezed");
            Assert.IsTrue(narrowAndTall, "swings back past the rest shape");
            Assert.AreEqual(Vector2.One, control.Scale);
        }

        [TestMethod]
        public void Shake_rings_out_around_the_rest_position()
        {
            var control = new TestControl();
            var crossings = 0;
            var previous = 0f;
            var animation = control.Shake();
            Run(control, animation, _ =>
            {
                var x = control.Translation.X;
                if (previous > 0.5f && x < -0.5f || previous < -0.5f && x > 0.5f)
                    crossings++;
                if (Math.Abs(x) > 0.5f)
                    previous = x;
            });

            Assert.IsTrue(crossings >= 3, $"crossings {crossings}");
            Assert.AreEqual(Vector2.Zero, control.Translation);
        }

        [TestMethod]
        public void Breathe_pulses_irregularly_and_stops_when_switched_off()
        {
            var control = new TestControl { Breathe = 0.04f };
            control.UpdateLayout(new Rect(0, 0, 40, 40));
            var min = float.MaxValue;
            var max = float.MinValue;
            var axesDiffer = false;
            for (var t = 0.0; t < 6.4; t += 1 / 30.0)
            {
                ScreenSystem.TotalTime = TimeSpan.FromSeconds(t);
                control.UpdateTimers();
                var scale = control.StretchDiagonal;
                min = Math.Min(min, scale.X);
                max = Math.Max(max, scale.X);
                axesDiffer |= Math.Abs(scale.X - scale.Y) > 0.001f;
            }

            Assert.IsTrue(max > 1.02f && min < 0.98f, $"range {min}..{max}");
            Assert.IsTrue(max <= 1.0401f && min >= 0.9599f, "bounded by the amplitude");
            Assert.IsTrue(axesDiffer, "axes out of phase");

            control.Breathe = 0;
            Assert.AreEqual(Matrix.Identity, control.GetRenderTransform());
            Assert.IsFalse(control.IsMotionDeformed);
        }
    }
}
