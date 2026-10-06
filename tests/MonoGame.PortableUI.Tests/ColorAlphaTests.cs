using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ColorAlphaTests
    {
        [TestMethod]
        public void WithAlpha_keeps_rgb_and_sets_straight_alpha()
        {
            var c = Color.White.WithAlpha(0.07f);
            Assert.AreEqual(new Color(255, 255, 255, 18), c);
            Assert.AreEqual(new Color(10, 20, 30, 128), new Color(10, 20, 30).WithAlpha((byte)128));
        }

        [TestMethod]
        public void ToStraightAlpha_undoes_monogame_premultiplication()
        {
            Assert.AreEqual(Color.White.WithAlpha(0.5f).A, (Color.White * 0.5f).ToStraightAlpha().A, 1);
            Assert.AreEqual(255, (Color.White * 0.5f).ToStraightAlpha().R);
            var straight = (Color.White * 0.94f).ToStraightAlpha();
            Assert.AreEqual(Color.White.R, straight.R);
            Assert.AreEqual(Color.Transparent, Color.Transparent.ToStraightAlpha());
            var orange = (Color.Orange * 0.5f).ToStraightAlpha();
            Assert.AreEqual(Color.Orange.R, orange.R, 1);
            Assert.AreEqual(Color.Orange.G, orange.G, 1);
        }

        [TestMethod]
        public void Straight_alpha_brush_colour_draws_at_its_alpha()
        {
            // ApplyOpacity premultiplies: a straight 7 % white becomes ~18/255 in all channels.
            var drawn = Brush.ApplyOpacity(Color.White.WithAlpha(0.07f), 1f);
            Assert.AreEqual(18, drawn.A);
            Assert.AreEqual(18, drawn.R, 1);
        }
    }
}
