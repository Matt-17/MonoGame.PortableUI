using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class SquashStretchTests
    {
        private sealed class TestControl : Control
        {
        }

        [TestInitialize]
        public void ResetTime()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
        }

        private static void Frame(Control control, double seconds)
        {
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(seconds);
            control.UpdateTimers();
        }

        private static float Determinant2D(Matrix m) => m.M11 * m.M22 - m.M12 * m.M21;

        [TestMethod]
        public void Fast_motion_stretches_along_the_direction_and_keeps_the_area()
        {
            var control = new TestControl { SquashStretch = 1 };
            control.UpdateLayout(new Rect(0, 0, 40, 40));
            Frame(control, 0);

            // 2000 units per second to the right.
            var t = 0.0;
            for (var i = 1; i <= 30; i++)
            {
                t = i / 60.0;
                control.Translation = new Vector2((float)(2000 * t), 0);
                Frame(control, t);
            }

            Assert.IsTrue(control.CurrentStretch > 1.3f, $"stretch {control.CurrentStretch}");
            var diagonal = control.StretchDiagonal;
            Assert.IsTrue(diagonal.X > 1 && diagonal.Y < 1, $"wide along x, narrow along y: {diagonal}");
            Assert.AreEqual(1, Determinant2D(control.GetRenderTransform()), 0.001f, "area preserved");
        }

        [TestMethod]
        public void A_sudden_stop_swings_into_a_squash_and_comes_to_rest()
        {
            var control = new TestControl { SquashStretch = 1 };
            Frame(control, 0);
            for (var i = 1; i <= 30; i++)
            {
                control.Translation = new Vector2(0, 2000 * i / 60f);
                Frame(control, i / 60.0);
            }

            var min = float.MaxValue;
            var t = 0.5;
            for (; t < 4 && control.IsMotionDeformed; t += 1 / 60.0)
            {
                Frame(control, t);
                min = Math.Min(min, control.CurrentStretch);
            }

            Assert.IsTrue(min < 0.97f, $"expected a squash after stopping, min {min}");
            Assert.IsFalse(control.IsMotionDeformed, "comes to rest");
            Assert.AreEqual(1, control.CurrentStretch);
            Assert.AreEqual(Matrix.CreateTranslation(0, 1000, 0), control.GetRenderTransform());
        }

        [TestMethod]
        public void Squash_impulse_swings_into_the_squash_and_back_without_SquashStretch()
        {
            var control = new TestControl();
            control.UpdateLayout(new Rect(0, 0, 40, 40));
            Frame(control, 0);
            control.Squash(0.3f);
            Assert.AreEqual(1f, control.CurrentStretch, 0.0001f, "no jump: the impulse starts from the current shape");

            var lowest = 1f;
            var wideAndFlat = false;
            for (var t = 1 / 60.0; t < 4 && control.IsMotionDeformed; t += 1 / 60.0)
            {
                Frame(control, t);
                lowest = Math.Min(lowest, control.CurrentStretch);
                var diagonal = control.StretchDiagonal;
                wideAndFlat |= diagonal.Y < 0.8f && diagonal.X > 1.2f;
            }

            Assert.AreEqual(0.7f, lowest, 0.04f, "swings out by about the amount");
            Assert.IsTrue(wideAndFlat, "flat and wide at the peak");
            Assert.IsFalse(control.IsMotionDeformed);
            Assert.AreEqual(Matrix.Identity, control.GetRenderTransform());
        }

        [TestMethod]
        public void Squash_continues_from_a_stretch_without_a_jump()
        {
            var control = new TestControl { SquashStretch = 1.2f, TransformOrigin = new Vector2(0.5f, 1) };
            control.UpdateLayout(new Rect(0, 0, 40, 40));
            // Falling fast: stretched along the fall.
            for (var i = 0; i <= 10; i++)
            {
                control.Translation = new Vector2(0, i * 20);
                Frame(control, i / 60.0);
            }

            var before = control.StretchDiagonal;
            Assert.IsTrue(before.Y > 1.05f, $"stretched while falling: {before}");
            control.Squash(0.3f);
            Assert.AreEqual(before, control.StretchDiagonal, "the shape is continuous at the impact");

            var flattest = float.MaxValue;
            for (var t = 11 / 60.0; t < 1; t += 1 / 60.0)
            {
                Frame(control, t);
                flattest = Math.Min(flattest, control.StretchDiagonal.Y);
            }
            Assert.IsTrue(flattest < 0.85f, $"then squashes: {flattest}");
        }

        [TestMethod]
        public void Squash_pivots_about_the_transform_origin()
        {
            var control = new TestControl { TransformOrigin = new Vector2(0.5f, 1) };
            control.UpdateLayout(new Rect(0, 0, 40, 40));
            Frame(control, 0);
            control.Squash(0.5f);
            for (var t = 1 / 60.0; t < 0.12; t += 1 / 60.0)
                Frame(control, t);

            var stretch = control.StretchDiagonal.Y;
            Assert.IsTrue(stretch < 0.8f, $"squashed: {stretch}");
            var bottom = Vector2.Transform(new Vector2(20, 40), control.GetRenderTransform());
            Assert.AreEqual(40, bottom.Y, 0.001f, "bottom edge stays on the ground");
            var top = Vector2.Transform(new Vector2(20, 0), control.GetRenderTransform());
            Assert.AreEqual(40 - 40 * stretch, top.Y, 0.001f, "the top comes down");
        }

        [TestMethod]
        public void Diagonal_motion_keeps_the_area()
        {
            var control = new TestControl { SquashStretch = 2 };
            control.UpdateLayout(new Rect(0, 0, 40, 40));
            Frame(control, 0);
            for (var i = 1; i <= 20; i++)
            {
                control.Translation = new Vector2(1500 * i / 60f, 1500 * i / 60f);
                Frame(control, i / 60.0);
            }

            Assert.IsTrue(control.CurrentStretch > 1.2f);
            Assert.AreEqual(1, Determinant2D(control.GetRenderTransform()), 0.001f);
        }

        [TestMethod]
        public void Invalid_spring_is_rejected()
        {
            var control = new TestControl();
            Assert.ThrowsExactly<ArgumentException>(() => control.SquashStretchSpring = default);
        }

        [TestMethod]
        public void Motion_updates_allocate_nothing()
        {
            var control = new TestControl { SquashStretch = 1 };
            Frame(control, 0);
            control.Translation = new Vector2(10, 0);
            Frame(control, 1 / 60.0);

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 2; i < 60; i++)
            {
                control.Translation = new Vector2(10 * i, 0);
                Frame(control, i / 60.0);
            }
            Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before);
        }
    }
}
