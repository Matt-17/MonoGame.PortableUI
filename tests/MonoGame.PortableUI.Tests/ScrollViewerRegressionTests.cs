using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Input;
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
        public void Touch_pan_starting_on_a_clickable_child_scrolls_and_does_not_click()
        {
            using var game = new Game();
            var (screen, source, viewer, clicks) = CreateTouchScreenWithButtons(game);

            Touch(screen, source, TouchLocationState.Pressed, new Vector2(10, 90));
            Touch(screen, source, TouchLocationState.Moved, new Vector2(10, 60));
            Touch(screen, source, TouchLocationState.Moved, new Vector2(10, 30));
            Touch(screen, source, TouchLocationState.Released, new Vector2(10, 30));

            Assert.IsTrue(viewer.Offset.Y > 0, $"expected the list to scroll, offset {viewer.Offset.Y}");
            Assert.AreEqual(0, clicks[0]);
        }

        [TestMethod]
        public void Touch_tap_on_a_clickable_child_inside_a_scroll_viewer_still_clicks()
        {
            using var game = new Game();
            var (screen, source, viewer, clicks) = CreateTouchScreenWithButtons(game);

            Touch(screen, source, TouchLocationState.Pressed, new Vector2(10, 10));
            Touch(screen, source, TouchLocationState.Moved, new Vector2(11, 12));
            Touch(screen, source, TouchLocationState.Released, new Vector2(11, 12));

            Assert.AreEqual(1, clicks[0]);
        }

        private static (TestScreen Screen, VirtualInputSource Source, ScrollViewer Viewer, int[] Clicks) CreateTouchScreenWithButtons(Game game)
        {
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(100, 100);
            var clicks = new int[1];
            var stack = new StackPanel();
            for (var i = 0; i < 10; i++)
            {
                var button = new Button { Height = 40, Text = $"Item {i}" };
                button.Click += (sender, args) => clicks[0]++;
                stack.AddChild(button);
            }

            var viewer = new ScrollViewer { Content = stack, EnableFling = false };
            var screen = new TestScreen();
            var source = new VirtualInputSource();
            screen.InputSource = source;
            screen.Content = viewer;
            engine.NavigateToScreen(screen);
            screen.InvalidateLayout(true);
            screen.Update();
            return (screen, source, viewer, clicks);
        }

        private static void Touch(TestScreen screen, VirtualInputSource source, TouchLocationState state, Vector2 position)
        {
            source.SetTouches(new TouchCollection(new[] { new TouchLocation(1, state, position) }));
            screen.Update();
        }

        [TestMethod]
        public void Scrolling_shifts_content_to_the_same_rects_a_full_relayout_produces()
        {
            var stack = new StackPanel();
            var items = new List<Button>();
            for (var i = 0; i < 20; i++)
            {
                var button = new Button { Height = 30, Margin = new Thickness(2), Text = $"Item {i}" };
                items.Add(button);
                stack.AddChild(button);
            }
            var viewer = new ScrollViewer { Content = stack };
            var slot = new Rect(10, 20, 200, 150);
            viewer.UpdateLayout(slot);

            viewer.ScrollBy(new PointF(0, 75));
            var shifted = items.Select(b => (b.BoundingRect, b.ClippingRect)).ToArray();
            var textRect = ((TextBlock)items[3].Content!).BoundingRect;

            viewer.UpdateLayout(slot);
            CollectionAssert.AreEqual(items.Select(b => (b.BoundingRect, b.ClippingRect)).ToArray(), shifted);
            Assert.AreEqual(((TextBlock)items[3].Content!).BoundingRect, textRect);
        }

        [TestMethod]
        public void Scrolling_after_a_content_change_relayouts_instead_of_shifting()
        {
            var first = new Border { Height = 30 };
            var second = new Border { Height = 30 };
            var stack = new StackPanel();
            stack.AddChild(first);
            stack.AddChild(second);
            for (var i = 0; i < 10; i++)
                stack.AddChild(new Border { Height = 30 });
            var viewer = new ScrollViewer { Content = stack };
            viewer.UpdateLayout(new Rect(0, 0, 100, 100));

            first.Height = 60;
            viewer.ScrollBy(new PointF(0, 10));

            Assert.AreEqual(60 - 10, second.BoundingRect.Top, 0.001f);
        }

        [TestMethod]
        public void Two_axis_viewer_scrolls_both_ways_and_shows_both_bars()
        {
            var viewer = new ScrollViewer
            {
                ScrollDirections = ScrollDirections.Both,
                EnableFling = false,
                EnableRubberBanding = false,
                Content = new FixedSizeControl(new Size(400, 300))
            };
            viewer.UpdateLayout(new Rect(0, 0, 100, 100));

            viewer.OnTouchDown(new TouchEventArgs(new PointF(80, 80)));
            viewer.OnTouchMove(new TouchEventArgs(new PointF(50, 40)));

            Assert.AreEqual(30, viewer.Offset.X, 0.001f);
            Assert.AreEqual(40, viewer.Offset.Y, 0.001f);
            Assert.IsTrue(viewer.Viewport.Width < 100 && viewer.Viewport.Height < 100, "both scrollbars take room");

            viewer.ScrollTo(new PointF(1000, 1000));
            Assert.AreEqual(viewer.Extent.Width - viewer.Viewport.Width, viewer.Offset.X, 0.001f);
            Assert.AreEqual(viewer.Extent.Height - viewer.Viewport.Height, viewer.Offset.Y, 0.001f);
        }

        [TestMethod]
        public void Horizontal_wheel_scrolls_sideways_and_the_vertical_wheel_scrolls_down()
        {
            var viewer = new ScrollViewer
            {
                ScrollDirections = ScrollDirections.Both,
                Content = new FixedSizeControl(new Size(400, 300))
            };
            viewer.UpdateLayout(new Rect(0, 0, 100, 100));

            viewer.OnScrollWheelChanged(new ScrollWheelChangedEventArgs(new PointF(10, 10), -120));
            Assert.AreEqual(0, viewer.Offset.X, 0.001f);
            Assert.AreEqual(30, viewer.Offset.Y, 0.001f);

            viewer.OnScrollWheelChanged(new ScrollWheelChangedEventArgs(new PointF(10, 10), -120) { IsHorizontal = true });
            Assert.AreEqual(30, viewer.Offset.X, 0.001f);
        }

        [TestMethod]
        public void Bring_into_view_moves_both_axes()
        {
            var target = new Border { Width = 20, Height = 20, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(300, 250, 0, 0) };
            var host = new Grid();
            host.Children.Add(new FixedSizeControl(new Size(400, 300)));
            host.Children.Add(target);
            var viewer = new ScrollViewer { ScrollDirections = ScrollDirections.Both, Content = host, ShowScrollBars = false };
            viewer.UpdateLayout(new Rect(0, 0, 100, 100));

            viewer.BringIntoView(target);

            Assert.IsTrue(viewer.Offset.X > 0 && viewer.Offset.Y > 0);
            Assert.IsTrue(target.BoundingRect.Right <= 100.5f && target.BoundingRect.Bottom <= 100.5f);
        }

        [TestMethod]
        public void Single_axis_orientation_maps_onto_scroll_directions()
        {
            var viewer = new ScrollViewer { ScrollOrientation = Orientation.Horizontal };
            Assert.AreEqual(ScrollDirections.Horizontal, viewer.ScrollDirections);

            viewer.ScrollDirections = ScrollDirections.Both;
            Assert.AreEqual(Orientation.Vertical, viewer.ScrollOrientation);
        }

        [TestMethod]
        public void Scroll_viewer_applies_touch_fling()
        {
            var viewer = CreateViewer(new Size(100, 300));
            viewer.UpdateLayout(new Rect(0, 0, 100, 100));

            ScreenSystem.TotalTime = TimeSpan.FromSeconds(10);
            viewer.OnTouchDown(new TouchEventArgs(new PointF(0, 50)));
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
            viewer.OnTouchMove(new TouchEventArgs(new PointF(0, 30)));
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
            viewer.OnTouchMove(new TouchEventArgs(new PointF(0, 10)));
            viewer.OnTouchUp(new TouchEventArgs(new PointF(0, 10)));
            var atRelease = viewer.Offset.Y;

            // Momentum: the content keeps moving over the next frames, slows down, ends in range.
            var previous = atRelease;
            var advancedAfterRelease = false;
            for (var frame = 0; frame < 180; frame++)
            {
                ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
                viewer.OnFrameUpdate();
                if (viewer.Offset.Y > previous + 0.01f)
                    advancedAfterRelease = true;
                previous = viewer.Offset.Y;
            }

            Assert.IsTrue(advancedAfterRelease, "the fling coasts after the finger lifts");
            Assert.AreEqual(200, viewer.Offset.Y, 0.5f, "a fast flick runs to the end and settles there");
        }

        [TestMethod]
        public void Rubber_band_swings_back_over_several_frames()
        {
            var viewer = CreateViewer(new Size(100, 300));
            viewer.UpdateLayout(new Rect(0, 0, 100, 100));
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(20);

            viewer.OnTouchDown(new TouchEventArgs(new PointF(0, 0)));
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
            viewer.OnTouchMove(new TouchEventArgs(new PointF(0, 30))); // pull past the top
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(300); // finger rests: no fling
            viewer.OnTouchUp(new TouchEventArgs(new PointF(0, 30)));
            Assert.IsTrue(viewer.Offset.Y < 0, "still over-scrolled right after release");

            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
            viewer.OnFrameUpdate();
            Assert.IsTrue(viewer.Offset.Y < 0 && viewer.Offset.Y > -30, $"one frame later it is on its way back ({viewer.Offset.Y})");

            for (var frame = 0; frame < 60; frame++)
            {
                ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
                viewer.OnFrameUpdate();
            }
            Assert.AreEqual(0, viewer.Offset.Y, "settles exactly at the edge");
        }

        [TestMethod]
        public void Touch_cancel_resets_overscroll_and_pan_state()
        {
            var viewer = CreateViewer(new Size(100, 300));
            viewer.EnableFling = false;
            viewer.UpdateLayout(new Rect(0, 0, 100, 100));

            viewer.OnTouchDown(new TouchEventArgs(new PointF(0, 0)));
            viewer.OnTouchMove(new TouchEventArgs(new PointF(0, 30)));
            viewer.OnTouchCancel(new TouchEventArgs(new PointF(0, 120)));

            Assert.AreEqual(0, viewer.Offset.Y);

            // A later move without a new touch-down must not apply a stale delta.
            viewer.OnTouchMove(new TouchEventArgs(new PointF(0, 10)));
            Assert.AreEqual(0, viewer.Offset.Y);
        }

        [TestMethod]
        public void Wheel_scrolls_only_the_innermost_viewer_that_can_move()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(100, 100);
            var inner = CreateViewer(new Size(100, 300));
            inner.Height = 60;
            var page = new StackPanel();
            page.AddChild(inner);
            page.AddChild(new FixedSizeControl(new Size(100, 300)));
            var outer = new ScrollViewer { Content = page };
            var screen = new TestScreen();
            var source = new VirtualInputSource();
            screen.InputSource = source;
            screen.Content = outer;
            engine.NavigateToScreen(screen);
            screen.InvalidateLayout(true);

            source.SetPointer(new PointF(10, 10));
            screen.Update();
            source.SetScrollWheelValue(-120);
            screen.Update();

            Assert.IsTrue(inner.Offset.Y > 0, "inner viewer should scroll");
            Assert.AreEqual(0, outer.Offset.Y, "outer viewer must not scroll on the same tick");
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
            screen.PerformLayoutIfDirty();
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
