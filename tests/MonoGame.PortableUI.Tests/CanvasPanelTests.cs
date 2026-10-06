using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class CanvasPanelTests
    {
        private static Border Box(float width = 20, float height = 10) => new Border { Width = width, Height = height };

        private static void AssertRect(Rect actual, float left, float top, float width, float height)
        {
            Assert.AreEqual(left, actual.Left, 0.001f, "left");
            Assert.AreEqual(top, actual.Top, 0.001f, "top");
            Assert.AreEqual(width, actual.Width, 0.001f, "width");
            Assert.AreEqual(height, actual.Height, 0.001f, "height");
        }

        [TestMethod]
        public void Children_sit_at_left_top_right_bottom_inside_the_padded_box()
        {
            var canvas = new Canvas { Padding = new Thickness(5) };
            var topLeft = Box();
            var bottomRight = Box();
            var unset = Box();
            canvas.AddChild(topLeft, 30, 40);
            Canvas.SetRight(bottomRight, 10);
            Canvas.SetBottom(bottomRight, 20);
            canvas.Children.Add(bottomRight);
            canvas.Children.Add(unset);

            canvas.UpdateLayout(new Rect(100, 100, 300, 200));

            AssertRect(topLeft.BoundingRect, 135, 145, 20, 10);
            AssertRect(bottomRight.BoundingRect, 100 + 295 - 10 - 20, 100 + 195 - 20 - 10, 20, 10);
            AssertRect(unset.BoundingRect, 105, 105, 20, 10);
        }

        [TestMethod]
        public void Canvas_keeps_its_own_size_and_margins_stay_part_of_the_slot()
        {
            var canvas = new Canvas();
            var child = Box();
            child.Margin = new Thickness(2);
            canvas.AddChild(child, 10, 10);

            Assert.AreEqual(0f, canvas.MeasureLayout().Width);
            canvas.UpdateLayout(new Rect(0, 0, 100, 100));
            AssertRect(child.ClippingRect, 12, 12, 20, 10);
        }

        [TestMethod]
        public void Anchor_centres_the_child_on_its_point()
        {
            var canvas = new Canvas();
            var marker = Box(20, 10);
            Canvas.SetAnchor(marker, new Vector2(0.5f, 0.5f));
            canvas.AddChild(marker, 50, 50);

            canvas.UpdateLayout(new Rect(0, 0, 100, 100));

            AssertRect(marker.BoundingRect, 40, 45, 20, 10);
        }

        [TestMethod]
        public void Moving_a_child_shifts_it_and_its_content_without_a_layout_pass()
        {
            var canvas = new Canvas();
            var inner = Box(10, 5);
            var marker = new Border { Content = inner };
            canvas.AddChild(marker, 10, 10);
            var host = new CountingHost { Content = canvas };
            host.UpdateLayout(new Rect(0, 0, 200, 200));
            var innerBefore = inner.BoundingRect;

            var invalidations = 0;
            host.Invalidated += bounds => { if (bounds) invalidations++; };

            Canvas.SetPosition(marker, new Vector2(60, 25));

            Assert.AreEqual(60f, marker.BoundingRect.Left, 0.001f);
            Assert.AreEqual(25f, marker.BoundingRect.Top, 0.001f);
            Assert.AreEqual(innerBefore.Left + 50, inner.BoundingRect.Left, 0.001f);
            Assert.AreEqual(innerBefore.Top + 15, inner.BoundingRect.Top, 0.001f);
            Assert.AreEqual(0, invalidations, "a move must not ask for a layout pass");
        }

        [TestMethod]
        public void Moving_after_a_size_change_waits_for_the_layout_pass()
        {
            var canvas = new Canvas();
            var marker = Box();
            canvas.AddChild(marker, 10, 10);
            canvas.UpdateLayout(new Rect(0, 0, 200, 200));

            marker.Width = 40; // invalidates the arrangement
            Canvas.SetLeft(marker, 70);
            Assert.AreEqual(10f, marker.BoundingRect.Left, 0.001f, "no shift with a stale size");

            canvas.UpdateLayout(new Rect(0, 0, 200, 200));
            AssertRect(marker.BoundingRect, 70, 10, 40, 10);
        }

        [TestMethod]
        public void Moving_every_frame_does_not_allocate()
        {
            var canvas = new Canvas();
            var marker = Box();
            canvas.AddChild(marker, 0, 0);
            canvas.UpdateLayout(new Rect(0, 0, 200, 200));
            Canvas.SetPosition(marker, new Vector2(1, 1));

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100; i++)
                Canvas.SetPosition(marker, new Vector2(i, i * 0.5f));
            Assert.AreEqual(before, GC.GetAllocatedBytesForCurrentThread());
        }

        private sealed class CountingHost : ContentControl
        {
            public event Action<bool>? Invalidated;

            public override void InvalidateLayout(bool boundsChanged)
            {
                Invalidated?.Invoke(boundsChanged);
                base.InvalidateLayout(boundsChanged);
            }
        }
    }
}
