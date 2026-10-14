using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>Control.TransformOrigin: the pivot of Scale within the control's box.</summary>
    [TestClass]
    public class TransformOriginTests
    {
        // Box 100x50 at (10, 20).
        private static Border CreateLaidOut()
        {
            var border = new Border
            {
                Width = 100,
                Height = 50,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            border.UpdateLayout(new Rect(10, 20, 300, 300));
            return border;
        }

        private static Vector2 Map(Control control, float x, float y)
            => Vector2.Transform(new Vector2(x, y), control.GetRenderTransform());

        private static void AssertPoint(Vector2 expected, Vector2 actual, string message)
        {
            Assert.AreEqual(expected.X, actual.X, 0.001f, message + " (x)");
            Assert.AreEqual(expected.Y, actual.Y, 0.001f, message + " (y)");
        }

        [TestMethod]
        public void Default_origin_is_the_centre()
        {
            var border = CreateLaidOut();
            Assert.AreEqual(new Vector2(0.5f, 0.5f), border.TransformOrigin);

            border.Scale = new Vector2(2, 2);
            AssertPoint(new Vector2(60, 45), Map(border, 60, 45), "centre stays");
            AssertPoint(new Vector2(-40, -5), Map(border, 10, 20), "top-left moves out");
        }

        [TestMethod]
        public void Top_left_origin_keeps_the_top_left_corner_in_place()
        {
            var border = CreateLaidOut();
            border.TransformOrigin = Vector2.Zero;
            border.Scale = new Vector2(2, 3);

            AssertPoint(new Vector2(10, 20), Map(border, 10, 20), "pivot");
            AssertPoint(new Vector2(210, 170), Map(border, 110, 70), "bottom-right");
        }

        [TestMethod]
        public void Bottom_origin_squashes_towards_the_bottom_edge()
        {
            var border = CreateLaidOut();
            border.TransformOrigin = new Vector2(0.5f, 1);
            border.Scale = new Vector2(1.2f, 0.5f);

            AssertPoint(new Vector2(60, 70), Map(border, 60, 70), "bottom centre stays");
            AssertPoint(new Vector2(60, 45), Map(border, 60, 20), "top edge comes down");
        }

        [TestMethod]
        public void Translation_applies_after_scaling_about_the_origin()
        {
            var border = CreateLaidOut();
            border.TransformOrigin = Vector2.Zero;
            border.Scale = new Vector2(2, 2);
            border.Translation = new Vector2(5, -3);

            AssertPoint(new Vector2(15, 17), Map(border, 10, 20), "pivot moved by translation");
        }

        [TestMethod]
        public void Origin_without_scale_or_translation_is_identity()
        {
            var border = CreateLaidOut();
            border.TransformOrigin = new Vector2(1, 1);
            Assert.AreEqual(Matrix.Identity, border.GetRenderTransform());
        }

        [TestMethod]
        public void Changing_the_origin_marks_a_cached_layer_dirty()
        {
            var child = new Border { Width = 20, Height = 20 };
            var cached = new Border { Content = child, CacheMode = CacheMode.Bitmap };
            var cache = new LayerCache { Dirty = false };
            cached.LayerCache = cache;

            child.TransformOrigin = Vector2.Zero;
            Assert.IsTrue(cache.Dirty);
        }
    }
}
