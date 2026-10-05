using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>#92: dynamic fonts resolve per theme, so surfaces with different themes keep their own font.</summary>
    [TestClass]
    [DoNotParallelize] // FontManager.DefaultDynamicFont and ScreenEngine.Instance are process-wide
    public class ThemeDynamicFontTests
    {
        /// <summary>Every character is <see cref="CharWidth"/> wide, so a measured width names the font.</summary>
        private sealed class FixedFont : UIFont
        {
            public FixedFont(float charWidth) => CharWidth = charWidth;
            public float CharWidth { get; }
            public override float DefaultSize => 16;
            public override float GetLineHeight(float pixelSize) => pixelSize;
            public override Vector2 MeasureString(string text, float pixelSize) => new Vector2(text.Length * CharWidth, pixelSize);
            public override Vector2 MeasureString(StringBuilder text, float pixelSize) => new Vector2(text.Length * CharWidth, pixelSize);
            public override void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float pixelSize, Vector2 scale) { }
            public override void DrawString(SpriteBatch spriteBatch, StringBuilder text, Vector2 position, Color color, float pixelSize, Vector2 scale) { }
        }

        private sealed class TestScreen : Screen
        {
        }

        private UIFont? _previousDefault;

        [TestInitialize]
        public void Remember() => _previousDefault = FontManager.DefaultDynamicFont;

        [TestCleanup]
        public void Restore() => FontManager.DefaultDynamicFont = _previousDefault;

        private static void Advance(UISurface surface, double seconds)
            => surface.Update(new GameTime(TimeSpan.FromSeconds(seconds), TimeSpan.FromMilliseconds(16)));

        [TestMethod]
        public void Two_surfaces_with_different_themes_measure_text_in_their_own_fonts()
        {
            using var game = new Game();
            FontManager.DefaultDynamicFont = new FixedFont(1);
            var dos = PortableTheme.CreateDefault();
            dos.Typography.DynamicFont = new FixedFont(8);
            var c64 = PortableTheme.CreateDefault();
            c64.Typography.DynamicFont = new FixedFont(10);
            var dosText = new TextBlock { Text = "READY" };
            var c64Text = new TextBlock { Text = "READY" };
            using var a = new UISurface(game, new TestScreen { Content = dosText }, 320, 200, dos);
            using var b = new UISurface(game, new TestScreen { Content = c64Text }, 320, 200, c64);
            Advance(a, 1);
            Advance(b, 1);

            Assert.AreEqual(40f, dosText.MeasureTextWidth("READY"));
            Assert.AreEqual(50f, c64Text.MeasureTextWidth("READY"));
        }

        [TestMethod]
        public void Without_a_theme_font_the_app_default_applies_and_a_control_font_wins()
        {
            using var game = new Game();
            FontManager.DefaultDynamicFont = new FixedFont(2);
            var theme = PortableTheme.CreateDefault();
            var plain = new TextBlock();
            var own = new TextBlock { DynamicFont = new FixedFont(5) };
            var panel = new StackPanel();
            panel.AddChild(plain);
            panel.AddChild(own);
            using var surface = new UISurface(game, new TestScreen { Content = panel }, 320, 200, theme);
            Advance(surface, 1);
            Assert.AreEqual(6f, plain.MeasureTextWidth("abc"), "app default");

            theme.Typography.DynamicFont = new FixedFont(3);
            Assert.AreEqual(9f, plain.MeasureTextWidth("abc"), "theme font");
            Assert.AreEqual(15f, own.MeasureTextWidth("abc"), "the control's own font wins");
        }

        [TestMethod]
        public void Switching_to_a_theme_with_another_font_remeasures_the_text()
        {
            using var game = new Game();
            var first = PortableTheme.CreateDefault();
            first.Typography.DynamicFont = new FixedFont(4);
            var second = PortableTheme.CreateDefault();
            second.Typography.DynamicFont = new FixedFont(6);
            var text = new TextBlock { Text = "abcd", HorizontalAlignment = HorizontalAlignment.Left };
            using var surface = new UISurface(game, new TestScreen { Content = text }, 320, 200, first);
            Advance(surface, 1);
            Assert.AreEqual(16f, text.MeasuredText.X);

            surface.Theme = second;
            Advance(surface, 2);

            Assert.AreEqual(24f, text.MeasuredText.X, "the cached measurement follows the new theme's font");
        }
    }
}
