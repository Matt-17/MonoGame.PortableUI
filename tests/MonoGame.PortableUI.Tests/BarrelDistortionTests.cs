using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Effects;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class BarrelDistortionTests
    {
        /// <summary>Replicates the forward mapping of PostProcessManager.DrawBarrel.</summary>
        private static PointF ForwardBarrel(PointF uiPoint, Rect rect, float distortion)
        {
            var ndcX = (uiPoint.X - rect.Left) / rect.Width * 2 - 1;
            var ndcY = (uiPoint.Y - rect.Top) / rect.Height * 2 - 1;
            var r2 = (ndcX * ndcX + ndcY * ndcY) / 2f;
            var scale = 1 - distortion * r2;
            return new PointF(
                rect.Left + (ndcX * scale * 0.5f + 0.5f) * rect.Width,
                rect.Top + (ndcY * scale * 0.5f + 0.5f) * rect.Height);
        }

        [TestMethod]
        [DataRow(0.05f)]
        [DataRow(0.08f)]
        [DataRow(0.15f)]
        // The forward map folds beyond d = 1/3, so 0.3 is the upper end of the invertible range.
        [DataRow(0.3f)]
        public void Inverse_barrel_round_trips_within_a_twentieth_pixel_full_screen(float distortion)
        {
            AssertRoundTrip(new Rect(0, 0, 1180, 760), distortion);
        }

        [TestMethod]
        [DataRow(0.06f)]
        [DataRow(0.2f)]
        public void Inverse_barrel_round_trips_for_offset_island_rect(float distortion)
        {
            AssertRoundTrip(new Rect(340, 120, 512, 400), distortion);
        }

        [TestMethod]
        public void Inverse_barrel_is_identity_without_distortion()
        {
            var point = new PointF(123.4f, 567.8f);
            var mapped = PostProcessManager.InverseBarrel(point, new Rect(0, 0, 800, 600), 0);
            Assert.AreEqual(point.X, mapped.X, 0.0001f);
            Assert.AreEqual(point.Y, mapped.Y, 0.0001f);
        }

        private static void AssertRoundTrip(Rect rect, float distortion)
        {
            for (var y = 0; y <= 10; y++)
            {
                for (var x = 0; x <= 10; x++)
                {
                    var uiPoint = new PointF(
                        rect.Left + rect.Width * x / 10f,
                        rect.Top + rect.Height * y / 10f);
                    var displayed = ForwardBarrel(uiPoint, rect, distortion);
                    var recovered = PostProcessManager.InverseBarrel(displayed, rect, distortion);
                    Assert.AreEqual(uiPoint.X, recovered.X, 0.05f, $"X at grid ({x},{y}) d={distortion}");
                    Assert.AreEqual(uiPoint.Y, recovered.Y, 0.05f, $"Y at grid ({x},{y}) d={distortion}");
                }
            }
        }
    }
}
