using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>Implicit transitions of layout values: Width/Height, Margin and TextBlock.TextSize.</summary>
    [TestClass]
    public class MetricsTransitionTests
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
        public void Width_grows_and_layout_follows_the_shown_width()
        {
            var growing = new TestControl { Width = 100, Height = 20 };
            var next = new TestControl { Width = 10, Height = 20 };
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.AddChild(growing);
            row.AddChild(next);
            growing.Transitions.Add(TransitionProperty.Size, Ms(100), Easings.Linear);

            growing.Width = 200;
            Assert.AreEqual(100, growing.Width, 0.001f, "the getter returns what layout uses: nothing moved yet");
            Assert.IsTrue(growing.IsTransitioning);

            At(growing, 50);
            row.UpdateLayout(new Rect(0, 0, 500, 20));
            Assert.AreEqual(150, growing.Width, 0.001f);
            Assert.AreEqual(150, growing.BoundingRect.Width, 0.001f);
            Assert.AreEqual(150, next.BoundingRect.Left, 0.001f, "the sibling moves along");

            At(growing, 100);
            Assert.AreEqual(200, growing.Width, 0.001f);
            Assert.IsFalse(growing.IsTransitioning);
        }

        [TestMethod]
        public void Setting_the_target_again_mid_flight_does_not_restart_it()
        {
            var control = new TestControl { Width = 100 };
            control.Transitions.Add(TransitionProperty.Size, Ms(100), Easings.Linear);
            control.Width = 200;
            At(control, 50);

            control.Width = 200; // e.g. set every frame from game code
            At(control, 100);

            Assert.AreEqual(200, control.Width, 0.001f);
            Assert.IsFalse(control.IsTransitioning);
        }

        [TestMethod]
        public void Height_animates_on_its_own_axis()
        {
            var control = new TestControl { Height = 40 };
            control.Transitions.Add(TransitionProperty.Size, Ms(100), Easings.Linear);

            control.Height = 80;
            At(control, 25);

            Assert.AreEqual(50, control.Height, 0.001f);
        }

        [TestMethod]
        public void A_change_to_or_from_auto_snaps()
        {
            var control = new TestControl();
            control.Transitions.Add(TransitionProperty.Size, Ms(100), Easings.Linear);

            control.Width = 120;
            Assert.AreEqual(120, control.Width, "from Auto");
            Assert.IsFalse(control.IsTransitioning);

            control.Width = float.NaN;
            Assert.IsTrue(float.IsNaN(control.Width), "to Auto");
            Assert.IsFalse(control.IsTransitioning);
        }

        [TestMethod]
        public void Margin_animates_every_side()
        {
            var control = new TestControl { Margin = new Thickness(0) };
            control.Transitions.Add(TransitionProperty.Margin, Ms(100), Easings.Linear);

            control.Margin = new Thickness(10, 20, 30, 40);
            At(control, 50);

            Assert.AreEqual(5, control.Margin.Left, 0.001f);
            Assert.AreEqual(10, control.Margin.Top, 0.001f);
            Assert.AreEqual(15, control.Margin.Right, 0.001f);
            Assert.AreEqual(20, control.Margin.Bottom, 0.001f);

            control.Margin = new Thickness(10, 20, 30, 40);
            At(control, 100);
            Assert.AreEqual(new Thickness(10, 20, 30, 40), control.Margin);
            Assert.IsFalse(control.IsTransitioning);
        }

        [TestMethod]
        public void Removing_the_transition_or_completing_ends_at_the_target()
        {
            var control = new TestControl { Width = 100, Margin = new Thickness(0) };
            control.Transitions.Add(TransitionProperty.Size | TransitionProperty.Margin, Ms(100), Easings.Linear);
            control.Width = 200;
            control.Margin = new Thickness(8);
            At(control, 50);

            control.Transitions.Remove(TransitionProperty.Size);
            Assert.AreEqual(200, control.Width, 0.001f);
            Assert.AreEqual(4, control.Margin.Left, 0.001f, "the margin still moves");

            control.CompleteTransitions();
            Assert.AreEqual(new Thickness(8), control.Margin);
            Assert.IsFalse(control.IsTransitioning);
        }

        [TestMethod]
        public void Text_size_animates_in_half_pixel_steps_and_ends_exactly()
        {
            var text = new TextBlock { Text = "Hello", TextSize = 14 };
            text.Transitions.Add(TransitionProperty.TextSize, Ms(100), Easings.Linear);

            text.TextSize = 24.3f;
            Assert.AreEqual(14, text.TextSize, 0.001f);
            Assert.IsTrue(text.IsTransitioning);

            At(text, 33);
            Assert.AreEqual(17.5f, text.TextSize, 0.001f, "17.399 rounded to a half pixel");

            At(text, 100);
            Assert.AreEqual(24.3f, text.TextSize, 0.001f);
            Assert.IsFalse(text.IsTransitioning);
        }

        [TestMethod]
        public void Text_size_without_a_transition_is_immediate()
        {
            var text = new TextBlock { Text = "Hello", TextSize = 14 };
            text.Transitions.Add(TransitionProperty.Size, Ms(100));

            text.TextSize = 20;

            Assert.AreEqual(20, text.TextSize);
            Assert.IsFalse(text.IsTransitioning);
        }
    }
}
