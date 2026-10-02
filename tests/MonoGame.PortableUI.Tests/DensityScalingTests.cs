using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class DensityScalingTests
    {
        [TestCleanup]
        public void Cleanup() => TextScaling.Reset();

        [TestMethod]
        public void Layout_scale_lays_out_in_density_independent_units()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, LayoutScale = 2.75f });

            Assert.IsTrue(engine.ApplyViewportSize(1080, 2200));
            Assert.AreEqual(1080 / 2.75f, engine.ScreenRect.Width, 0.01f);
            Assert.AreEqual(2.75f, engine.RenderScale);
            Assert.IsTrue(engine.ScalesNatively, "drawn through the transform, not a scaled render target");
            Assert.AreEqual(new Rect(275, 550, 110, 55), engine.LayoutToWindow(new Rect(100, 200, 40, 20)));
        }

        [TestMethod]
        public void Reference_size_still_uses_the_render_target_path()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, ReferenceSize = new PointF(400, 300), LayoutScale = 2 });

            engine.ApplyViewportSize(800, 600);
            Assert.AreEqual(400, engine.ScreenRect.Width);
            Assert.IsFalse(engine.ScalesNatively);
        }

        [TestMethod]
        public void List_rows_grow_with_scaled_text_but_keep_item_height_as_minimum()
        {
            var list = new ListBox { ItemHeight = 20 };
            list.Items.Add("Apple");
            list.UpdateLayout(new Rect(0, 0, 200, 400));
            var normal = list.ItemButtons[0].ClippingRect.Height;
            Assert.IsTrue(normal >= 20);

            TextScaling.AppScale = 2f;
            var scaled = new ListBox { ItemHeight = 20 };
            scaled.Items.Add("Apple");
            scaled.UpdateLayout(new Rect(0, 0, 200, 400));

            Assert.IsTrue(scaled.ItemButtons[0].ClippingRect.Height > normal, "rows fit twice-as-large text");
        }

        [TestMethod]
        public void Data_grid_rows_are_at_least_one_text_line_tall()
        {
            var grid = new DataGrid { RowHeight = 10, HeaderHeight = 10 };
            Assert.IsTrue(grid.EffectiveRowHeight > 10);
            Assert.IsTrue(grid.EffectiveHeaderHeight > 10);

            var tall = new DataGrid { RowHeight = 200, HeaderHeight = 0 };
            Assert.AreEqual(200, tall.EffectiveRowHeight, "a larger RowHeight is kept");
            Assert.AreEqual(0, tall.EffectiveHeaderHeight, "a hidden header stays hidden");
        }

        [TestMethod]
        public void Single_line_text_box_grows_only_from_the_theme_height()
        {
            TextScaling.AppScale = 2f;
            var themed = new TextBox { Text = "Hello" };
            var explicitHeight = new TextBox { Text = "Hello", Height = 12 };

            Assert.IsTrue(themed.MeasureLayout().Height > PortableTheme.ResolveCurrent().TextBoxHeight);
            Assert.AreEqual(12, explicitHeight.MeasureLayout().Height, "an explicit height is respected");
        }
    }
}
