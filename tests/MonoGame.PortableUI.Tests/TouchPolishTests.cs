using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class TouchPolishTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private static (ScreenEngine Engine, TestScreen Screen, VirtualInputSource Source, Button Button) CreateButtonScreen(Game game, bool hoverOnTouch)
        {
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, HoverOnTouch = hoverOnTouch });
            engine.SetScreenSize(200, 200);
            var button = new Button { Text = "Tap", Width = 100, Height = 50, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var source = new VirtualInputSource();
            var screen = new TestScreen { Content = button, InputSource = source };
            engine.NavigateToScreen(screen);
            source.SetPointer(new PointF(180, 180)); // start outside the button
            engine.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(16)));
            return (engine, screen, source, button);
        }

        // A touch platform moves the emulated mouse to the finger as well.
        private static void TapWithEmulatedMouse(ScreenEngine engine, VirtualInputSource source, PointF at)
        {
            var time = TimeSpan.FromSeconds(2);
            source.SetTouches(new TouchCollection(new[] { new TouchLocation(1, TouchLocationState.Pressed, new Vector2(at.X, at.Y)) }));
            source.SetPointer(at);
            engine.Update(new GameTime(time, TimeSpan.FromMilliseconds(16)));
            source.SetTouches(new TouchCollection(new[] { new TouchLocation(1, TouchLocationState.Released, new Vector2(at.X, at.Y)) }));
            engine.Update(new GameTime(time + TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16)));
            source.SetTouches(new TouchCollection(Array.Empty<TouchLocation>()));
            engine.Update(new GameTime(time + TimeSpan.FromMilliseconds(32), TimeSpan.FromMilliseconds(16)));
        }

        [TestMethod]
        public void A_tap_does_not_leave_the_button_hovered_by_default()
        {
            using var game = new Game();
            var (engine, _, source, button) = CreateButtonScreen(game, hoverOnTouch: false);

            TapWithEmulatedMouse(engine, source, new PointF(20, 20));

            Assert.IsFalse(button.IsMouseHovering);
        }

        [TestMethod]
        public void Hover_on_touch_can_be_enabled()
        {
            using var game = new Game();
            var (engine, _, source, button) = CreateButtonScreen(game, hoverOnTouch: true);

            TapWithEmulatedMouse(engine, source, new PointF(20, 20));

            Assert.IsTrue(button.IsMouseHovering);
        }

        [TestMethod]
        public void A_real_mouse_still_hovers()
        {
            using var game = new Game();
            var (engine, _, source, button) = CreateButtonScreen(game, hoverOnTouch: false);

            source.SetPointer(new PointF(30, 30));
            engine.Update(new GameTime(TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(16)));

            Assert.IsTrue(button.IsMouseHovering);
        }

        [TestMethod]
        public void Data_grid_scrolls_sideways_only_when_its_columns_do_not_fit()
        {
            var grid = new DataGrid { Width = 300, Height = 200 };
            grid.Columns.Add(new DataGridColumn { Header = "A", Width = new GridLength(1, GridLengthUnit.Relative), CellText = i => i.ToString()! });
            grid.Columns.Add(new DataGridColumn { Header = "B", Width = new GridLength(80, GridLengthUnit.Absolute), CellText = i => i.ToString()! });
            grid.Items.Add(1);
            grid.UpdateLayout(new Rect(0, 0, 300, 200));
            Assert.AreEqual(ScrollDirections.None, grid.HorizontalScroller.ScrollDirections, "columns fit");

            grid.Columns.Add(new DataGridColumn { Header = "C", Width = new GridLength(400, GridLengthUnit.Absolute), CellText = i => i.ToString()! });
            grid.UpdateLayout(new Rect(0, 0, 300, 200));
            Assert.AreEqual(ScrollDirections.Horizontal, grid.HorizontalScroller.ScrollDirections, "too wide now");
        }

        [TestMethod]
        public void Stretch_overscroll_keeps_the_content_at_the_edge_and_stretches_it()
        {
            var content = new Border { Height = 300 };
            var viewer = new ScrollViewer { Content = content, OverscrollEffect = OverscrollEffect.Stretch };
            viewer.UpdateLayout(new Rect(0, 0, 100, 100));
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(30);

            viewer.OnTouchDown(new TouchEventArgs(new PointF(10, 10)));
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
            viewer.OnTouchMove(new TouchEventArgs(new PointF(10, 40))); // pull down past the top

            Assert.IsTrue(content.ClippingRect.Top > 0 && content.ClippingRect.Top <= viewer.OverscrollStretchShift, $"content glides only a little ({content.ClippingRect.Top})");
            Assert.IsTrue(content.OverscrollScale.Y > 1, "and stretches instead");
            Assert.AreEqual(0, content.OverscrollOrigin.Y, "from the pulled (top) edge");

            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(300);
            viewer.OnTouchUp(new TouchEventArgs(new PointF(10, 40)));
            for (var frame = 0; frame < 60; frame++)
            {
                ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
                viewer.OnFrameUpdate();
            }
            Assert.AreEqual(1f, content.OverscrollScale.Y, "relaxes back");
        }

        [TestMethod]
        public void Auto_hide_scroll_bars_fade_out_when_idle_and_return_when_scrolling()
        {
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(100);
            var viewer = new ScrollViewer { Content = new Border { Height = 300 }, ScrollBarVisibility = ScrollBarVisibility.AutoHide };
            viewer.UpdateLayout(new Rect(0, 0, 100, 100));
            Assert.AreEqual(1f, viewer.ScrollBarOpacity, "shown at first");

            ScreenSystem.TotalTime += TimeSpan.FromSeconds(2);
            Assert.AreEqual(0f, viewer.ScrollBarOpacity, "faded out after the idle delay");

            viewer.ScrollTo(new PointF(0, 50));
            Assert.AreEqual(1f, viewer.ScrollBarOpacity, "back while scrolling");
        }

        [TestMethod]
        public void Only_always_visible_bars_take_space_from_the_content()
        {
            var visibleContent = new Border { Height = 300 };
            var visible = new ScrollViewer { Content = visibleContent, ScrollBarVisibility = ScrollBarVisibility.Visible };
            visible.UpdateLayout(new Rect(0, 0, 100, 100));
            var overlayContent = new Border { Height = 300 };
            var overlay = new ScrollViewer { Content = overlayContent, ScrollBarVisibility = ScrollBarVisibility.AutoHide };
            overlay.UpdateLayout(new Rect(0, 0, 100, 100));

            Assert.IsTrue(visibleContent.ClippingRect.Width < 100, "a visible bar takes its width");
            Assert.AreEqual(100, overlayContent.ClippingRect.Width, "an auto-hiding bar is drawn over the content");
        }
    }
}
