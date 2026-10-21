using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class RoundedMaskTests
    {
        // The straightforward definition: per pixel, coverage by the distance to the corner's arc.
        private static byte Reference(int x, int y, int width, int height, int tl, int tr, int br, int bl)
        {
            var px = x + 0.5f;
            var py = y + 0.5f;
            float Corner(float cx, float cy, int r) => r <= 0 ? 1 : MathHelper.Clamp(r + 0.5f - Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy)), 0, 1);
            float coverage = 1;
            if (px < tl && py < tl)
                coverage = Corner(tl, tl, tl);
            else if (px >= width - tr && py < tr)
                coverage = Corner(width - tr, tr, tr);
            else if (px >= width - br && py >= height - br)
                coverage = Corner(width - br, height - br, br);
            else if (px < bl && py >= height - bl)
                coverage = Corner(bl, height - bl, bl);
            return (byte)(coverage * 255);
        }

        [DataTestMethod]
        [DataRow(12, 12, 12, 12)]
        [DataRow(137, 137, 137, 137)]
        [DataRow(20, 6, 0, 33)]
        [DataRow(1, 1, 1, 1)]
        public void Nine_slice_mask_matches_the_per_pixel_definition(int tl, int tr, int br, int bl)
        {
            var pixels = RoundedRectRenderer.NineSliceMaskPixels(tl, tr, br, bl, out var width, out var height);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var expected = Reference(x, y, width, height, tl, tr, br, bl);
                    var actual = pixels[y * width + x] & 0xFF;
                    Assert.IsTrue(Math.Abs(expected - (int)actual) <= 1, $"({x}, {y}): {actual} instead of {expected}");
                    Assert.AreEqual(actual * 0x01010101u, pixels[y * width + x], "premultiplied white");
                }
            }
        }
    }
}
