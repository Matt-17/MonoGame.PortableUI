using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ThemeLiveSwitchTests
    {
        private static PortableTheme CreateTheme(Color buttonText, Brush hover)
        {
            var theme = PortableTheme.CreateDefault();
            theme.ButtonTextColor = buttonText;
            theme.ButtonHoverBrush = hover;
            theme.ButtonPadding = new Thickness(9, 7);
            return theme;
        }

        [TestMethod]
        public void Theme_switch_reseeds_untouched_button_snapshots()
        {
            var hoverA = new SolidColorBrush(Color.Red);
            var hoverB = new SolidColorBrush(Color.Lime);
            var themeA = CreateTheme(Color.Yellow, hoverA);
            var themeB = CreateTheme(Color.Cyan, hoverB);

            var island = new ThemeIsland { Theme = themeA };
            var button = new Button();
            island.Content = button;

            // Seed from theme A (button was constructed under the fallback theme).
            button.RefreshThemeResources();
            // The ctor snapshot came from the fallback theme; after the first refresh the
            // untouched values must equal theme A's.
            Assert.AreSame(hoverA, button.HoverColor);

            island.Theme = themeB;
            button.RefreshThemeResources();

            Assert.AreSame(hoverB, button.HoverColor);
            Assert.AreEqual(Color.Cyan, button.TextColor);
        }

        [TestMethod]
        public void Theme_switch_preserves_user_overrides()
        {
            var themeA = CreateTheme(Color.Yellow, new SolidColorBrush(Color.Red));
            var themeB = CreateTheme(Color.Cyan, new SolidColorBrush(Color.Lime));

            var island = new ThemeIsland { Theme = themeA };
            var button = new Button();
            island.Content = button;
            button.RefreshThemeResources();

            var customHover = new SolidColorBrush(Color.Purple);
            button.HoverColor = customHover;
            button.TextColor = Color.Orange;

            island.Theme = themeB;
            button.RefreshThemeResources();

            Assert.AreSame(customHover, button.HoverColor);
            Assert.AreEqual(Color.Orange, button.TextColor);
        }

        [TestMethod]
        public void Button_consumes_theme_button_style_for_border_and_corner_radius()
        {
            var theme = PortableTheme.CreateDefault();
            var border = new SolidColorBrush(Color.Navy);
            theme.Button.Normal.BorderBrush = border;
            theme.Button.Normal.BorderThickness = new Thickness(2);
            theme.Button.Normal.CornerRadius = 5;

            var island = new ThemeIsland { Theme = theme };
            var button = new Button();
            island.Content = button;

            Assert.AreSame(border, button.BorderBrush);
            Assert.AreEqual(new Thickness(2), button.BorderThickness);
            Assert.AreEqual(5f, button.CornerRadius.TopLeft);

            // Explicit assignment wins over the style.
            button.CornerRadius = 0;
            Assert.AreEqual(0f, button.CornerRadius.TopLeft);
        }

        [TestMethod]
        public void Internal_buttons_do_not_consume_the_button_style()
        {
            var theme = PortableTheme.CreateDefault();
            theme.Button.Normal.BorderThickness = new Thickness(3);

            var island = new ThemeIsland { Theme = theme };
            var button = new Button { UseThemeStyle = false };
            island.Content = button;

            Assert.AreEqual(new Thickness(0), button.BorderThickness);
        }
    }
}
