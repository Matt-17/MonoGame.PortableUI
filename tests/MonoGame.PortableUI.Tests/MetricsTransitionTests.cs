using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>Implicit transitions of layout values: Width/Height, Margin and TextBlock.TextSize.</summary>
    [TestClass]
    public class MetricsTransitionTests
    {
        private sealed class TestControl : Control
        {
        }

        /// <summary>Records the size glyphs are rasterized at and the scale they are drawn with.</summary>
        private sealed class SizeRecordingFont : UIFont
        {
            public float PixelSize;
            public Vector2 Scale;
            public override float DefaultSize => 16;
            public override float GetLineHeight(float pixelSize) => pixelSize;
            public override Vector2 MeasureString(string text, float pixelSize) => new Vector2(text.Length * pixelSize / 2, pixelSize);
            public override Vector2 MeasureString(StringBuilder text, float pixelSize) => new Vector2(text.Length * pixelSize / 2, pixelSize);
            public override void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float pixelSize, Vector2 scale)
                => (PixelSize, Scale) = (pixelSize, scale);
            public override void DrawString(SpriteBatch spriteBatch, StringBuilder text, Vector2 position, Color color, float pixelSize, Vector2 scale)
                => (PixelSize, Scale) = (pixelSize, scale);
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
        public void Text_size_animates_smoothly_and_ends_exactly()
        {
            var text = new TextBlock { Text = "Hello", TextSize = 14 };
            text.Transitions.Add(TransitionProperty.TextSize, Ms(100), Easings.Linear);

            text.TextSize = 24.3f;
            Assert.AreEqual(14, text.TextSize, 0.001f);
            Assert.IsTrue(text.IsTransitioning);

            At(text, 33);
            Assert.AreEqual(17.399f, text.TextSize, 0.001f, "no steps: layout follows the exact size");

            At(text, 100);
            Assert.AreEqual(24.3f, text.TextSize, 0.001f);
            Assert.IsFalse(text.IsTransitioning);
        }

        [TestMethod]
        public void Moving_text_scales_glyphs_of_the_larger_end_to_the_size_shown()
        {
            var font = new SizeRecordingFont();
            var text = new TextBlock { Text = "Hello", TextSize = 14, DynamicFont = font, TextColor = Color.White };
            text.Transitions.Add(TransitionProperty.TextSize, Ms(100), Easings.Linear);
            text.TextSize = 24.3f;

            At(text, 33);
            text.OnDraw(null!, new Rect(0, 0, 200, 40));
            Assert.AreEqual(24.3f, font.PixelSize, 0.001f, "set at the larger end for the whole transition");
            Assert.AreEqual(17.399f, font.PixelSize * font.Scale.X, 0.001f, "drawn at the size shown: no steps");
            Assert.AreEqual(5 * 17.399f / 2, text.MeasuredText.X, 0.01f, "measured like it is drawn");

            At(text, 100);
            text.OnDraw(null!, new Rect(0, 0, 200, 40));
            Assert.AreEqual(24.3f, font.PixelSize, 0.001f, "at rest the exact size is rasterized");
            Assert.AreEqual(1, font.Scale.X, 0.001f);

            text.TextSize = 14;
            At(text, 150);
            text.OnDraw(null!, new Rect(0, 0, 200, 40));
            Assert.AreEqual(24.3f, font.PixelSize, 0.001f, "shrinking scales the larger start down, never a small size up");
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
