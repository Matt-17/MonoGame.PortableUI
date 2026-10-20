using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class ParticleLayerTests
    {
        private sealed class TestScreen : Screen
        {
        }

        [TestMethod]
        public void A_burst_flies_up_then_falls_under_gravity()
        {
            var layer = new ParticleLayer(new Random(1));
            // Straight up at 500 units/s against 1000 units/s² of gravity: the top is reached after 0.5 s, 125 units up.
            var burst = new ParticleBurst { Count = 5, Direction = -90, Spread = 0, MinSpeed = 500, MaxSpeed = 500, Drag = 0, Gravity = 1000, Lifetime = TimeSpan.FromSeconds(5) };
            layer.Burst(new PointF(100, 300), burst);
            Assert.AreEqual(5, layer.ActiveParticles);

            for (var i = 0; i < 50; i++)
                layer.Step(0.01f);
            Assert.AreEqual(175, layer.PositionsForTests().Average(p => p.Y), 0.5, "at the top");

            for (var i = 0; i < 50; i++)
                layer.Step(0.01f);
            Assert.AreEqual(300, layer.PositionsForTests().Average(p => p.Y), 0.5, "back down where it started");
            Assert.IsTrue(layer.PositionsForTests().All(p => Math.Abs(p.X - 100) < 0.01f), "no spread: straight up and down");
        }

        [TestMethod]
        public void Particles_expire()
        {
            var layer = new ParticleLayer(new Random(1));
            layer.Burst(new PointF(0, 0), new ParticleBurst { Count = 10, Lifetime = TimeSpan.FromSeconds(1) });

            for (var i = 0; i < 20; i++)
                layer.Step(0.06f);

            Assert.AreEqual(0, layer.ActiveParticles);
        }

        [TestMethod]
        public void Burst_on_a_control_adds_one_layer_and_lets_touches_through()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(300, 300);
            var button = new Button { Text = "Go", Width = 100, Height = 50, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var screen = new TestScreen { Content = button };
            engine.NavigateToScreen(screen);
            screen.PerformLayoutIfDirty();

            button.Burst();
            button.Burst(ParticleBurst.Sparks);

            Assert.AreEqual(1, screen.Adorners.OfType<ParticleLayer>().Count());
            var layer = screen.Particles();
            Assert.AreEqual(ParticleBurst.Confetti.Count + ParticleBurst.Sparks.Count, layer.ActiveParticles);
            Assert.IsFalse(layer.HitTest(new PointF(20, 20)));
        }
    }
}
