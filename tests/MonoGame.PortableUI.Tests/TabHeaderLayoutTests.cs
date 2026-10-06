using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>#58: tab headers never run into each other - gaps shrink first, then only the longest
    /// labels shorten with an ellipsis.</summary>
    [TestClass]
    public class TabHeaderLayoutTests
    {
        private static TabControl Tabs(params string[] headers)
        {
            var tabs = new TabControl();
            foreach (var header in headers)
                tabs.Items.Add(new TabItem { Header = header, Content = new TextBlock { Text = header } });
            return tabs;
        }

        private static Button Header(TabControl tabs, int index) => (Button)tabs.GetVisualChild(index);

        private static float LabelWidth(TabControl tabs, int index) => ((TextBlock)Header(tabs, index).Content!).Measure().Width;

        [TestMethod]
        public void Headers_fill_the_strip_without_overlapping_and_keep_their_labels_whole_when_they_fit()
        {
            var tabs = Tabs("One", "Two", "Three");
            tabs.UpdateLayout(new Rect(0, 0, 600, 200));

            var right = 0f;
            for (var i = 0; i < 3; i++)
            {
                var rect = Header(tabs, i).BoundingRect;
                Assert.AreEqual(right, rect.Left, 0.01f, "headers are adjacent");
                Assert.IsTrue(rect.Width - Header(tabs, i).Padding.Horizontal >= LabelWidth(tabs, i) - 0.01f, "label fits");
                right = rect.Right;
            }
            Assert.AreEqual(600, right, 0.01f, "the strip is filled");
        }

        [TestMethod]
        public void A_tight_strip_shrinks_the_gaps_before_any_label()
        {
            var tabs = Tabs("Gallery", "Inspector", "Controls", "Visual FX");
            tabs.UpdateLayout(new Rect(0, 0, 10000, 200));
            var text = 0f;
            for (var i = 0; i < 4; i++)
                text += LabelWidth(tabs, i);

            // Room for the labels and a small gap each, less than the comfortable 16.
            var width = text + 4 * 10;
            tabs.UpdateLayout(new Rect(0, 0, width, 200));

            for (var i = 0; i < 4; i++)
            {
                var header = Header(tabs, i);
                Assert.IsTrue(header.Padding.Left >= 3 && header.Padding.Left < 8, $"gap shrank ({header.Padding.Left})");
                Assert.IsTrue(header.BoundingRect.Width - header.Padding.Horizontal >= LabelWidth(tabs, i) - 0.01f, "every label still whole");
            }
        }

        [TestMethod]
        public void Without_room_only_the_longest_labels_shorten()
        {
            var tabs = Tabs("Go", "Up", "A rather long header", "Another long one");
            tabs.UpdateLayout(new Rect(0, 0, 10000, 200));
            var shortWidth = LabelWidth(tabs, 0);
            var longWidth = LabelWidth(tabs, 2);
            var width = 2 * shortWidth + 2 * longWidth * 0.5f + 4 * 6;

            tabs.UpdateLayout(new Rect(0, 0, width, 200));

            Assert.IsTrue(Header(tabs, 0).BoundingRect.Width - Header(tabs, 0).Padding.Horizontal >= shortWidth - 0.01f, "short labels stay whole");
            Assert.IsTrue(Header(tabs, 2).BoundingRect.Width - Header(tabs, 2).Padding.Horizontal < longWidth, "long labels shorten");
            Assert.AreEqual(TextTrimming.Ellipsis, ((TextBlock)Header(tabs, 2).Content!).TextTrimming);
            Assert.AreEqual(width, Header(tabs, 3).BoundingRect.Right, 0.01f, "still exactly the strip");
        }
    }
}
