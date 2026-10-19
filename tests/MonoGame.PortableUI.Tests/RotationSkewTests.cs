using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>#144: Control.Rotation and Control.Skew, render-only transforms about TransformOrigin.</summary>
    [TestClass]
    [DoNotParallelize] // drives the global ScreenSystem.TotalTime clock
    public class RotationSkewTests
    {
        // Box 100x50 at (10, 20), centre (60, 45).
        private static Border CreateLaidOut()
        {
            var border = new Border
            {
                Width = 100,
                Height = 50,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            border.UpdateLayout(new Rect(10, 20, 300, 300));
            return border;
        }

        private static Vector2 Map(Control control, float x, float y)
            => Vector2.Transform(new Vector2(x, y), control.GetRenderTransform());

        private static void AssertPoint(Vector2 expected, Vector2 actual, string message)
        {
            Assert.AreEqual(expected.X, actual.X, 0.001f, message + " (x)");
            Assert.AreEqual(expected.Y, actual.Y, 0.001f, message + " (y)");
        }

        private static void Frame(Control control, double seconds)
        {
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(seconds);
            control.UpdateTimers();
        }

        [TestMethod]
        public void Rotation_turns_clockwise_about_the_centre()
        {
            var border = CreateLaidOut();
            border.Rotation = MathHelper.PiOver2;

            AssertPoint(new Vector2(60, 45), Map(border, 60, 45), "centre stays");
            // Right of the centre goes below it on a y-down screen.
            AssertPoint(new Vector2(60, 95), Map(border, 110, 45), "right edge midpoint turns down");
            Assert.IsTrue(border.HasOffAxisTransform);
        }

        [TestMethod]
        public void Rotation_uses_the_transform_origin_and_composes_with_scale_and_translation()
        {
            var border = CreateLaidOut();
            border.TransformOrigin = Vector2.Zero;
            border.Scale = new Vector2(2, 2);
            border.Rotation = MathHelper.Pi;
            border.Translation = new Vector2(5, 0);

            AssertPoint(new Vector2(15, 20), Map(border, 10, 20), "pivot moved by translation only");
            AssertPoint(new Vector2(-185, 20), Map(border, 110, 20), "scaled, then turned half way, then moved");
        }

        [TestMethod]
        public void Skew_leans_the_edges_about_the_origin()
        {
            var border = CreateLaidOut();
            border.Skew = new Vector2(MathHelper.PiOver4, 0);
            // X skew: a point 25 below the centre shifts right by 25 (tan 45°).
            AssertPoint(new Vector2(85, 70), Map(border, 60, 70), "bottom edge shifts right");
            AssertPoint(new Vector2(35, 20), Map(border, 60, 20), "top edge shifts left");

            border.Skew = new Vector2(0, MathHelper.PiOver4);
            AssertPoint(new Vector2(110, 95), Map(border, 110, 45), "right edge shifts down");
        }

        [TestMethod]
        public void Skew_is_clamped_short_of_a_right_angle()
        {
            var border = CreateLaidOut();
            border.Skew = new Vector2(MathHelper.PiOver2, -10);
            Assert.IsTrue(border.Skew.X < MathHelper.PiOver2 && border.Skew.X > 1.3f, $"clamped: {border.Skew}");
            Assert.AreEqual(-border.Skew.X, border.Skew.Y, 0.0001f);
            Assert.IsTrue(Math.Abs(border.GetRenderTransform().Determinant()) > 1e-3f, "stays invertible");
        }

        [TestMethod]
        public void Without_rotation_or_skew_the_box_stays_axis_aligned()
        {
            var border = CreateLaidOut();
            Assert.IsFalse(border.HasOffAxisTransform);
            Assert.AreEqual(Matrix.Identity, border.GetRenderTransform());

            border.Rotation = 0.3f;
            border.Rotation = 0;
            Assert.IsFalse(border.HasOffAxisTransform);
            Assert.AreEqual(Matrix.Identity, border.GetRenderTransform());
        }

        [TestMethod]
        public void Suppressing_the_off_axis_part_keeps_scale_and_translation()
        {
            var border = CreateLaidOut();
            border.Translation = new Vector2(5, 0);
            border.Rotation = 1;
            border.OffAxisSuppressed = true;
            try
            {
                Assert.IsFalse(border.HasOffAxisTransform);
                AssertPoint(new Vector2(15, 20), Map(border, 10, 20), "only the translation");
            }
            finally
            {
                border.OffAxisSuppressed = false;
            }
        }

        [TestMethod]
        public void Changing_rotation_or_skew_marks_a_cached_layer_dirty()
        {
            var child = new Border { Width = 20, Height = 20 };
            var cached = new Border { Content = child, CacheMode = CacheMode.Bitmap };
            var cache = new LayerCache { Dirty = false };
            cached.LayerCache = cache;

            child.Rotation = 0.5f;
            Assert.IsTrue(cache.Dirty);

            cache.Dirty = false;
            child.Skew = new Vector2(0.2f, 0);
            Assert.IsTrue(cache.Dirty);
        }

        [TestMethod]
        public void A_rotation_transition_animates_and_the_getter_returns_the_target()
        {
            var border = CreateLaidOut();
            Frame(border, 10);
            border.Transitions.Add(TransitionProperty.Rotation, TimeSpan.FromMilliseconds(100), Easings.Linear);
            border.Rotation = 1;

            Assert.AreEqual(1f, border.Rotation, "getter returns the target");
            Assert.AreEqual(0f, border.ShownRotation, "nothing drawn yet");
            Assert.IsTrue(border.IsTransitioning);

            Frame(border, 10.05);
            Assert.AreEqual(0.5f, border.ShownRotation, 0.02f, "half way");
            Frame(border, 10.2);
            Assert.AreEqual(1f, border.ShownRotation);
            Assert.IsFalse(border.IsTransitioning);
        }

        [TestMethod]
        public void Transform_transitions_include_rotation_and_skew()
        {
            var border = CreateLaidOut();
            Frame(border, 20);
            border.Transitions.Add(TransitionProperty.Transform, TimeSpan.FromMilliseconds(100));
            border.Skew = new Vector2(0.4f, 0);
            Assert.AreEqual(Vector2.Zero, border.ShownSkew);

            border.CompleteTransitions();
            Assert.AreEqual(new Vector2(0.4f, 0), border.ShownSkew, "completing snaps to the target");
        }

        [TestMethod]
        public void Animate_rotates_and_skews()
        {
            var border = CreateLaidOut();
            Frame(border, 30);
            border.Animate().RotateTo(MathHelper.Pi).SkewTo(new Vector2(0, 0.3f))
                .Duration(TimeSpan.FromMilliseconds(100)).Ease(Easings.Linear).Start();
            Frame(border, 30.05);
            Assert.AreEqual(MathHelper.PiOver2, border.ShownRotation, 0.05f);
            Assert.IsTrue(border.IsInMotion, "an animated rotation is motion");
            Frame(border, 30.2);
            Assert.AreEqual(MathHelper.Pi, border.ShownRotation, 0.0001f);
            Assert.AreEqual(0.3f, border.ShownSkew.Y, 0.0001f);
        }
    }
}
