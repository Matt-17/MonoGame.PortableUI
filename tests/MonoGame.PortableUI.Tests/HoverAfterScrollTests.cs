using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class HoverAfterScrollTests
    {
        private sealed class TestScreen : Screen
        {
        }

        [TestMethod]
        public void Scrolling_under_a_resting_pointer_moves_the_hover_to_the_row_now_under_it()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 300);
            var list = new ListBox { Width = 200, Height = 150, ItemHeight = 30, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            for (var i = 0; i < 40; i++)
                list.Items.Add($"Row {i}");
            var screen = new TestScreen { Content = list };
            var input = new VirtualInputSource();
            screen.InputSource = input;
            engine.NavigateToScreen(screen);
            void Tick()
            {
                ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
                engine.Update(new GameTime(ScreenSystem.TotalTime, TimeSpan.FromMilliseconds(16)));
            }
            Tick();

            var pointer = new PointF(50, 45);
            input.SetPointer(new PointF(50, 44));
            Tick();
            input.SetPointer(pointer);
            Tick();
            Button HoveredRow() => list.ItemButtons.Single(b => b.IsMouseHovering && b.ClippingRect.Contains(pointer));
            var before = HoveredRow();

            input.SetScrollWheelValue(-480);
            for (var i = 0; i < 60; i++)
                Tick();

            var viewer = (ScrollViewer)list.GetVisualChild(0);
            Assert.IsTrue(viewer.Offset.Y > 30, $"the wheel scrolled the list (offset {viewer.Offset.Y})");
            var after = HoveredRow();
            Assert.AreNotSame(before, after, "another row object");
            Assert.AreNotEqual(before.Text, after.Text, "a different row is under the pointer now and it is hovered");
            Assert.AreEqual(1, list.ItemButtons.Count(b => b.IsMouseHovering), "only the row under the pointer is hovered");

            // Scroll on, then nudge the pointer inside the same row (no edge is crossed).
            input.SetScrollWheelValue(-960);
            for (var i = 0; i < 60; i++)
                Tick();
            foreach (var dx in new[] { 2, 4, 6 })
            {
                pointer = new PointF(50 + dx, 45);
                input.SetPointer(pointer);
                Tick();
                Assert.AreEqual(1, list.ItemButtons.Count(b => b.IsMouseHovering && b.ClippingRect.Contains(pointer)), "the row under the moving pointer is hovered");
            }
        }

        [TestMethod]
        public void Scrolling_an_open_dropdown_keeps_hovering_the_row_under_the_pointer()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 600);
            var combo = new ComboBox { Width = 200, Height = 30, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, DropDownMaxHeight = 150 };
            for (var i = 0; i < 40; i++)
                combo.Items.Add($"Row {i}");
            var screen = new TestScreen { Content = combo };
            var input = new VirtualInputSource();
            screen.InputSource = input;
            engine.NavigateToScreen(screen);
            void Tick()
            {
                ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
                engine.Update(new GameTime(ScreenSystem.TotalTime, TimeSpan.FromMilliseconds(16)));
            }
            Tick();
            input.SetPointer(new PointF(50, 15));
            Tick();
            input.SetPointer(new PointF(50, 15), leftDown: true);
            Tick();
            input.SetPointer(new PointF(50, 15));
            for (var i = 0; i < 20; i++)
                Tick();
            var list = screen.FlyOutContent is { } flyoutContent ? FindList(flyoutContent) : null;
            Assert.IsNotNull(list, "dropdown open");

            var pointer = new PointF(50, list!.ClippingRect.Top + 50);
            input.SetPointer(new PointF(pointer.X, pointer.Y - 1));
            Tick();
            input.SetPointer(pointer);
            Tick();
            Assert.AreEqual(1, list.ItemButtons.Count(b => b.IsMouseHovering && b.ClippingRect.Contains(pointer)), "hovered before scrolling");

            input.SetScrollWheelValue(-480);
            for (var i = 0; i < 60; i++)
                Tick();

            var viewer = (ScrollViewer)list.GetVisualChild(0);
            Assert.IsTrue(viewer.Offset.Y > 30, "the dropdown scrolled");
            Assert.AreEqual(1, list.ItemButtons.Count(b => b.IsMouseHovering && b.ClippingRect.Contains(pointer)), "the row now under the pointer is hovered");
            Assert.AreEqual(1, list.ItemButtons.Count(b => b.IsMouseHovering), "and only that one");
        }

        [TestMethod]
        public void List_rows_meet_the_frame_exactly_on_half_pixel_positions()
        {
            // Frame lines cover the pixels whose centres they contain: top line [20.5, 21.5) is
            // pixel row 20, bottom line [119.5, 120.5) is row 119 — rows must fill 21..118, no gap,
            // no overlap; same for the sides.
            var list = new ListBox { BorderThickness = new Thickness(1) };
            for (var i = 0; i < 20; i++)
                list.Items.Add($"Row {i}");
            list.UpdateLayout(new Rect(10.5f, 20.5f, 200, 100));
            var area = list.GetVisualChild(0).ClippingRect;

            Assert.AreEqual(21, area.Top, "starts right under the top line");
            Assert.AreEqual(119, area.Bottom, "ends right above the bottom line");
            Assert.AreEqual(11, area.Left);
            Assert.AreEqual(209, area.Right);
        }

        private static ListBox? FindList(Control root)
        {
            if (root is ListBox list)
                return list;
            for (var i = 0; i < root.VisualChildCount; i++)
            {
                if (FindList(root.GetVisualChild(i)) is { } found)
                    return found;
            }
            return null;
        }
    }
}
