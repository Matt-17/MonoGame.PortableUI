using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class FractionalTextSizeTests
    {
        /// <summary>Measures one unit per character and the pixel size as height.</summary>
        private sealed class SizeFont : UIFont
        {
            public override float DefaultSize => 16;
            public override float GetLineHeight(float pixelSize) => pixelSize;
            public override Vector2 MeasureString(string text, float pixelSize) => new Vector2(text.Length, pixelSize);
            public override Vector2 MeasureString(StringBuilder text, float pixelSize) => new Vector2(text.Length, pixelSize);
            public override void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float pixelSize, Vector2 scale) { }
            public override void DrawString(SpriteBatch spriteBatch, StringBuilder text, Vector2 position, Color color, float pixelSize, Vector2 scale) { }
        }

        [TestMethod]
        public void Fractional_text_size_reaches_the_font()
        {
            var text = new TextBlock { Text = "GOOD NIGHT", DynamicFont = new SizeFont(), TextSize = 48.4f };
            Assert.AreEqual(48.4f, text.TextSize);
            Assert.AreEqual(48.4f, text.MeasuredText.Y, 0.0001f);
        }

        [TestMethod]
        public void Fractional_theme_sizes_follow_into_blocks()
        {
            var theme = PortableTheme.CreateDefault();
            theme.TextSize = 13.5f;
            theme.Typography.HeadingSize = 21.25f;
            var island = new ThemeIsland { Theme = PortableTheme.CreateDefault() };
            var body = new TextBlock { Text = "x" };
            var heading = new TextBlock { Text = "x", IsHeading = true };
            var panel = new StackPanel();
            panel.AddChild(body);
            panel.AddChild(heading);
            island.Content = panel;
            body.RefreshThemeResources();
            heading.RefreshThemeResources();

            island.Theme = theme;
            body.RefreshThemeResources();
            heading.RefreshThemeResources();

            Assert.AreEqual(13.5f, body.TextSize);
            Assert.AreEqual(21.25f, heading.TextSize);
        }
    }
}
