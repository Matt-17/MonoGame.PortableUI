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
    }
}
