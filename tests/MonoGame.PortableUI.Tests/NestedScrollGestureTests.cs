using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class NestedScrollGestureTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private sealed class Page
        {
            public ScreenEngine Engine = null!;
            public TestScreen Screen = null!;
            public VirtualInputSource Source = null!;
            public ScrollViewer Outer = null!;
            public ListBox List = null!;
            public DataGrid Grid = null!;
        }

        // Like the Android demo: a page scroll viewer holding a list and a data grid.
        private static Page CreatePage(Game game)
        {
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 600);
            var list = new ListBox { Height = 200 };
            for (var i = 0; i < 30; i++)
                list.Items.Add($"Item {i}");
            var grid = new DataGrid { Height = 200, RowHeight = 30, HeaderHeight = 36 };
            grid.Columns.Add(new DataGridColumn { Header = "N", Width = new GridLength(1, GridLengthUnit.Relative), CellText = i => i.ToString()!, SortKey = i => (int)i });
            grid.Columns.Add(new DataGridColumn { Header = "Q", Width = new GridLength(70, GridLengthUnit.Absolute), CellText = i => i.ToString()! });
            for (var i = 0; i < 30; i++)
                grid.Items.Add(i);
            var content = new StackPanel { Children = { new Border { Height = 100 }, list, new Border { Height = 50 }, grid, new Border { Height = 600 } } };
            var outer = new ScrollViewer { Content = content };
            var source = new VirtualInputSource();
            var screen = new TestScreen { Content = outer, InputSource = source };
            engine.NavigateToScreen(screen);
            Tick(engine, 0);
            return new Page { Engine = engine, Screen = screen, Source = source, Outer = outer, List = list, Grid = grid };
        }

        private static int _frame;

        private static void Tick(ScreenEngine engine, int ms) => engine.Update(new GameTime(TimeSpan.FromMilliseconds(++_frame * 16), TimeSpan.FromMilliseconds(16)));

        private static void Touch(Page page, TouchLocationState state, Vector2 position)
        {
            page.Source.SetTouches(new TouchCollection(new[] { new TouchLocation(1, state, position) }));
            Tick(page.Engine, 16);
        }

        [TestMethod]
        public void A_drag_that_starts_in_an_inner_list_never_scrolls_the_outer_viewer()
        {
            using var game = new Game();
            var page = CreatePage(game);
            var start = new Vector2(100, page.List.ClippingRect.Top + 100);

            Touch(page, TouchLocationState.Pressed, start);
            // Drag up far beyond the list's own scroll range.
            for (var y = 0; y <= 900; y += 30)
            {
                Touch(page, TouchLocationState.Moved, start - new Vector2(0, y));
            }

            Touch(page, TouchLocationState.Released, start - new Vector2(0, 900));
            page.Source.SetTouches(new TouchCollection(Array.Empty<TouchLocation>()));
            Tick(page.Engine, 16);

            Assert.AreEqual(0, page.Outer.Offset.Y, "the outer viewer stays put while the list owns the gesture");

            // A new drag outside the list scrolls the page again.
            var outside = new Vector2(100, page.List.ClippingRect.Bottom + 25); // the gap between list and grid
            Touch(page, TouchLocationState.Pressed, outside);
            for (var y = 10; y <= 100; y += 10)
                Touch(page, TouchLocationState.Moved, outside - new Vector2(0, y));
            Touch(page, TouchLocationState.Released, outside - new Vector2(0, 100));
            Assert.IsTrue(page.Outer.Offset.Y > 50, $"page scrolled {page.Outer.Offset.Y} after a fresh drag");
        }

        [TestMethod]
        public void A_drag_on_the_grid_header_scrolls_the_page_smoothly_without_layout_passes()
        {
            using var game = new Game();
            var page = CreatePage(game);
            var header = page.Grid.HeaderRow.ClippingRect;
            var start = new Vector2(header.Left + 40, header.Top + header.Height / 2);

            Touch(page, TouchLocationState.Pressed, start);
            var layoutPasses = 0;
            var lastOffset = page.Outer.Offset.Y;
            for (var y = 10; y <= 200; y += 10)
            {
                Touch(page, TouchLocationState.Moved, start - new Vector2(0, y));
                layoutPasses += page.Engine.LayoutPassesThisFrame;
                Assert.IsTrue(page.Outer.Offset.Y >= lastOffset, "the page scrolls monotonically");
                lastOffset = page.Outer.Offset.Y;
            }
            Touch(page, TouchLocationState.Released, start - new Vector2(0, 200));

            Assert.IsTrue(page.Outer.Offset.Y > 150, $"page scrolled {page.Outer.Offset.Y}; grid h={page.Grid.HorizontalScroller.Offset} v={page.Grid.Scroller.Offset} header={header} gridRect={page.Grid.ClippingRect}");
            Assert.IsTrue(layoutPasses <= 2, $"{layoutPasses} layout passes during a 20-frame pan");
        }

        [TestMethod]
        public void The_owning_list_keeps_scrolling_when_the_finger_leaves_it()
        {
            using var game = new Game();
            var page = CreatePage(game);
            page.List.Items.AddRange(Enumerable.Range(30, 200).Select(i => (object)$"Item {i}"));
            Tick(page.Engine, 16);
            var listViewer = (ScrollViewer)page.List.GetVisualChild(0);
            var start = new Vector2(100, page.List.ClippingRect.Bottom - 10);

            Touch(page, TouchLocationState.Pressed, start);
            float offsetWhenLeaving = -1;
            for (var y = 0; y <= 400; y += 20)
            {
                var position = start - new Vector2(0, y);
                Touch(page, TouchLocationState.Moved, position);
                if (offsetWhenLeaving < 0 && !page.List.ClippingRect.Contains(new PointF(position.X, position.Y)))
                    offsetWhenLeaving = listViewer.Offset.Y;
            }

            Assert.IsTrue(listViewer.Offset.Y > offsetWhenLeaving + 100, $"list kept scrolling outside its bounds ({offsetWhenLeaving} -> {listViewer.Offset.Y})");
            Assert.AreEqual(0, page.Outer.Offset.Y);
            Touch(page, TouchLocationState.Released, start - new Vector2(0, 400));
        }

        private static void Tap(Page page, Vector2 at)
        {
            Touch(page, TouchLocationState.Pressed, at);
            Touch(page, TouchLocationState.Released, at);
            page.Source.SetTouches(new TouchCollection(Array.Empty<TouchLocation>()));
            Tick(page.Engine, 16);
        }

        [TestMethod]
        public void Tapping_the_grid_header_or_a_row_keeps_the_rows_visible()
        {
            using var game = new Game();
            var page = CreatePage(game);
            var header = page.Grid.HeaderRow.ClippingRect;

            Tap(page, new Vector2(header.Left + 20, header.Top + header.Height / 2));
            AssertRowsVisible(page.Grid);

            var firstRow = page.Grid.Rows[0].ClippingRect;
            Tap(page, new Vector2(firstRow.Left + 20, firstRow.Top + firstRow.Height / 2));
            AssertRowsVisible(page.Grid);
        }

        private static void AssertRowsVisible(DataGrid grid)
        {
            var viewport = grid.Scroller.ClippingRect;
            var visible = grid.Rows.Count(r => r.ClippingRect.Height > 0 && r.ClippingRect.Bottom > viewport.Top && r.ClippingRect.Top < viewport.Bottom);
            Assert.IsTrue(visible >= 4, $"{visible} of {grid.Rows.Count} realized rows inside the grid viewport {viewport}; first row {(grid.Rows.Count > 0 ? grid.Rows[0].ClippingRect.ToString() : "-")}");
        }

        [TestMethod]
        public void After_scrolling_the_page_tapping_the_grid_keeps_its_rows()
        {
            using var game = new Game();
            var page = CreatePage(game);
            // Scroll the page by dragging in the gap above the list.
            var gap = new Vector2(100, 50);
            Touch(page, TouchLocationState.Pressed, gap + new Vector2(0, 0));
            for (var y = 10; y <= 300; y += 10)
                Touch(page, TouchLocationState.Moved, gap + new Vector2(0, 400 - y) - new Vector2(0, 350));
            Touch(page, TouchLocationState.Released, gap);
            page.Source.SetTouches(new TouchCollection(Array.Empty<TouchLocation>()));
            for (var i = 0; i < 30; i++)
                Tick(page.Engine, 16); // fling settles
            AssertRowsVisible(page.Grid);

            var header = page.Grid.HeaderRow.ClippingRect;
            Tap(page, new Vector2(header.Left + 20, header.Top + header.Height / 2));
            AssertRowsVisible(page.Grid);
            var row = page.Grid.Rows[2].ClippingRect;
            Tap(page, new Vector2(row.Left + 20, row.Top + row.Height / 2));
            AssertRowsVisible(page.Grid);
        }

        [TestMethod]
        public void A_fast_fling_to_the_end_of_a_list_keeps_rows_in_view()
        {
            using var game = new Game();
            var page = CreatePage(game);
            var listViewer = (ScrollViewer)page.List.GetVisualChild(0);
            var start = new Vector2(100, page.List.ClippingRect.Bottom - 10);

            Touch(page, TouchLocationState.Pressed, start);
            for (var y = 0; y <= 180; y += 60)
                Touch(page, TouchLocationState.Moved, start - new Vector2(0, y));
            Touch(page, TouchLocationState.Released, start - new Vector2(0, 180));
            page.Source.SetTouches(new TouchCollection(Array.Empty<TouchLocation>()));
            for (var i = 0; i < 30; i++)
                Tick(page.Engine, 16);

            var viewport = listViewer.ClippingRect;
            var visible = page.List.ItemButtons.Count(b => b.ClippingRect.Bottom > viewport.Top && b.ClippingRect.Top < viewport.Bottom);
            Assert.IsTrue(visible >= 6, $"{visible} rows in view at offset {listViewer.Offset.Y}; realized {string.Join(",", page.List.ItemButtons.Select(b => b.Tag))}; first {page.List.ItemButtons[0].ClippingRect} viewport {viewport}");
        }

        [TestMethod]
        public void Scrolling_the_page_then_the_inner_list_keeps_the_list_content_in_place()
        {
            using var game = new Game();
            var page = CreatePage(game);
            var listViewer = (ScrollViewer)page.List.GetVisualChild(0);

            page.Outer.ScrollTo(new PointF(0, 120)); // shifts the list subtree without a layout pass
            listViewer.ScrollTo(new PointF(0, 40));

            var first = page.List.ItemButtons[0];
            Assert.AreEqual(listViewer.ClippingRect.Top - 40, first.ClippingRect.Top, 0.5f,
                $"row 0 must sit 40 above the list viewport; list viewport {listViewer.ClippingRect}, row {first.ClippingRect}");
        }

        [TestMethod]
        public void A_jittery_vertical_swipe_over_the_grid_header_scrolls_the_page_not_the_grid()
        {
            using var game = new Game();
            var page = CreatePage(game);
            var header = page.Grid.HeaderRow.ClippingRect;
            var start = new Vector2(header.Left + 60, header.Top + header.Height / 2);

            Touch(page, TouchLocationState.Pressed, start);
            for (var step = 1; step <= 15; step++)
                Touch(page, TouchLocationState.Moved, start + new Vector2(step % 2 == 0 ? 4 : -4, -12 * step));
            Touch(page, TouchLocationState.Released, start + new Vector2(0, -180));

            Assert.AreEqual(0, page.Grid.HorizontalScroller.Offset.X, "columns fit: nothing to drag sideways");
            Assert.IsTrue(page.Outer.Offset.Y > 120, $"the page scrolled ({page.Outer.Offset.Y})");
        }

        [TestMethod]
        public void Starting_to_drag_a_list_never_shows_a_pressed_row()
        {
            using var game = new Game();
            var page = CreatePage(game);
            var start = new Vector2(100, page.List.ClippingRect.Top + 50);

            Touch(page, TouchLocationState.Pressed, start);
            var row = page.List.ItemButtons.First(b => b.ClippingRect.Contains(new PointF(start.X, start.Y)));
            for (var y = 6; y <= 60; y += 6)
            {
                Touch(page, TouchLocationState.Moved, start - new Vector2(0, y)); // 16 ms per frame: a quick drag
                Assert.AreNotEqual(ControlVisualState.Pressed, row.CurrentVisualStateForTests, $"row looked pressed after {y} px");
            }
            Touch(page, TouchLocationState.Released, start - new Vector2(0, 60));
        }

        [TestMethod]
        public void Holding_a_row_then_moving_selects_rows_instead_of_scrolling()
        {
            using var game = new Game();
            var page = CreatePage(game);
            var listViewer = (ScrollViewer)page.List.GetVisualChild(0);
            var row2 = page.List.ItemButtons[2].ClippingRect;
            var start = new Vector2(100, row2.Top + row2.Height / 2);
            var invoked = -1;
            page.List.ItemInvoked += (s, e) => invoked = e.Index;

            Touch(page, TouchLocationState.Pressed, start);
            for (var i = 0; i < 15; i++) // rest ~240 ms (16 ms frames)
                Touch(page, TouchLocationState.Moved, start + new Vector2(0, i % 2)); // tiny jitter below the slop
            Assert.AreEqual(2, page.List.SelectedIndex, "holding selects the row");

            var rowHeight = row2.Height;
            Touch(page, TouchLocationState.Moved, start + new Vector2(0, rowHeight * 2));
            Assert.AreEqual(4, page.List.SelectedIndex, "moving selects the row under the finger");
            Assert.AreEqual(0, listViewer.Offset.Y, "and does not scroll the list");
            Assert.AreEqual(0, page.Outer.Offset.Y, "nor the page");

            Touch(page, TouchLocationState.Moved, start);
            Touch(page, TouchLocationState.Released, start);
            Assert.AreEqual(2, invoked, "releasing on the start row activates it");
        }

        [TestMethod]
        public void A_quick_drag_still_scrolls_the_list()
        {
            using var game = new Game();
            var page = CreatePage(game);
            var listViewer = (ScrollViewer)page.List.GetVisualChild(0);
            var start = new Vector2(100, page.List.ClippingRect.Bottom - 20);

            Touch(page, TouchLocationState.Pressed, start);
            for (var y = 10; y <= 100; y += 10)
                Touch(page, TouchLocationState.Moved, start - new Vector2(0, y));
            Touch(page, TouchLocationState.Released, start - new Vector2(0, 100));

            Assert.IsTrue(listViewer.Offset.Y > 50, $"scrolled {listViewer.Offset.Y}");
            Assert.AreEqual(-1, page.List.SelectedIndex, "nothing selected by a scroll");
        }
    }
}
