using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;
using MonoGame.PortableUI.Themes;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>#91: selected text stays readable on an opaque selection highlight.</summary>
    [TestClass]
    [DoNotParallelize] // sets ScreenEngine.Instance and its theme, which new controls read
    public class TextBoxSelectionColorTests
    {
        private static readonly Color Navy = new Color(0, 0, 128);

        [TestMethod]
        public void A_translucent_highlight_keeps_the_text_colour()
        {
            var box = new TextBox { SelectionBrush = new SolidColorBrush(new Color(51, 153, 255, 95)) };

            Assert.IsNull(box.GetSelectedTextColor());
        }

        [TestMethod]
        public void An_opaque_highlight_inverts_to_the_box_background()
        {
            // Windows 3.1: black on white, navy highlight -> white selected text.
            var box = new TextBox
            {
                TextColor = Color.Black,
                BackgroundBrush = new SolidColorBrush(Color.White),
                SelectionBrush = new SolidColorBrush(Navy)
            };

            Assert.AreEqual(Color.White, box.GetSelectedTextColor());
        }

        [TestMethod]
        public void A_background_too_close_to_the_highlight_falls_back_to_black_or_white()
        {
            var box = new TextBox
            {
                BackgroundBrush = new SolidColorBrush(new Color(0, 0, 150)),
                SelectionBrush = new SolidColorBrush(Navy)
            };

            Assert.AreEqual(Color.White, box.GetSelectedTextColor());

            box.SelectionBrush = new SolidColorBrush(Color.Yellow);
            box.BackgroundBrush = new SolidColorBrush(new Color(255, 255, 200));
            Assert.AreEqual(Color.Black, box.GetSelectedTextColor());
        }

        [TestMethod]
        public void An_explicit_selection_text_colour_wins()
        {
            var box = new TextBox
            {
                BackgroundBrush = new SolidColorBrush(Color.White),
                SelectionBrush = new SolidColorBrush(Navy),
                SelectionTextColor = Color.Yellow
            };

            Assert.AreEqual(Color.Yellow, box.GetSelectedTextColor());

            box.SelectionBrush = new SolidColorBrush(new Color(51, 153, 255, 95));
            Assert.AreEqual(Color.Yellow, box.GetSelectedTextColor(), "also under a translucent highlight");
        }

        [TestMethod]
        public void Game_boy_selection_reads_light_on_dark_green()
        {
            var theme = PortableThemes.Resolve("gameboy").CreateTheme();
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, Theme = theme });
            var box = new TextBox();
            var background = ((FrameBrush)theme.TextBoxBackgroundBrush!).Face;

            Assert.AreEqual(background, box.GetSelectedTextColor(), "the selection inverts to the screen's light green");
            Assert.AreNotEqual(box.TextColor, box.GetSelectedTextColor());
            engine.Dispose();
        }

        [TestMethod]
        public void The_theme_slot_seeds_the_box_and_follows_theme_switches()
        {
            var first = PortableTheme.CreateDefault();
            first.TextBoxSelectionTextColor = Color.Red;
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, Theme = first });
            var box = new TextBox();
            var screen = new TestScreen { Content = box };
            engine.SetScreenSize(200, 100);
            engine.NavigateToScreen(screen);
            Assert.AreEqual(Color.Red, box.SelectionTextColor);

            var second = PortableTheme.CreateDefault();
            second.TextBoxSelectionTextColor = Color.Lime;
            engine.Options.Theme = second;
            engine.Update(new GameTime(System.TimeSpan.FromSeconds(1), System.TimeSpan.FromMilliseconds(16)));
            Assert.AreEqual(Color.Lime, box.SelectionTextColor, "a theme value follows the theme");

            box.SelectionTextColor = Color.Blue;
            engine.Options.Theme = first;
            engine.Update(new GameTime(System.TimeSpan.FromSeconds(2), System.TimeSpan.FromMilliseconds(16)));
            Assert.AreEqual(Color.Blue, box.SelectionTextColor, "a user override survives theme switches");
            engine.Dispose();
        }

        private sealed class TestScreen : Screen
        {
        }
    }
}
