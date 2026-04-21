using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ScrollViewerRegressionTests
    {
        [TestMethod]
        public void Scroll_viewer_keeps_offset_zero_when_content_fits()
        {
            var viewer = CreateViewer(new Size(80, 80));

            viewer.UpdateLayout(new Rect(0, 0, 100, 100));
            viewer.ScrollTo(new PointF(0, 500));

            Assert.AreEqual(100, viewer.Viewport.Height);
            Assert.AreEqual(100, viewer.Extent.Height);
            Assert.AreEqual(0, viewer.Offset.Y);
        }

        [TestMethod]
        public void Scroll_viewer_clamps_to_extent()
        {
            var viewer = CreateViewer(new Size(100, 300));

            viewer.UpdateLayout(new Rect(0, 0, 100, 100));
            viewer.ScrollTo(new PointF(0, 500));

            Assert.AreEqual(100, viewer.Viewport.Height);
            Assert.AreEqual(300, viewer.Extent.Height);
            Assert.AreEqual(200, viewer.Offset.Y);
        }

        [TestMethod]
        public void Scroll_viewer_applies_touch_fling()
        {
            var viewer = CreateViewer(new Size(100, 300));
            viewer.UpdateLayout(new Rect(0, 0, 100, 100));

            viewer.OnTouchDown(new TouchEventArgs(new PointF(0, 50)));
            viewer.OnTouchMove(new TouchEventArgs(new PointF(0, 0)));
            viewer.OnTouchUp(new TouchEventArgs(new PointF(0, 0)));

            Assert.AreEqual(200, viewer.Offset.Y);
        }

        [TestMethod]
        public void Scroll_viewer_allows_limited_rubber_band()
        {
            var viewer = CreateViewer(new Size(100, 300));
            viewer.EnableFling = false;
            viewer.UpdateLayout(new Rect(0, 0, 100, 100));

            viewer.OnTouchDown(new TouchEventArgs(new PointF(0, 0)));
            viewer.OnTouchMove(new TouchEventArgs(new PointF(0, 300)));

            Assert.AreEqual(-viewer.RubberBandLimit, viewer.Offset.Y);
        }

        private static ScrollViewer CreateViewer(Size contentSize)
        {
            return new ScrollViewer
            {
                ScrollOrientation = Orientation.Vertical,
                Content = new FixedSizeControl(contentSize)
            };
        }

        private sealed class FixedSizeControl : Control
        {
            private readonly Size _size;

            public FixedSizeControl(Size size)
            {
                _size = size;
            }

            public override Size MeasureLayout()
            {
                return _size;
            }
        }
    }
}
