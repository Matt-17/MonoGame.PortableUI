using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;
using MonoGame.PortableUI.Text;

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

        /// <summary>Draws nothing: lets a TextBlock run its draw path without a device.</summary>
        private sealed class NullFont : UIFont
        {
            public override float DefaultSize => 16;
            public override float GetLineHeight(float pixelSize) => pixelSize;
            public override Vector2 MeasureString(string text, float pixelSize) => new Vector2(text.Length * 8, pixelSize);
            public override Vector2 MeasureString(StringBuilder text, float pixelSize) => new Vector2(text.Length * 8, pixelSize);
            public override void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float pixelSize, Vector2 scale)
            {
            }
            public override void DrawString(SpriteBatch spriteBatch, StringBuilder text, Vector2 position, Color color, float pixelSize, Vector2 scale)
            {
            }
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
        public void Same_stroke_or_glow_instance_does_not_invalidate()
        {
            var (host, text) = Host(new TextBlock { Text = "Hi" });
            var stroke = new TextStroke(Color.Black, 2);
            var glow = new TextGlow(Color.White, 4);

            text.Stroke = stroke;
            text.Glow = glow;
            Assert.AreEqual(2, host.Visual);
            text.Stroke = stroke;
            text.Glow = glow;
            Assert.AreEqual(2, host.Visual, "a host syncing the same effects every frame costs nothing");
            text.Stroke = null;
            Assert.AreEqual(3, host.Visual);
        }

        [TestMethod]
        public void Shadow_properties_redraw_on_change_only()
        {
            var (host, text) = Host(new TextBlock { Text = "Hi" });
            text.ShadowColor = Color.Black;
            text.ShadowOffset = new Vector2(2, 2);
            text.ShadowBlur = 0;
            Assert.AreEqual(3, host.Visual);
            text.ShadowColor = Color.Black;
            text.ShadowOffset = new Vector2(2, 2);
            text.ShadowBlur = 0;
            Assert.AreEqual(3, host.Visual);
            Assert.AreEqual(0, host.Layout, "the shadow is ink: no layout pass");
        }

        [TestMethod]
        public void Same_ink_overflow_does_not_invalidate()
        {
            var (host, child) = Host(new Border());
            child.InkOverflow = new Thickness(3);
            child.InkOverflow = new Thickness(3);
            Assert.AreEqual(1, host.Visual);
        }

        [TestMethod]
        public void Changing_a_shown_stroke_or_glow_redraws_the_engine_that_drew_it()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, RenderMode = RenderMode.OnDemand });
            // Invisible at first: making it visible later must still show.
            var stroke = new TextStroke(Color.Black, 0);
            var glow = new TextGlow(Color.Transparent, 4);
            var text = new TextBlock { Text = "Hi", DynamicFont = new NullFont(), Stroke = stroke, Glow = glow };
            engine.ConsumeRedrawRequest();
            stroke.Width = 1; // never drawn: nobody shows it
            Assert.IsFalse(engine.ConsumeRedrawRequest(), "not drawn yet");

            engine.DrawAs(() => text.OnDraw(null!, new Rect(0, 0, 40, 16)));
            engine.ConsumeRedrawRequest();
            var generation = engine.LayerCacheGeneration;

            stroke.Width = 2;
            Assert.IsTrue(engine.ConsumeRedrawRequest(), "stroke width");
            Assert.AreNotEqual(generation, engine.LayerCacheGeneration, "cached layers holding the text re-render");
            glow.Color = Color.White;
            Assert.IsTrue(engine.ConsumeRedrawRequest(), "glow colour");

            stroke.Width = 2;
            glow.Color = Color.White;
            Assert.IsFalse(engine.ConsumeRedrawRequest(), "unchanged: nothing to draw");
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
