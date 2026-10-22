using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class TextWrapModeTests
    {
        private static float WidthOf(string text) => new TextBlock { Text = text }.MeasuredText.X;

        [TestMethod]
        public void Bottom_heavy_puts_the_short_part_on_top()
        {
            var block = new TextBlock { Text = "Deep Purple Magic Sour", TextWrapping = TextWrapping.WrapBottomHeavy };

            var lines = block.WrappedLinesAt(WidthOf("Purple Magic Sour") + 1);

            CollectionAssert.AreEqual(new[] { "Deep", "Purple Magic Sour" }, (System.Collections.ICollection)lines);
        }

        [TestMethod]
        public void Plain_wrap_still_fills_the_top_line()
        {
            var block = new TextBlock { Text = "Deep Purple Magic Sour", TextWrapping = TextWrapping.Wrap };

            var lines = block.WrappedLinesAt(WidthOf("Purple Magic Sour") + 1);

            CollectionAssert.AreEqual(new[] { "Deep Purple Magic", "Sour" }, (System.Collections.ICollection)lines);
        }

        [TestMethod]
        public void Text_that_fits_stays_on_one_line_in_every_mode()
        {
            foreach (var mode in new[] { TextWrapping.WrapBottomHeavy, TextWrapping.WrapBalanced })
            {
                var block = new TextBlock { Text = "Deep Purple Magic Sour", TextWrapping = mode };
                Assert.AreEqual(1, block.WrappedLinesAt(WidthOf("Deep Purple Magic Sour") + 1).Count, mode.ToString());
            }
        }

        [TestMethod]
        public void Bottom_heavy_keeps_the_fewest_lines_and_fills_upwards()
        {
            var block = new TextBlock { Text = "aa bb cc dd ee", TextWrapping = TextWrapping.WrapBottomHeavy };

            var lines = block.WrappedLinesAt(WidthOf("aa bb") + 1);

            CollectionAssert.AreEqual(new[] { "aa", "bb cc", "dd ee" }, (System.Collections.ICollection)lines);
        }

        [TestMethod]
        public void Balanced_evens_out_the_lines()
        {
            var block = new TextBlock { Text = "Deep Purple Magic Sour", TextWrapping = TextWrapping.WrapBalanced };

            var lines = block.WrappedLinesAt(WidthOf("Deep Purple Magic") + 1);

            CollectionAssert.AreEqual(new[] { "Deep Purple", "Magic Sour" }, (System.Collections.ICollection)lines);
        }

        [TestMethod]
        public void A_word_wider_than_the_line_still_breaks_by_characters()
        {
            var block = new TextBlock { Text = "a Supercalifragilistic", TextWrapping = TextWrapping.WrapBottomHeavy };

            var lines = block.WrappedLinesAt(WidthOf("Supercali") + 1);

            Assert.IsTrue(lines.Count >= 3);
            Assert.AreEqual("a", lines[0]);
        }

        [TestMethod]
        public void Every_wrap_mode_measures_its_lines()
        {
            var block = new TextBlock { Text = "Deep Purple Magic Sour", TextWrapping = TextWrapping.WrapBottomHeavy, Width = WidthOf("Purple Magic Sour") + 1 };
            var oneLine = new TextBlock { Text = "Deep" }.MeasureLayout().Height;

            Assert.IsTrue(block.MeasureLayout().Height > oneLine * 1.5f);
        }
    }
}
