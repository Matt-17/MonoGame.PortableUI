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
