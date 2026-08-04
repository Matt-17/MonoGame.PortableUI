using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class DataGridRegressionTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private sealed record Person(string Name, int Age);

        private static DataGrid CreateGrid(IEnumerable<object> items, params DataGridColumn[] columns)
        {
            var grid = new DataGrid
            {
                BorderThickness = new Thickness(0),
                HeaderHeight = 20,
                RowHeight = 20
            };
            grid.Columns.AddRange(columns);
            grid.Items.AddRange(items);
            grid.Refresh();
            return grid;
        }

        private static DataGridColumn NameColumn() => new DataGridColumn
        {
            Header = "Name",
            CellText = item => ((Person)item).Name,
            SortKey = item => ((Person)item).Name
        };

        private static DataGridColumn AgeColumn(GridLength width) => new DataGridColumn
        {
            Header = "Age",
            Width = width,
            CellText = item => ((Person)item).Age.ToString(),
            SortKey = item => ((Person)item).Age
        };

        [TestMethod]
        public void Absolute_and_star_columns_fill_available_width()
        {
            var grid = CreateGrid(
                new object[] { new Person("A", 1) },
                new DataGridColumn { Header = "Name", Width = new GridLength(100, GridLengthUnit.Absolute), CellText = i => ((Person)i).Name },
                new DataGridColumn { Header = "Age", Width = new GridLength(1, GridLengthUnit.Relative), CellText = i => ((Person)i).Age.ToString() });

            grid.UpdateLayout(new Rect(0, 0, 300, 200));

            Assert.AreEqual(100, grid.Columns[0].ActualWidth, 0.001f);
            Assert.AreEqual(200, grid.Columns[1].ActualWidth, 0.001f);
        }

        [TestMethod]
        public void Two_star_columns_split_available_width_by_weight()
        {
            var grid = CreateGrid(
                new object[] { new Person("A", 1) },
                new DataGridColumn { Header = "Name", Width = new GridLength(1, GridLengthUnit.Relative), CellText = i => ((Person)i).Name },
                new DataGridColumn { Header = "Age", Width = new GridLength(3, GridLengthUnit.Relative), CellText = i => ((Person)i).Age.ToString() });

            grid.UpdateLayout(new Rect(0, 0, 400, 200));

            Assert.AreEqual(100, grid.Columns[0].ActualWidth, 0.001f);
            Assert.AreEqual(300, grid.Columns[1].ActualWidth, 0.001f);
        }

        [TestMethod]
        public void Auto_column_widens_for_longer_content()
        {
            var shortGrid = CreateGrid(
                new object[] { new Person("Al", 1) },
                new DataGridColumn { Header = "N", Width = GridLength.Auto, CellText = i => ((Person)i).Name });
            var longGrid = CreateGrid(
                new object[] { new Person("Alexander the Great", 1) },
                new DataGridColumn { Header = "N", Width = GridLength.Auto, CellText = i => ((Person)i).Name });

            shortGrid.UpdateLayout(new Rect(0, 0, 500, 200));
            longGrid.UpdateLayout(new Rect(0, 0, 500, 200));

            Assert.IsTrue(longGrid.Columns[0].ActualWidth > shortGrid.Columns[0].ActualWidth,
                $"expected long ({longGrid.Columns[0].ActualWidth}) > short ({shortGrid.Columns[0].ActualWidth})");
        }

        [TestMethod]
        public void Star_column_never_shrinks_below_min_width()
        {
            var grid = CreateGrid(
                new object[] { new Person("A", 1) },
                new DataGridColumn { Header = "Name", Width = new GridLength(260, GridLengthUnit.Absolute), CellText = i => ((Person)i).Name },
                new DataGridColumn { Header = "Age", Width = new GridLength(1, GridLengthUnit.Relative), MinWidth = 80, CellText = i => ((Person)i).Age.ToString() });

            // Leftover after the absolute column is only 40, but MinWidth forces 80.
            grid.UpdateLayout(new Rect(0, 0, 300, 200));

            Assert.AreEqual(80, grid.Columns[1].ActualWidth, 0.001f);
        }

        [TestMethod]
        public void Header_click_sorts_ascending_then_descending()
        {
            var people = new object[] { new Person("C", 3), new Person("A", 1), new Person("B", 2) };
            var age = AgeColumn(new GridLength(1, GridLengthUnit.Relative));
            var grid = CreateGrid(people, NameColumn(), age);

            grid.SortByColumn(age);
            Assert.IsTrue(grid.SortAscending);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, grid.Items.Cast<Person>().Select(p => p.Age).ToArray());

            grid.SortByColumn(age);
            Assert.IsFalse(grid.SortAscending);
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, grid.Items.Cast<Person>().Select(p => p.Age).ToArray());
        }

        [TestMethod]
        public void Sort_falls_back_to_text_when_no_sort_key()
        {
            var people = new object[] { new Person("Charlie", 3), new Person("alice", 1), new Person("Bob", 2) };
            var column = new DataGridColumn { Header = "Name", CellText = i => ((Person)i).Name };
            var grid = CreateGrid(people, column);

            grid.SortByColumn(column);

            CollectionAssert.AreEqual(new[] { "alice", "Bob", "Charlie" }, grid.Items.Cast<Person>().Select(p => p.Name).ToArray());
        }

        [TestMethod]
        public void Sort_is_stable_for_equal_keys()
        {
            // All same age; stable sort keeps insertion order.
            var people = new object[] { new Person("First", 5), new Person("Second", 5), new Person("Third", 5) };
            var age = AgeColumn(new GridLength(1, GridLengthUnit.Relative));
            var grid = CreateGrid(people, NameColumn(), age);

            grid.SortByColumn(age);

            CollectionAssert.AreEqual(new[] { "First", "Second", "Third" }, grid.Items.Cast<Person>().Select(p => p.Name).ToArray());
        }

        [TestMethod]
        public void Sort_preserves_selected_item_across_reorder()
        {
            var target = new Person("B", 2);
            var people = new object[] { new Person("C", 3), new Person("A", 1), target };
            var age = AgeColumn(new GridLength(1, GridLengthUnit.Relative));
            var grid = CreateGrid(people, NameColumn(), age);
            grid.SelectedIndex = 2; // "B"

            grid.SortByColumn(age); // order becomes A,B,C

            Assert.AreSame(target, grid.SelectedItem);
            Assert.AreEqual(1, grid.SelectedIndex);
        }

        [TestMethod]
        public void Selection_change_raises_event_and_tracks_item()
        {
            var people = new object[] { new Person("A", 1), new Person("B", 2), new Person("C", 3) };
            var grid = CreateGrid(people, NameColumn());
            var raised = 0;
            grid.SelectionChanged += (_, _) => raised++;

            grid.SelectedIndex = 1;

            Assert.AreEqual(1, raised);
            Assert.AreSame(people[1], grid.SelectedItem);
        }

        [TestMethod]
        public void Keyboard_navigation_moves_and_clamps_selection()
        {
            var people = new object[] { new Person("A", 1), new Person("B", 2) };
            var grid = CreateGrid(people, NameColumn());

            grid.OnKeyPressed(KeyboardCommand.CursorDown);
            grid.OnKeyPressed(KeyboardCommand.CursorDown);
            grid.OnKeyPressed(KeyboardCommand.CursorDown);
            Assert.AreEqual(1, grid.SelectedIndex);

            grid.OnKeyPressed(KeyboardCommand.CursorUp);
            grid.OnKeyPressed(KeyboardCommand.CursorUp);
            Assert.AreEqual(0, grid.SelectedIndex);
        }

        [TestMethod]
        public void Enter_key_invokes_selected_row()
        {
            var people = new object[] { new Person("A", 1), new Person("B", 2) };
            var grid = CreateGrid(people, NameColumn());
            DataGridRowInvokedEventArgs? invoked = null;
            grid.RowInvoked += (_, e) => invoked = e;
            grid.SelectedIndex = 1;

            grid.OnKeyPressed(KeyboardCommand.Enter);

            Assert.IsNotNull(invoked);
            Assert.AreEqual(1, invoked!.Index);
            Assert.AreSame(people[1], invoked.Item);
        }

        [TestMethod]
        public void Clicking_a_row_selects_it()
        {
            var screen = new TestScreen();
            var people = new object[] { new Person("A", 1), new Person("B", 2), new Person("C", 3) };
            var grid = CreateGrid(people, NameColumn());
            screen.Content = grid;
            grid.UpdateLayout(new Rect(0, 0, 200, 200));

            grid.Rows[2].OnClick();

            Assert.AreEqual(2, grid.SelectedIndex);
        }

        [TestMethod]
        public void Dragging_a_header_splitter_resizes_and_clamps_to_min_width()
        {
            var grid = CreateGrid(
                new object[] { new Person("A", 1) },
                new DataGridColumn { Header = "Name", Width = new GridLength(150, GridLengthUnit.Absolute), MinWidth = 40, CanResize = true, CellText = i => ((Person)i).Name },
                new DataGridColumn { Header = "Age", Width = new GridLength(1, GridLengthUnit.Relative), CellText = i => ((Person)i).Age.ToString() });
            grid.UpdateLayout(new Rect(0, 0, 400, 200));

            var header = grid.HeaderRow;
            var boundaryX = header.BoundingRect.Left + grid.ColumnOffset(1); // trailing edge of column 0
            var y = header.BoundingRect.Top + 5;

            header.OnMouseDown(new MouseEventArgs(new PointF(boundaryX, y), MouseButton.Left));
            header.OnMouseMove(new MouseEventArgs(new PointF(boundaryX + 40, y), new List<MouseButton> { MouseButton.Left }));
            header.OnMouseUp(new MouseEventArgs(new PointF(boundaryX + 40, y), MouseButton.Left));

            grid.UpdateLayout(new Rect(0, 0, 400, 200));
            Assert.AreEqual(190, grid.Columns[0].ActualWidth, 0.001f, "column should widen by the drag delta");

            // Drag far left, past the minimum width.
            header.OnMouseDown(new MouseEventArgs(new PointF(header.BoundingRect.Left + grid.ColumnOffset(1), y), MouseButton.Left));
            header.OnMouseMove(new MouseEventArgs(new PointF(header.BoundingRect.Left - 500, y), new List<MouseButton> { MouseButton.Left }));
            header.OnMouseUp(new MouseEventArgs(new PointF(header.BoundingRect.Left - 500, y), MouseButton.Left));

            grid.UpdateLayout(new Rect(0, 0, 400, 200));
            Assert.AreEqual(40, grid.Columns[0].ActualWidth, 0.001f, "column should clamp to MinWidth");
        }

        [TestMethod]
        public void Header_stays_fixed_while_body_scrolls()
        {
            var people = Enumerable.Range(0, 30).Select(i => (object)new Person($"P{i}", i)).ToArray();
            var grid = CreateGrid(people, NameColumn());
            grid.Height = 100;
            grid.UpdateLayout(new Rect(0, 0, 200, 100));
            var headerTop = grid.HeaderRow.BoundingRect.Top;

            grid.Scroller.ScrollTo(new PointF(0, 200));

            Assert.IsTrue(grid.Scroller.Offset.Y > 0, "body should scroll");
            Assert.AreEqual(headerTop, grid.HeaderRow.BoundingRect.Top, 0.001f, "header must not move when the body scrolls");
        }

        [TestMethod]
        public void Selecting_an_offscreen_row_scrolls_it_into_view()
        {
            var people = Enumerable.Range(0, 30).Select(i => (object)new Person($"P{i}", i)).ToArray();
            var grid = CreateGrid(people, NameColumn());
            grid.Height = 100;
            grid.UpdateLayout(new Rect(0, 0, 200, 100));

            // First CursorDown selects index 0, so N presses land on index N-1.
            for (var i = 0; i < 20; i++)
                grid.OnKeyPressed(KeyboardCommand.CursorDown);

            Assert.AreEqual(19, grid.SelectedIndex);
            Assert.IsTrue(grid.Scroller.Offset.Y > 0, "selecting a row below the fold should scroll it into view");
        }

        [TestMethod]
        public void Cell_template_column_creates_custom_cell_controls()
        {
            var people = new object[] { new Person("A", 1), new Person("B", 2) };
            var built = 0;
            var column = new DataGridColumn
            {
                Header = "Custom",
                CellTemplate = item =>
                {
                    built++;
                    return new TextBlock { Text = "#" + ((Person)item).Age };
                }
            };
            var grid = CreateGrid(people, column);

            grid.UpdateLayout(new Rect(0, 0, 200, 200));

            Assert.AreEqual(2, built);
            var firstCell = grid.Rows[0].GetDescendants().OfType<TextBlock>().First();
            Assert.AreEqual("#1", firstCell.Text);
        }
    }
}
