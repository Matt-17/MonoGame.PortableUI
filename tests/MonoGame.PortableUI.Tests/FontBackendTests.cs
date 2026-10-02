using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.FontStashSharp;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class FontBackendTests
    {
        private static FontStashUIFont? _font;

        private static FontStashUIFont Font => _font ??= FontStashUIFont.FromFiles(16, Path.Combine(AppContext.BaseDirectory, "Fonts", "AtkinsonHyperlegible-Regular.ttf"));

        [TestCleanup]
        public void Cleanup()
        {
            FontManager.DefaultDynamicFont = null;
            TextScaling.Reset();
        }

        /// <summary>
        ///     A SpriteFont "baked" at 32 px from the same TTF (advances only, no texture): the
        ///     SpriteFont path scaled to another size must agree with FontStashSharp rasterizing there.
        /// </summary>
        private static SpriteFont BakeSpriteFont(int bakedSize)
        {
            var baked = Font.GetFont(bakedSize);
            var bounds = new List<Rectangle>();
            var cropping = new List<Rectangle>();
            var characters = new List<char>();
            var kerning = new List<Vector3>();
            var lineHeight = (int)MathF.Round(baked.LineHeight);
            for (var c = ' '; c <= '~'; c++)
            {
                var advance = Font.MeasureString(new string(c, 10), bakedSize).X / 10f;
                characters.Add(c);
                bounds.Add(new Rectangle(0, 0, (int)MathF.Ceiling(advance), lineHeight));
                cropping.Add(Rectangle.Empty);
                kerning.Add(new Vector3(0, advance, 0));
            }
            return new SpriteFont(null!, bounds, cropping, characters, lineHeight, 0, kerning, '?');
        }

        [DataTestMethod]
        [DataRow(12f)]
        [DataRow(20f)]
        [DataRow(48f)]
        public void Sprite_font_and_font_stash_backends_measure_alike(float size)
        {
            var spriteFont = new SpriteFontUIFont(BakeSpriteFont(32), 32);
            const string text = "The quick brown fox jumps over 13 lazy dogs.";

            var viaSprite = spriteFont.MeasureString(text, size);
            var viaStash = Font.MeasureString(text, size);

            Assert.AreEqual(viaStash.X, viaSprite.X, viaStash.X * 0.05f, "width within 5 %");
            Assert.AreEqual(Font.GetLineHeight(size), spriteFont.GetLineHeight(size), size * 0.1f);
        }

        [TestMethod]
        public void Text_block_measures_with_the_dynamic_font_at_its_text_size()
        {
            var small = new TextBlock { DynamicFont = Font, TextSize = 16, Text = "Scaled text" };
            var large = new TextBlock { DynamicFont = Font, TextSize = 32, Text = "Scaled text" };

            var ratio = large.MeasureLayout().Width / small.MeasureLayout().Width;
            Assert.AreEqual(2f, ratio, 0.05f);
        }

        [TestMethod]
        public void Characters_outside_any_baked_range_measure_instead_of_failing()
        {
            var block = new TextBlock { DynamicFont = Font, Text = "Zoë, Łódź and naïve café" };
            Assert.IsTrue(block.MeasureLayout().Width > 0);
        }

        [TestMethod]
        public void Wrapping_uses_the_dynamic_font_measurements()
        {
            var block = new TextBlock
            {
                DynamicFont = Font,
                TextSize = 16,
                Width = 120,
                TextWrapping = TextWrapping.Wrap,
                Text = "Words wrap at the width of the block when measured by FontStashSharp"
            };
            var size = block.MeasureLayout();
            var oneLine = Font.GetLineHeight(16);

            Assert.IsTrue(size.Height >= oneLine * 3, $"wrapped onto several lines ({size.Height})");
            Assert.AreEqual(120, size.Width);
        }

        [TestMethod]
        public void Default_dynamic_font_applies_to_blocks_without_an_override_and_follows_text_scaling()
        {
            var block = new TextBlock { TextSize = 16, Text = "Hello" };
            FontManager.DefaultDynamicFont = Font;
            block.OnTextScaleChanged();
            var at1 = block.MeasureLayout().Width;
            Assert.AreEqual(Font.MeasureString("Hello", 16).X, at1, 0.01f);

            TextScaling.AppScale = 1.5f;
            block.OnTextScaleChanged();
            Assert.AreEqual(Font.MeasureString("Hello", 24).X, block.MeasureLayout().Width, 0.01f);
        }

        [TestMethod]
        public void Nearby_sizes_share_one_atlas_entry()
        {
            var font = FontStashUIFont.FromFiles(16, Path.Combine(AppContext.BaseDirectory, "Fonts", "AtkinsonHyperlegible-Regular.ttf"));
            for (var i = 0; i < 20; i++)
                font.MeasureString("x", 16f + i * 0.005f); // 16.0 .. 16.095 rounds to the 16 px step
            Assert.AreEqual(1, font.CachedSizeCount);
            font.Dispose();
        }
    }
}
