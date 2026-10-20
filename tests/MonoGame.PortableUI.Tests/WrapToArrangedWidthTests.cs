using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class WrapToArrangedWidthTests
    {
        private sealed class TestScreen : Screen
        {
        }

        [TestMethod]
        public void A_stretched_wrapping_label_measures_its_wrapped_height_in_the_same_layout_pass()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(200, 600);
            var label = new TextBlock
            {
                Text = "Steady pace along the river, twenty-eight minutes. Two short breaks at the bridges.",
                TextWrapping = TextWrapping.Wrap
            };
            var below = new Border { Height = 10 };
            var stack = new StackPanel { Children = { label, below } };
            var screen = new TestScreen { Content = stack };
            engine.NavigateToScreen(screen);

            screen.PerformLayoutIfDirty();

            var lineHeight = label.CurrentLineHeight;
            Assert.IsTrue(label.ClippingRect.Height > lineHeight * 1.5f,
                $"wrapped lines are measured (height {label.ClippingRect.Height}, line {lineHeight})");
            Assert.AreEqual(label.ClippingRect.Bottom, below.BoundingRect.Top, 0.01f, "the next element moves below the wrapped text");
        }

        [TestMethod]
        public void A_left_aligned_wrapping_label_keeps_wrapping_at_its_slot_width()
        {
            // With layout rounding the aligned rect of a left-aligned label can end a fraction of a pixel short
            // of its longest line; wrapping to that rect would push the line's last word down.
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, UseLayoutRounding = true });
            engine.SetNativeRenderScale(2.75f);
            engine.SetScreenSize(200, 600);
            var label = new TextBlock
            {
                Text = "Steady pace along the river, twenty-eight minutes. Two short breaks at the bridges.",
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var screen = new TestScreen { Content = new StackPanel { Children = { label } } };
            engine.NavigateToScreen(screen);
            screen.PerformLayoutIfDirty();
            var height = label.ClippingRect.Height;

            // Further passes must not narrow the wrap width to the longest line and push words down.
            for (var i = 0; i < 3; i++)
            {
                label.InvalidateLayout(true);
                screen.PerformLayoutIfDirty();
            }

            Assert.AreEqual(height, label.ClippingRect.Height, 0.01f);
        }

        [TestMethod]
        public void Drawing_wraps_at_the_laid_out_width_not_at_a_rounded_rect()
        {
            // A left-aligned label is arranged as wide as its line; rounded to pixels that rect can be a
            // fraction narrower than the line, and wrapping to it moved the last word into a clipped line.
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, UseLayoutRounding = true });
            engine.SetScreenSize(400, 600);
            var label = new TextBlock { Text = "to Ben", TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Left };
            var screen = new TestScreen { Content = new StackPanel { Children = { label } } };
            engine.NavigateToScreen(screen);
            screen.PerformLayoutIfDirty();

            var lineWidth = label.MeasuredText.X;
            var narrower = new Rect(0, 0, lineWidth - 0.3f, label.ClippingRect.Height);

            Assert.IsTrue(label.DrawWrapWidth(narrower) >= lineWidth, "draws on the line layout measured");
        }

        [TestMethod]
        public void A_label_that_fits_on_one_line_needs_no_second_pass()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 600);
            var label = new TextBlock { Text = "Short", TextWrapping = TextWrapping.Wrap };
            var screen = new TestScreen { Content = new StackPanel { Children = { label } } };
            engine.NavigateToScreen(screen);
            engine.Update(new GameTime(System.TimeSpan.FromMilliseconds(16), System.TimeSpan.FromMilliseconds(16)));

            Assert.AreEqual(1, engine.LayoutPassesThisFrame);
        }
    }
}
