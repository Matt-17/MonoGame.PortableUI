using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class PanelLayoutTests
    {
        private static Border Item(float width, float height) => new Border
        {
            Width = width,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        [TestMethod]
        public void Stack_panel_spacing_separates_visible_children_only()
        {
            var first = Item(50, 20);
            var gone = Item(50, 20);
            gone.IsGone = true;
            var last = Item(50, 20);
            var stack = new StackPanel { Spacing = 10 };
            stack.AddChild(first);
            stack.AddChild(gone);
            stack.AddChild(last);

            Assert.AreEqual(50, stack.MeasureLayout().Height, 0.001f);

            stack.UpdateLayout(new Rect(0, 0, 200, 200));
            Assert.AreEqual(30, last.BoundingRect.Top, 0.001f);
        }

        [TestMethod]
        public void Wrap_panel_with_a_fixed_width_breaks_into_rows()
        {
            var wrap = new WrapPanel { Width = 100, HorizontalSpacing = 10, VerticalSpacing = 5 };
            var items = new Border[5];
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = Item(40, 20);
                wrap.AddChild(items[i]);
            }

            // 40 + 10 + 40 = 90 fits, a third item would need 140: two per row, three rows.
            Assert.AreEqual(3 * 20 + 2 * 5, wrap.MeasureLayout().Height, 0.001f);

            wrap.UpdateLayout(new Rect(0, 0, 100, 200));
            Assert.AreEqual(50, items[1].BoundingRect.Left, 0.001f);
            Assert.AreEqual(0, items[2].BoundingRect.Left, 0.001f);
            Assert.AreEqual(25, items[2].BoundingRect.Top, 0.001f);
            Assert.AreEqual(50, items[4].BoundingRect.Top, 0.001f);
        }

        [TestMethod]
        public void Auto_sized_wrap_panel_measures_against_its_last_arranged_width()
        {
            var wrap = new WrapPanel();
            for (var i = 0; i < 6; i++)
                wrap.AddChild(Item(40, 20));
            var host = new Border { Content = wrap };

            // First pass: width unknown, so one row; arranging at 100 px wraps to three rows and
            // the next pass measures the correct height.
            host.UpdateLayout(new Rect(0, 0, 100, 400));
            host.UpdateLayout(new Rect(0, 0, 100, 400));

            Assert.AreEqual(60, wrap.MeasureLayout().Height, 0.001f);
        }

        [TestMethod]
        public void Wrap_panel_item_size_overrides_child_sizes()
        {
            var wrap = new WrapPanel { Width = 100, ItemWidth = 50, ItemHeight = 30 };
            for (var i = 0; i < 4; i++)
                wrap.AddChild(new Border());

            Assert.AreEqual(60, wrap.MeasureLayout().Height, 0.001f);
        }

        [TestMethod]
        public void Uniform_grid_derives_a_square_layout_and_equal_cells()
        {
            var grid = new UniformGrid { Spacing = 4 };
            var items = new Border[5];
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = new Border { Width = 30, Height = 20 };
                grid.AddChild(items[i]);
            }

            Assert.AreEqual((2, 3), grid.GetDimensions());
            var size = grid.MeasureLayout();
            Assert.AreEqual(3 * 30 + 2 * 4, size.Width, 0.001f);
            Assert.AreEqual(2 * 20 + 4, size.Height, 0.001f);

            grid.UpdateLayout(new Rect(0, 0, 98, 44));
            Assert.AreEqual(34, items[1].BoundingRect.Left, 0.001f);
            Assert.AreEqual(24, items[3].BoundingRect.Top, 0.001f);
            Assert.AreEqual(0, items[3].BoundingRect.Left, 0.001f);
        }

        [TestMethod]
        public void Uniform_grid_with_fixed_columns_adds_rows_as_needed()
        {
            var grid = new UniformGrid { Columns = 4 };
            for (var i = 0; i < 9; i++)
                grid.AddChild(new Border { Width = 10, Height = 10 });

            Assert.AreEqual((3, 4), grid.GetDimensions());
        }
    }
}
