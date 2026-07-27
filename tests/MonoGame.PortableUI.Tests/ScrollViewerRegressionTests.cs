using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Media;

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
        public void Scroll_viewer_default_scrollbar_thickness_is_easy_to_target()
        {
            var viewer = new ScrollViewer();

            Assert.AreEqual(8, viewer.ScrollBarThickness);
        }

        [TestMethod]
        public void Scroll_viewer_default_scrollbar_gutter_is_very_light_gray()
        {
            var viewer = new ScrollViewer();
            var brush = viewer.ScrollBarGutterBrush as SolidColorBrush;

            Assert.IsNotNull(brush);
            Assert.AreEqual(new Color(245, 245, 245), brush.Color);
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

        [TestMethod]
        public void Scroll_viewer_refreshes_hover_state_after_wheel_scroll()
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical };
            var first = new InspectableButton { Text = "One", Height = 40 };
            var second = new InspectableButton { Text = "Two", Height = 40 };
            stack.AddChild(first);
            stack.AddChild(second);
            var viewer = new ScrollViewer
            {
                ScrollOrientation = Orientation.Vertical,
                Content = stack
            };
            viewer.UpdateLayout(new Rect(0, 0, 100, 40));
            first.OnMouseEnter(new MouseEventArgs(new PointF(10, 20), new System.Collections.Generic.List<MouseButton>()));

            viewer.OnScrollWheelChanged(new ScrollWheelChangedEventArgs(new PointF(10, 20), -120));

            Assert.AreEqual(HoverStates.NotHovering, first.CurrentHoverState);
            Assert.AreEqual(HoverStates.Hovering, second.CurrentHoverState);
        }

        [TestMethod]
        public void Scroll_viewer_dragging_vertical_scrollbar_thumb_updates_offset()
        {
            var viewer = CreateViewer(new Size(100, 300));
            viewer.UpdateLayout(new Rect(0, 0, 100, 100));

            viewer.OnMouseDown(new MouseEventArgs(new PointF(98, 16), MouseButton.Left));
            viewer.OnMouseMove(new MouseEventArgs(new PointF(98, 66), new List<MouseButton> { MouseButton.Left }));
            viewer.OnMouseUp(new MouseEventArgs(new PointF(98, 66), MouseButton.Left));

            Assert.AreEqual(150, viewer.Offset.Y, 0.001f);
        }

        [TestMethod]
        public void List_box_routes_scrollbar_drag_to_nested_scroll_viewer()
        {
            var listBox = new ListBox
            {
                Width = 100,
                Height = 100
            };
            for (var i = 1; i <= 10; i++)
                listBox.Items.Add($"Item {i}");
            listBox.UpdateLayout(new Rect(0, 0, 100, 100));
            var scrollViewer = listBox.GetDescendants().OfType<ScrollViewer>().Single();

            RouteMouseDown(listBox, new PointF(98, 16));
            RouteMouseMove(listBox, new PointF(98, 66));
            RouteMouseUp(listBox, new PointF(98, 66));

            Assert.IsTrue(scrollViewer.Offset.Y > 0);
            Assert.AreEqual(-1, listBox.SelectedIndex);
        }

        [TestMethod]
        public void List_box_scrollbar_reserves_layout_gutter_for_items()
        {
            var listBox = CreateScrollableListBox();
            var scrollViewer = listBox.GetDescendants().OfType<ScrollViewer>().Single();
            var firstItem = listBox.ItemButtons[0];

            Assert.AreEqual(92, scrollViewer.Viewport.Width);
            Assert.AreEqual(92, firstItem.BoundingRect.Width);
            Assert.IsFalse(firstItem.BoundingRect.Contains(new PointF(98, 16)));
        }

        [TestMethod]
        public void Moving_from_list_box_item_to_scrollbar_gutter_clears_item_hover()
        {
            var listBox = CreateScrollableListBox();
            var firstItem = listBox.ItemButtons[0];
            var previousPosition = new PointF(10, 16);
            var scrollBarPosition = new PointF(98, 16);
            firstItem.OnMouseEnter(new MouseEventArgs(previousPosition, new List<MouseButton>()));

            RouteMouseLeave(listBox, previousPosition, scrollBarPosition);

            Assert.IsFalse(firstItem.IsMouseHovering);
        }

        [TestMethod]
        public void List_box_click_scrolls_partially_visible_item_into_view()
        {
            var listBox = CreateScrollableListBox();
            var scrollViewer = listBox.GetDescendants().OfType<ScrollViewer>().Single();

            listBox.ItemButtons[3].OnClick();

            Assert.AreEqual(12, scrollViewer.Offset.Y, 0.001f);
            Assert.AreEqual(100, listBox.ItemButtons[3].BoundingRect.Bottom, 0.001f);

            listBox.ItemButtons[0].OnClick();

            Assert.AreEqual(0, scrollViewer.Offset.Y, 0.001f);
            Assert.AreEqual(0, listBox.ItemButtons[0].BoundingRect.Top, 0.001f);
        }

        [TestMethod]
        public void Scroll_viewer_capture_keeps_dragging_when_pointer_leaves_control_bounds()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(100, 100);
            var screen = new TestScreen();
            engine.NavigateToScreen(screen);
            var listBox = new ListBox
            {
                Width = 100,
                Height = 100
            };
            for (var i = 1; i <= 10; i++)
                listBox.Items.Add($"Item {i}");
            screen.Content = listBox;
            screen.InvalidateLayout(true);
            var scrollViewer = listBox.GetDescendants().OfType<ScrollViewer>().Single();

            RouteMouseDown(listBox, new PointF(98, 16));
            Assert.AreSame(scrollViewer, screen.CapturedMouseControl);

            var routedMove = screen.RouteCapturedMouseMove(new PointF(-500, 66), new List<MouseButton> { MouseButton.Left });
            var routedUp = screen.RouteCapturedMouseUp(new PointF(-500, 66), MouseButton.Left);

            Assert.IsTrue(routedMove);
            Assert.IsTrue(routedUp);
            Assert.IsNull(screen.CapturedMouseControl);
            Assert.IsTrue(scrollViewer.Offset.Y > 0);
            Assert.AreEqual(-1, listBox.SelectedIndex);
        }

        [TestMethod]
        public void Scroll_viewer_uses_hover_and_pressed_scrollbar_brushes()
        {
            var normalBrush = new SolidColorBrush(new Color(1, 2, 3));
            var hoverBrush = new SolidColorBrush(new Color(4, 5, 6));
            var pressedBrush = new SolidColorBrush(new Color(7, 8, 9));
            var viewer = CreateViewer(new Size(100, 300));
            viewer.ScrollBarBrush = normalBrush;
            viewer.ScrollBarHoverBrush = hoverBrush;
            viewer.ScrollBarPressedBrush = pressedBrush;
            viewer.UpdateLayout(new Rect(0, 0, 100, 100));

            Assert.AreSame(normalBrush, viewer.CurrentScrollBarBrush);

            viewer.OnMouseMove(new MouseEventArgs(new PointF(98, 16), new List<MouseButton>()));

            Assert.AreSame(hoverBrush, viewer.CurrentScrollBarBrush);

            viewer.OnMouseDown(new MouseEventArgs(new PointF(98, 16), MouseButton.Left));

            Assert.AreSame(pressedBrush, viewer.CurrentScrollBarBrush);

            viewer.OnMouseUp(new MouseEventArgs(new PointF(98, 16), MouseButton.Left));

            Assert.AreSame(hoverBrush, viewer.CurrentScrollBarBrush);

            viewer.OnMouseLeave(new MouseEventArgs(new PointF(20, 16), new List<MouseButton>()));

            Assert.AreSame(normalBrush, viewer.CurrentScrollBarBrush);
        }

        private static ScrollViewer CreateViewer(Size contentSize)
        {
            return new ScrollViewer
            {
                ScrollOrientation = Orientation.Vertical,
                Content = new FixedSizeControl(contentSize)
            };
        }

        private static ListBox CreateScrollableListBox()
        {
            var listBox = new ListBox
            {
                Width = 100,
                Height = 100
            };
            for (var i = 1; i <= 10; i++)
                listBox.Items.Add($"Item {i}");
            listBox.UpdateLayout(new Rect(0, 0, 100, 100));
            return listBox;
        }

        private static void RouteMouseDown(Control root, PointF position)
        {
            var args = new MouseEventArgs(position, MouseButton.Left);
            VisualTreeHelper.IterateVisualTree(root, args, ContainsMousePosition, (control, eventArgs) => control.OnMouseDown(eventArgs), null);
            Assert.IsTrue(args.Handled);
        }

        private static void RouteMouseMove(Control root, PointF position)
        {
            var args = new MouseEventArgs(position, new List<MouseButton> { MouseButton.Left });
            VisualTreeHelper.IterateVisualTree(root, args, ContainsMousePosition, (control, eventArgs) => control.OnMouseMove(eventArgs), null);
            Assert.IsTrue(args.Handled);
        }

        private static void RouteMouseUp(Control root, PointF position)
        {
            var args = new MouseEventArgs(position, MouseButton.Left);
            VisualTreeHelper.IterateVisualTree(root, args, ContainsMousePosition, (control, eventArgs) => control.OnMouseUp(eventArgs), null);
            Assert.IsTrue(args.Handled);
        }

        private static void RouteMouseLeave(Control root, PointF previousPosition, PointF position)
        {
            var args = new MouseEventArgs(position, new List<MouseButton>());
            VisualTreeHelper.IterateVisualTree(root, args,
                (control, eventArgs) => !control.BoundingRect.Contains(eventArgs.Position) && control.BoundingRect.Contains(previousPosition),
                (control, eventArgs) => control.OnMouseLeave(eventArgs),
                (control, eventArgs) => control.BoundingRect.Contains(previousPosition));
        }

        private static bool ContainsMousePosition(Control control, MouseEventArgs args)
        {
            return control.BoundingRect.Contains(args.Position);
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

        private sealed class InspectableButton : Button
        {
            public HoverStates CurrentHoverState => HoverState;
        }

        private sealed class TestScreen : Screen
        {
        }
    }
}
