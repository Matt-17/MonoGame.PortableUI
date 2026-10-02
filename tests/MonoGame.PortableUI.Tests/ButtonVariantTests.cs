using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ButtonVariantTests
    {
        [TestMethod]
        public void Variants_take_chrome_from_their_theme_slot()
        {
            var theme = PortableTheme.FromPalette(new ThemePalette
            {
                Primary = Color.Navy,
                Secondary = Color.Orange,
                Danger = Color.DarkRed,
                SelectionText = Color.White,
                Surface = Color.Silver
            });
            var marker = new SolidColorBrush(Color.Magenta);
            theme.PrimaryButton.Normal.Background = marker;
            theme.PrimaryButton.InvalidateResolvedCache();

            var button = new TextButton("Go");
            _ = new ThemeIsland { Theme = theme, Content = button };
            button.Variant = ButtonVariant.Primary;

            Assert.AreSame(marker, button.BackgroundBrush);
            Assert.AreEqual(Color.White, ((TextBlock)button.Content!).TextColor, "readable text from the slot");

            button.Variant = ButtonVariant.Standard;
            Assert.AreNotSame(marker, button.BackgroundBrush);
        }

        [TestMethod]
        public void Variant_text_falls_back_to_black_on_light_fills()
        {
            var style = ControlStyleBuilder.Variant(Color.Yellow, Color.White, Color.Gray);
            Assert.AreEqual(Color.Black, style.Normal.TextColor);
        }
    }
}
