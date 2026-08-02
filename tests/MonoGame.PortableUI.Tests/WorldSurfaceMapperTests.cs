using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class WorldSurfaceMapperTests
    {
        [TestMethod]
        public void Ray_mapping_hits_centered_quad_and_scales_to_ui_coordinates()
        {
            var ray = new Ray(new Vector3(0, 0, 10), new Vector3(0, 0, -1));

            var mapped = WorldSurfaceMapper.TryMapRayToSurface(ray, Matrix.Identity, new Vector2(2, 2), 200, 100, out var uiPoint);

            Assert.IsTrue(mapped);
            AssertPoint(new PointF(100, 50), uiPoint);
        }

        [TestMethod]
        public void Ray_mapping_rejects_hits_outside_quad()
        {
            var ray = new Ray(new Vector3(2, 0, 10), new Vector3(0, 0, -1));

            var mapped = WorldSurfaceMapper.TryMapRayToSurface(ray, Matrix.Identity, new Vector2(2, 2), 200, 100, out _);

            Assert.IsFalse(mapped);
        }

        [TestMethod]
        public void Point_mapping_uses_inverse_sprite_transform()
        {
            var transform = Matrix.CreateTranslation(10, 20, 0);

            var mapped = WorldSurfaceMapper.TryMapPointToSurface(new PointF(60, 45), transform, new Vector2(100, 50), 200, 100, out var uiPoint);

            Assert.IsTrue(mapped);
            AssertPoint(new PointF(100, 50), uiPoint);
        }

        private static void AssertPoint(PointF expected, PointF actual)
        {
            Assert.AreEqual(expected.X, actual.X, 0.001f);
            Assert.AreEqual(expected.Y, actual.Y, 0.001f);
        }
    }
}
