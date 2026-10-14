using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>Subpixel drawing while in motion, and PortableTheme.PixelMotion for pixel-art themes.</summary>
    [TestClass]
    public class MotionRenderingTests
    {
        private sealed class Probe : Control
        {
            public Vector2 Scale2 => RenderScale;
            public bool Snaps => SnapsDrawing;
        }

        [TestInitialize]
        public void ResetTime()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
        }

        private static Probe InTheme(bool pixelMotion)
        {
            var island = new ThemeIsland { Theme = new PortableTheme { PixelMotion = pixelMotion } };
            var probe = new Probe { SnapToPixel = true };
            island.Content = probe;
            return probe;
        }

        [TestMethod]
        public void A_control_at_rest_draws_snapped()
        {
            var probe = InTheme(pixelMotion: false);
            probe.SetRenderState(1, Vector2.One, moving: false);
            Assert.IsFalse(probe.DrawsSubpixel);
            Assert.IsTrue(probe.Snaps);
        }

        [TestMethod]
        public void A_moving_control_draws_with_subpixel_precision()
        {
            var probe = InTheme(pixelMotion: false);
            probe.SetRenderState(1, new Vector2(1.03f), moving: true, restScale: Vector2.One);
            Assert.IsTrue(probe.DrawsSubpixel);
            Assert.IsFalse(probe.Snaps, "text follows the shape instead of snapping");
            Assert.AreEqual(new Vector2(1.03f), probe.Scale2, "the scale stays exact");
        }

        [TestMethod]
        public void Pixel_motion_keeps_snapping_and_steps_the_scale_in_whole_multiples()
        {
            var probe = InTheme(pixelMotion: true);
            probe.SetRenderState(1, new Vector2(1.03f, 1.6f), moving: true, restScale: Vector2.One);
            Assert.IsFalse(probe.DrawsSubpixel);
            Assert.IsTrue(probe.Snaps);
            Assert.AreEqual(new Vector2(1, 2), probe.Scale2);

            // On a 2x layout the motion's share is stepped, not the layout scale.
            probe.SetRenderState(1, new Vector2(2.1f), moving: true, restScale: new Vector2(2));
            Assert.AreEqual(new Vector2(2), probe.Scale2);

            // Shrinking away stays smooth.
            probe.SetRenderState(1, new Vector2(0.4f), moving: true, restScale: Vector2.One);
            Assert.AreEqual(new Vector2(0.4f), probe.Scale2);
        }

        [TestMethod]
        public void Animations_transitions_and_breathing_count_as_motion()
        {
            var control = new Probe();
            Assert.IsFalse(control.IsInMotion);

            control.Animate().Scale(1.2).Duration(TimeSpan.FromMilliseconds(100)).Start();
            Assert.IsTrue(control.IsInMotion);
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(200);
            control.UpdateTimers();
            Assert.IsFalse(control.IsInMotion, "at rest again");

            control.Transitions.Add(TransitionProperty.Translation, TimeSpan.FromMilliseconds(100));
            control.Translation = new Vector2(10, 0);
            Assert.IsTrue(control.IsInMotion);
        }
    }
}
