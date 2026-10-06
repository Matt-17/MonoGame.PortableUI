using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>#125: setters that change what is drawn or laid out ask for it; unchanged values do nothing.</summary>
    [TestClass]
    [DoNotParallelize]
    public class ChangeNotificationTests
    {
        private sealed class CountingHost : ContentControl
        {
            public int Layout;
            public int Visual;

            public override void InvalidateLayout(bool boundsChanged)
            {
                if (boundsChanged)
                    Layout++;
                else
                    Visual++;
                base.InvalidateLayout(boundsChanged);
            }
        }

        /// <summary>A brush with the same change plumbing as the built-in ones, drawable without a device.</summary>
        private sealed class TestBrush : Brush
        {
            private Color _color;
            public Color Color { get => _color; set => SetProperty(ref _color, value); }
            public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Rect rect) => MarkDrawn();
        }

        private static (CountingHost Host, T Child) Host<T>(T child) where T : Control
        {
            var host = new CountingHost { Content = child };
            host.Layout = host.Visual = 0;
            return (host, child);
        }

        [TestMethod]
        public void Unchanged_visibility_does_not_invalidate()
        {
            var (host, child) = Host(new Border());
            child.IsVisible = true;
            child.IsGone = false;
            Assert.AreEqual(0, host.Layout + host.Visual);

            child.IsGone = true;
            Assert.AreEqual(1, host.Layout);
            child.IsGone = true;
            Assert.AreEqual(1, host.Layout);
            child.IsVisible = false;
            Assert.AreEqual(1, host.Visual);
        }

        [TestMethod]
        public void Margin_change_relays_out_and_same_margin_does_not()
        {
            var (host, child) = Host(new Border());
            child.Margin = new Thickness(4);
            Assert.AreEqual(1, host.Layout);
            child.Margin = new Thickness(4);
            Assert.AreEqual(1, host.Layout);
        }

        [TestMethod]
        public void Image_tint_and_sampler_redraw_and_stretch_relays_out()
        {
            var (host, image) = Host(new Image());
            image.TintColor = Color.Red;
            Assert.AreEqual(1, host.Visual);
            image.TintColor = Color.Red;
            Assert.AreEqual(1, host.Visual);
            image.SamplerState = Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp;
            Assert.AreEqual(2, host.Visual);
            image.Stretch = Stretch.Fill;
            Assert.AreEqual(1, host.Layout);
        }

        [TestMethod]
        public void Brush_change_redraws_the_engine_that_drew_it()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, RenderMode = RenderMode.OnDemand });
            var brush = new TestBrush { Color = Color.Red };
            engine.ConsumeRedrawRequest();

            brush.Color = Color.Blue; // never drawn: nobody shows it
            Assert.IsFalse(engine.ConsumeRedrawRequest());

            var generation = engine.LayerCacheGeneration;
            engine.DrawAs(() => brush.Draw(null!, Rect.Empty));
            brush.Color = Color.Green;
            Assert.IsTrue(engine.ConsumeRedrawRequest());
            Assert.AreNotEqual(generation, engine.LayerCacheGeneration, "cached layers holding the brush re-render");

            brush.Color = Color.Green;
            Assert.IsFalse(engine.ConsumeRedrawRequest(), "same colour: nothing to draw");
        }

        [TestMethod]
        public void Brush_drawn_outside_an_engine_redraws_every_engine_on_its_next_update()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, RenderMode = RenderMode.OnDemand });
            var brush = new TestBrush { Color = Color.Red };
            brush.Draw(null!, Rect.Empty); // a host's own batch
            engine.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(16)));
            engine.ConsumeRedrawRequest();

            brush.Color = Color.Blue;
            engine.Update(new GameTime(TimeSpan.FromSeconds(1.016), TimeSpan.FromMilliseconds(16)));
            Assert.IsTrue(engine.ConsumeRedrawRequest());
        }
    }
}
