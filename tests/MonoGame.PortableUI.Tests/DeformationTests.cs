using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>Control.Deformation: the soft-body grid (drawing is covered by the demo recording).</summary>
    [TestClass]
    public class DeformationTests
    {
        private sealed class TestControl : Control
        {
        }

        private static readonly Vector2 Size = new Vector2(100, 40);

        private static void Frame(Control control, double seconds)
        {
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(seconds);
            control.UpdateTimers();
        }

        [TestMethod]
        public void A_moving_control_trails_at_the_tail_and_swells_at_the_front()
        {
            var deformation = new Deformation();
            // Moving right at full speed for a few frames.
            for (var i = 0; i < 12; i++)
                deformation.Update(Size, new Vector2(1000, 0), 1 / 60.0);

            Assert.IsTrue(deformation.IsActive);
            var tail = deformation.OffsetAt(0, 0.5f, Size, 0);
            var front = deformation.OffsetAt(1, 0.5f, Size, 0);
            Assert.IsTrue(tail.X < -5, $"the tail lags: {tail}");
            Assert.IsTrue(Math.Abs(front.X) < Math.Abs(tail.X) / 3, $"the front leads: {front}");

            var tailTop = deformation.OffsetAt(0, 0, Size, 0);
            var frontTop = deformation.OffsetAt(1, 0, Size, 0);
            Assert.IsTrue(tailTop.Y > 0, $"the tail narrows (top edge moves down): {tailTop}");
            Assert.IsTrue(frontTop.Y < 0, $"the front swells (top edge moves up): {frontTop}");
        }

        [TestMethod]
        public void It_wobbles_out_and_comes_to_rest_when_the_motion_stops()
        {
            var deformation = new Deformation();
            for (var i = 0; i < 12; i++)
                deformation.Update(Size, new Vector2(1000, 0), 1 / 60.0);

            var crossedRest = false;
            var previous = deformation.OffsetAt(0, 0.5f, Size, 0).X;
            for (var i = 0; i < 600 && deformation.IsActive; i++)
            {
                deformation.Update(Size, Vector2.Zero, 1 / 60.0);
                var x = deformation.OffsetAt(0, 0.5f, Size, 0).X;
                crossedRest |= previous < 0 && x > 0;
                previous = x;
            }

            Assert.IsTrue(crossedRest, "the tail swings past its rest position (jelly)");
            Assert.IsFalse(deformation.IsActive, "settles");
            Assert.AreEqual(Vector2.Zero, deformation.OffsetAt(0.3f, 0.7f, Size, 0));
        }

        [TestMethod]
        public void A_wobble_kick_deforms_and_settles_without_motion()
        {
            var deformation = new Deformation();
            deformation.Update(Size, Vector2.Zero, 1 / 60.0);
            deformation.Wobble();
            Assert.IsTrue(deformation.IsActive);
            deformation.Update(Size, Vector2.Zero, 1 / 30.0);
            var corner = deformation.OffsetAt(1, 1, Size, 0);
            Assert.IsTrue(corner.X > 0.5f && corner.Y < -0.5f, $"corners swing (x out, y in): {corner}");
            for (var i = 0; i < 600 && deformation.IsActive; i++)
                deformation.Update(Size, Vector2.Zero, 1 / 60.0);
            Assert.IsFalse(deformation.IsActive);
        }

        [TestMethod]
        public void Static_deformers_shape_the_mesh_without_motion()
        {
            var deformation = new Deformation { Bend = 0.25f };
            Assert.IsTrue(deformation.IsActive);
            Assert.AreEqual(10f, deformation.OffsetAt(0, 0.5f, Size, 0).Y, 0.001f, "ends move down by a quarter of the height");
            Assert.AreEqual(0f, deformation.OffsetAt(0.5f, 0.5f, Size, 0).Y, 0.001f, "middle stays");

            deformation = new Deformation { Bulge = 0.2f };
            var middleRight = deformation.OffsetAt(0.75f, 0.5f, Size, 0);
            Assert.IsTrue(middleRight.X > 0, "bulges outwards");
            Assert.AreEqual(Vector2.Zero, deformation.OffsetAt(1, 1, Size, 0), "corners stay");

            deformation = new Deformation { Wave = 0.1f };
            Assert.AreNotEqual(deformation.OffsetAt(0.25f, 0.5f, Size, 0), deformation.OffsetAt(0.25f, 0.5f, Size, 0.25), "the wave travels");
        }

        [TestMethod]
        public void A_control_feeds_its_translation_motion_into_the_deformation()
        {
            var control = new TestControl { Deformation = new Deformation() };
            control.UpdateLayout(new Rect(0, 0, 100, 40));
            Frame(control, 0);
            for (var i = 1; i <= 10; i++)
            {
                control.Translation = new Vector2(i * 16, 0);
                Frame(control, i / 60.0);
            }

            Assert.IsTrue(control.Deformation!.IsActive);
            Assert.IsTrue(control.IsInMotion, "drawn with subpixel precision while deformed");
        }

        [TestMethod]
        public void Updating_a_deformation_allocates_nothing()
        {
            var deformation = new Deformation(12, 12);
            deformation.Update(Size, new Vector2(500, 200), 1 / 60.0);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 200; i++)
            {
                deformation.Update(Size, i < 100 ? new Vector2(500, 200) : Vector2.Zero, 1 / 60.0);
                deformation.OffsetAt(0.4f, 0.6f, Size, i / 60.0);
            }
            Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before);
        }
    }
}
