using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class SurfacePointerCaptureTests
    {
        private sealed class EmptyScreen : Screen
        {
        }

        [TestMethod]
        public void Captured_pointer_moves_relatively_stays_on_the_curved_picture_and_escape_releases()
        {
            using var game = new Game();
            using var surface = new UISurface(game, new EmptyScreen(), 400, 300);
            surface.PostEffects = new PostEffect[] { new CrtBarrelPostEffect { Distortion = 0.1f } };
            var input = new VirtualInputSource();
            var capture = new SurfacePointerCapture(surface, input);
            var released = 0;
            capture.Released += (_, _) => released++;

            capture.Capture(new PointF(200, 150));
            Assert.IsTrue(capture.IsCaptured);
            Assert.IsTrue(surface.ShowSoftwareCursor);

            Assert.IsTrue(capture.Update(new Vector2(5, -5), false, false, false, new KeyboardState()));
            Assert.AreEqual(new PointF(205, 145), capture.Position, "5 px of mouse = 5 surface px");
            Assert.AreEqual(capture.Position, input.MousePosition);

            for (var i = 0; i < 50; i++)
                capture.Update(new Vector2(-40, -40), false, false, false, new KeyboardState());
            Assert.IsTrue(surface.IsPointOnDisplay(capture.Position), "pushed into the corner it stays on the picture");
            Assert.IsTrue(capture.Position.X > 0 && capture.Position.Y > 0, "not in the dark rim of the curve");

            Assert.IsFalse(capture.Update(Vector2.Zero, false, false, false, new KeyboardState(Keys.Escape)));
            Assert.IsFalse(capture.IsCaptured);
            Assert.AreEqual(1, released);
            Assert.IsFalse(surface.ShowSoftwareCursor);
        }

        [TestMethod]
        public void Escape_still_held_while_capturing_does_not_release_at_once()
        {
            using var game = new Game();
            using var surface = new UISurface(game, new EmptyScreen(), 400, 300);
            var capture = new SurfacePointerCapture(surface, new VirtualInputSource());

            capture.Capture(new PointF(10, 10));
            Assert.IsTrue(capture.Update(Vector2.Zero, false, false, false, new KeyboardState(Keys.Escape)), "held from before");
            capture.Update(Vector2.Zero, false, false, false, new KeyboardState());
            Assert.IsFalse(capture.Update(Vector2.Zero, false, false, false, new KeyboardState(Keys.Escape)), "a new press releases");
        }
    }
}
