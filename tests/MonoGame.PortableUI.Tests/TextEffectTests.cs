using System.Collections.Generic;
using System.Linq;
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
    [TestClass]
    public class TextEffectTests
    {
        /// <summary>Records every glyph run instead of drawing it.</summary>
        private sealed class RecordingFont : UIFont
        {
            public readonly List<(Vector2 Position, Color Color)> Draws = new List<(Vector2, Color)>();
            public Thickness Ink { get; set; }
            public override float DefaultSize => 16;
            public override float GetLineHeight(float pixelSize) => pixelSize;
            public override Vector2 MeasureString(string text, float pixelSize) => new Vector2(text.Length * 8, pixelSize);
            public override Vector2 MeasureString(StringBuilder text, float pixelSize) => new Vector2(text.Length * 8, pixelSize);
            public override void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float pixelSize, Vector2 scale)
                => Draws.Add((position, color));
            public override void DrawString(SpriteBatch spriteBatch, StringBuilder text, Vector2 position, Color color, float pixelSize, Vector2 scale)
                => Draws.Add((position, color));
            public override Thickness GetInkOverflow(float pixelSize) => Ink;
        }

        private static (TextBlock Text, RecordingFont Font) Block()
        {
            var font = new RecordingFont();
            var text = new TextBlock { Text = "2,000", DynamicFont = font, TextColor = Color.White };
            return (text, font);
        }

        private static Vector2 Draw(TextBlock text, RecordingFont font)
        {
            font.Draws.Clear();
            text.OnDraw(null!, new Rect(0, 0, 40, 16));
            // The fill is the last run, at the text origin.
            return font.Draws[^1].Position;
        }

        [TestMethod]
        public void Stroke_stamps_a_round_ring_of_outline_copies_before_the_fill()
        {
            var (text, font) = Block();
            text.Stroke = new TextStroke(Color.Black, 3f);
            var origin = Draw(text, font);

            var outline = font.Draws.Take(font.Draws.Count - 1).ToList();
            Assert.IsTrue(outline.Count >= 16, "two rings with at least eight stamps each");
            Assert.IsTrue(outline.All(d => d.Color == Color.Black));
            var radii = outline.Select(d => Vector2.Distance(d.Position, origin)).ToList();
            Assert.AreEqual(3f, radii.Max(), 0.01f);
            Assert.IsTrue(radii.All(r => r <= 3.01f && r > 0.5f));
            Assert.AreEqual(Color.White, font.Draws[^1].Color);
        }

        [TestMethod]
        public void Render_opacity_and_scale_apply_to_the_stroke()
        {
            var (text, font) = Block();
            text.Stroke = new TextStroke(Color.Black, 1.5f);
            text.SetRenderState(0.5f, new Vector2(2, 2));
            var origin = Draw(text, font);

            var outline = font.Draws.Take(font.Draws.Count - 1).ToList();
            Assert.IsTrue(outline.All(d => d.Color.A == 127 || d.Color.A == 128));
            Assert.AreEqual(3f, outline.Max(d => Vector2.Distance(d.Position, origin)), 0.01f);
        }

        [TestMethod]
        public void Glow_reaches_its_radius_and_adds_up_to_its_alpha()
        {
            var (text, font) = Block();
            text.Glow = new TextGlow(Color.Yellow.WithAlpha(0.45f), 6f);
            var origin = Draw(text, font);

            var glow = font.Draws.Take(font.Draws.Count - 1).ToList();
            Assert.AreEqual(24, glow.Count);
            Assert.AreEqual(6f, glow.Max(d => Vector2.Distance(d.Position, origin)), 0.01f);
            // 24 overlapping stamps composite to roughly the glow alpha.
            var perStamp = glow[0].Color.A / 255f;
            var combined = 1 - System.MathF.Pow(1 - perStamp, 24);
            Assert.AreEqual(0.45f, combined, 0.08f);
        }

        [TestMethod]
        public void Theme_typography_supplies_default_stroke_and_null_switches_it_off()
        {
            var theme = PortableTheme.CreateDefault();
            theme.Typography.TextStroke = new TextStroke(Color.Red, 2f);
            var (text, font) = Block();
            var island = new ThemeIsland { Theme = theme, Content = text };

            Assert.AreSame(theme.Typography.TextStroke, text.Stroke);
            Draw(text, font);
            Assert.IsTrue(font.Draws.Count > 1);

            text.Stroke = null;
            Draw(text, font);
            Assert.AreEqual(1, font.Draws.Count);
            Assert.IsNotNull(island);
        }

        [TestMethod]
        public void Sharp_translucent_shadow_is_one_copy()
        {
            var (text, font) = Block();
            text.ShadowColor = Color.Black.WithAlpha(0.5f);
            text.ShadowBlur = 0;
            Draw(text, font);
            Assert.AreEqual(2, font.Draws.Count);
        }

        [TestMethod]
        public void Ink_overflow_covers_font_ink_stroke_glow_and_shadow()
        {
            var (text, font) = Block();
            Assert.AreEqual(0f, text.GetInkOverflow().Bottom);

            font.Ink = new Thickness(0, 0, 0, 4);
            Assert.AreEqual(4f, text.GetInkOverflow().Bottom);

            text.Stroke = new TextStroke(Color.Black, 2f);
            text.Glow = new TextGlow(Color.White, 3f);
            var ink = text.GetInkOverflow();
            Assert.AreEqual(5f, ink.Left);
            Assert.AreEqual(9f, ink.Bottom);

            text.Glow = null;
            text.Stroke = null;
            text.ShadowColor = Color.Black;
            text.ShadowOffset = new Vector2(0, 3);
            text.ShadowBlur = 2;
            ink = text.GetInkOverflow();
            Assert.AreEqual(4f + 5f, ink.Bottom);
            Assert.AreEqual(2f, ink.Left);

            text.InkOverflow = new Thickness(20);
            Assert.AreEqual(20f, text.GetInkOverflow().Left);
        }

        [TestMethod]
        public void TextBox_ignores_text_effects_for_its_clip()
        {
            var box = new TextBox { Text = "abc", Stroke = new TextStroke(Color.Black, 4f) };
            Assert.AreEqual(0f, box.GetInkOverflow().Left);
        }
    }
}
