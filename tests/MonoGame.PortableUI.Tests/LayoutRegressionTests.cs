using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class LayoutRegressionTests
    {
        [TestMethod]
        public void Grid_handles_auto_star_and_absolute_definitions()
        {
            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition()
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(80) },
                    new ColumnDefinition()
                }
            };
            var first = Text("first", 50, 20);
            var second = Text("second", 50, 40);
            grid.AddChild(first);
            grid.AddChild(second, row: 1, column: 1);

            grid.UpdateLayout(new Rect(0, 0, 280, 180));

            Assert.AreEqual(80, first.BoundingRect.Width);
            Assert.AreEqual(20, first.BoundingRect.Height);
            Assert.AreEqual(200, second.BoundingRect.Width);
            Assert.AreEqual(160, second.BoundingRect.Height);
        }

        [TestMethod]
        public void Grid_without_explicit_definitions_stretches_children_to_available_axis()
        {
            var screenGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition()
                }
            };
            var contentGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(320) },
                    new ColumnDefinition()
                }
            };
            var side = new Border();
            var main = new Border();
            contentGrid.AddChild(side);
            contentGrid.AddChild(main, column: 1);
            screenGrid.AddChild(contentGrid);

            screenGrid.UpdateLayout(new Rect(0, 0, 800, 480));

            Assert.AreEqual(800, contentGrid.BoundingRect.Width);
            Assert.AreEqual(480, contentGrid.BoundingRect.Height);
            Assert.AreEqual(320, side.BoundingRect.Width);
            Assert.AreEqual(480, side.BoundingRect.Height);
            Assert.AreEqual(480, main.BoundingRect.Width);
            Assert.AreEqual(480, main.BoundingRect.Height);
        }

        [TestMethod]
        public void Grid_auto_definitions_do_not_force_non_auto_tracks_for_spanning_children()
        {
            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition(),
                    new RowDefinition { Height = new GridLength(40) }
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition(),
                    new ColumnDefinition { Width = new GridLength(260) }
                }
            };
            var auto = new FixedSizeControl(new Size(120, 42));
            var star = new FixedSizeControl(new Size(160, 42));
            var fixedSize = new FixedSizeControl(new Size(160, 42));
            var spanningPreview = new FixedSizeControl(new Size(900, 120));
            var spanningFooter = new FixedSizeControl(new Size(500, 20));
            grid.AddChild(auto);
            grid.AddChild(star, column: 1);
            grid.AddChild(fixedSize, column: 2);
            grid.AddChild(spanningPreview, row: 1, columnSpan: 3);
            grid.AddChild(spanningFooter, row: 2, columnSpan: 3);

            grid.UpdateLayout(new Rect(0, 0, 1000, 300));

            Assert.AreEqual(640, auto.BoundingRect.Width);
            Assert.AreEqual(100, star.BoundingRect.Width);
            Assert.AreEqual(260, fixedSize.BoundingRect.Width);
            Assert.AreEqual(42, auto.BoundingRect.Height);
            Assert.AreEqual(218, spanningPreview.BoundingRect.Height);
            Assert.AreEqual(40, spanningFooter.BoundingRect.Height);
        }

        [TestMethod]
        public void Grid_auto_columns_share_spanning_child_width()
        {
            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(100) }
                }
            };
            var first = new FixedSizeControl(new Size(1, 10));
            var second = new FixedSizeControl(new Size(1, 10));
            var fixedColumn = new FixedSizeControl(new Size(100, 10));
            var spanning = new FixedSizeControl(new Size(300, 10));
            grid.AddChild(first);
            grid.AddChild(second, column: 1);
            grid.AddChild(fixedColumn, column: 2);
            grid.AddChild(spanning, columnSpan: 2);

            grid.UpdateLayout(new Rect(0, 0, 400, 40));

            Assert.AreEqual(150, first.BoundingRect.Width);
            Assert.AreEqual(150, second.BoundingRect.Width);
            Assert.AreEqual(100, fixedColumn.BoundingRect.Width);
            Assert.AreEqual(300, spanning.BoundingRect.Width);
        }

        [TestMethod]
        public void Grid_auto_rows_share_spanning_child_height()
        {
            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(50) }
                }
            };
            var first = new FixedSizeControl(new Size(10, 1));
            var second = new FixedSizeControl(new Size(10, 1));
            var fixedRow = new FixedSizeControl(new Size(10, 50));
            var spanning = new FixedSizeControl(new Size(10, 120));
            grid.AddChild(first);
            grid.AddChild(second, row: 1);
            grid.AddChild(fixedRow, row: 2);
            grid.AddChild(spanning, rowSpan: 2);

            grid.UpdateLayout(new Rect(0, 0, 100, 170));

            Assert.AreEqual(60, first.BoundingRect.Height);
            Assert.AreEqual(60, second.BoundingRect.Height);
            Assert.AreEqual(50, fixedRow.BoundingRect.Height);
            Assert.AreEqual(120, spanning.BoundingRect.Height);
        }

        [TestMethod]
        public void Content_control_measurement_includes_padding_and_margin()
        {
            var border = new Border
            {
                Margin = new Thickness(0, 0, 12, 0),
                Padding = new Thickness(12, 10, 14, 10),
                Content = new FixedSizeControl(new Size(100, 20))
            };

            var size = border.MeasureLayout();

            Assert.AreEqual(138, size.Width);
            Assert.AreEqual(40, size.Height);
        }

        [TestMethod]
        public void Fixed_height_content_control_preserves_margin_outside_clipping_rect()
        {
            var button = new Button
            {
                Text = "Comfortable density",
                Height = 38,
                Margin = new Thickness(0, 6, 12, 14)
            };

            button.UpdateLayout(new Rect(0, 0, 220, Size.Infinity));

            Assert.AreEqual(58, button.BoundingRect.Height);
            Assert.AreEqual(38, button.ClippingRect.Height);
        }

        [TestMethod]
        public void Empty_stack_panel_is_measurable()
        {
            var panel = new StackPanel();

            var size = panel.MeasureLayout();

            Assert.AreEqual(Size.Empty, size);
        }

        [TestMethod]
        public void Control_constraints_apply_to_derived_measurements()
        {
            var text = Text("short", 30, 12);
            text.MinWidth = 120;
            text.MaxHeight = 20;

            var size = text.MeasureLayout();

            Assert.AreEqual(120, size.Width);
            Assert.AreEqual(12, size.Height);
        }

        private static TextBlock Text(string value, float width, float height)
        {
            return new TextBlock
            {
                Text = value,
                TextMeasurer = new FixedTextMeasurer(width, height)
            };
        }

        private sealed class FixedTextMeasurer : ITextMeasurer
        {
            private readonly float _width;
            private readonly float _height;

            public FixedTextMeasurer(float width, float height)
            {
                _width = width;
                _height = height;
            }

            public Microsoft.Xna.Framework.Vector2 MeasureString(string text)
            {
                return new Microsoft.Xna.Framework.Vector2(_width, _height);
            }
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
