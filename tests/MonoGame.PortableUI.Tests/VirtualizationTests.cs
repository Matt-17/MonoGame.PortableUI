using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class VirtualizationTests
    {
        private static ListBox CreateLongList(int count, out int created)
        {
            var list = new ListBox { Width = 200, Height = 200, ItemHeight = 20 };
            for (var i = 0; i < count; i++)
                list.Items.Add($"Item {i}");
            list.UpdateLayout(new Rect(0, 0, 200, 200));
            created = list.ItemButtons.Count;
            return list;
        }

        [TestMethod]
        public void Only_rows_in_view_are_realized()
        {
            var list = CreateLongList(10_000, out var realized);

            Assert.IsTrue(realized < 30, $"{realized} rows realized for a 200 px viewport");
            Assert.AreEqual("Item 0", list.ItemButtons[0].Text);
        }

        [TestMethod]
        public void Scrolling_rebinds_recycled_rows_to_the_items_now_in_view()
        {
            var list = CreateLongList(10_000, out _);
            var rowsBefore = list.ItemButtons.ToArray();

            list.SelectedIndex = 9_999;
            list.ScrollSelectedIntoView();

            var last = list.ItemButtons.Single(b => b.Tag is int index && index == 9_999);
            Assert.AreEqual("Item 9999", last.Text);
            Assert.IsTrue(last.ClippingRect.Bottom <= 200.5f && last.ClippingRect.Top >= 0, "the selected row is inside the viewport");
            Assert.IsTrue(list.ItemButtons.All(b => rowsBefore.Contains(b)), "rows are recycled, not created");
        }

        [TestMethod]
        public void Keyboard_paging_and_end_work_on_a_virtualized_list()
        {
            var list = CreateLongList(500, out _);
            list.SelectedIndex = 0;

            list.OnKeyPressed(KeyboardCommand.End, KeyboardModifiers.None);
            Assert.AreEqual(499, list.SelectedIndex);
            Assert.IsTrue(list.ItemButtons.Any(b => b.Tag is int i && i == 499));

            list.OnKeyPressed(KeyboardCommand.PageUp, KeyboardModifiers.None);
            Assert.IsTrue(list.SelectedIndex < 499 && list.SelectedIndex > 480);
        }

        [TestMethod]
        public void Selection_visuals_follow_items_not_recycled_controls()
        {
            var list = CreateLongList(1_000, out _);
            list.SelectedIndex = 1;
            var selectedBrush = list.SelectedItemBackgroundBrush;

            list.SelectedIndex = 900;
            list.ScrollSelectedIntoView();

            foreach (var row in list.ItemButtons)
            {
                var isSelectedItem = row.Tag is int index && index == 900;
                Assert.AreEqual(isSelectedItem, ReferenceEquals(row.BackgroundBrush, selectedBrush), $"row {row.Tag}");
            }
        }

        [TestMethod]
        public void Removing_items_shrinks_the_realized_range()
        {
            var list = CreateLongList(100, out _);
            list.Items.RemoveRange(3, 97);
            list.UpdateLayout(new Rect(0, 0, 200, 200));

            Assert.AreEqual(3, list.ItemButtons.Count);
        }

        private static DataGrid CreateLongGrid(int count)
        {
            var grid = new DataGrid { Width = 300, Height = 240, RowHeight = 24, HeaderHeight = 28 };
            grid.Columns.Add(new DataGridColumn { Header = "N", CellText = i => ((int)i).ToString(), SortKey = i => (int)i });
            for (var i = 0; i < count; i++)
                grid.Items.Add(i);
            grid.UpdateLayout(new Rect(0, 0, 300, 240));
            return grid;
        }

        [TestMethod]
        public void Data_grid_realizes_only_visible_rows_and_follows_sorting()
        {
            var grid = CreateLongGrid(5_000);
            Assert.IsTrue(grid.Rows.Count < 30, $"{grid.Rows.Count} rows realized");

            grid.SortBy(grid.Columns[0], false);
            grid.UpdateLayout(new Rect(0, 0, 300, 240));
            Assert.AreEqual(4_999, grid.DisplayedItems.First());
            Assert.AreEqual(0, grid.Rows[0].Index, "first realized row is display position 0");

            grid.SelectedIndex = 0; // item 0 is the last displayed row after the descending sort
            grid.SelectRow(4_999, false);
            Assert.IsTrue(grid.Rows.Any(r => r.Index == 4_999), "selecting brings the last row into view");
        }
    }
}
