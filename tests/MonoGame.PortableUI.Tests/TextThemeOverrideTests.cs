using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class TextThemeOverrideTests
    {
        private static (ThemeIsland island, PortableTheme oldTheme, PortableTheme newTheme) Island(Control content)
        {
            var oldTheme = PortableTheme.CreateDefault();
            var newTheme = PortableTheme.CreateDefault();
            newTheme.TextSize = 15;
            newTheme.TextColor = Color.Orange;
            newTheme.TextBoxTextColor = Color.Teal;
            newTheme.Typography.HeadingSize = 22;
            var island = new ThemeIsland { Theme = oldTheme, Content = content };
            content.RefreshThemeResources();
            return (island, oldTheme, newTheme);
        }

        [TestMethod]
        public void Explicit_values_equal_to_old_theme_default_survive_a_theme_change()
        {
            var text = new TextBlock { Text = "X", TextSize = 14, TextColor = Color.Black };
            var (island, oldTheme, newTheme) = Island(text);
            Assert.AreEqual(oldTheme.TextSize, text.TextSize);
            Assert.AreEqual(oldTheme.TextColor, text.TextColor);

            island.Theme = newTheme;
            text.RefreshThemeResources();

            Assert.AreEqual(14, text.TextSize);
            Assert.AreEqual(Color.Black, text.TextColor);
        }

        [TestMethod]
        public void Seeded_values_follow_the_theme()
        {
            var text = new TextBlock { Text = "X" };
            var (island, _, newTheme) = Island(text);

            island.Theme = newTheme;
            text.RefreshThemeResources();

            Assert.AreEqual(15, text.TextSize);
            Assert.AreEqual(Color.Orange, text.TextColor);
        }

        [TestMethod]
        public void Heading_size_follows_the_theme_until_set_explicitly()
        {
            var heading = new TextBlock { Text = "X", IsHeading = true };
            var (island, _, newTheme) = Island(heading);

            island.Theme = newTheme;
            heading.RefreshThemeResources();
            Assert.AreEqual(22, heading.TextSize);

            heading.TextSize = 30;
            island.Theme = PortableTheme.CreateDefault();
            heading.RefreshThemeResources();
            Assert.AreEqual(30, heading.TextSize);
        }

        [TestMethod]
        public void TextBox_follows_its_own_text_colour_slot_unless_overridden()
        {
            var seeded = new TextBox();
            var (island, _, newTheme) = Island(seeded);
            island.Theme = newTheme;
            seeded.RefreshThemeResources();
            Assert.AreEqual(Color.Teal, seeded.TextColor);

            var explicitBox = new TextBox { TextColor = Color.Black };
            var (island2, _, newTheme2) = Island(explicitBox);
            island2.Theme = newTheme2;
            explicitBox.RefreshThemeResources();
            Assert.AreEqual(Color.Black, explicitBox.TextColor);
        }
    }
}
