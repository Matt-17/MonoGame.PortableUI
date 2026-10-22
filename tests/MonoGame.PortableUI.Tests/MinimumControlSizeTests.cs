using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class MinimumControlSizeTests
    {
        private sealed class TestScreen : Screen
        {
        }

        // Measures controls inside a screen of an engine with the given theme minimum.
        private static Size Measured(ScreenEngine engine, Control control)
        {
            engine.NavigateToScreen(new TestScreen { Content = new StackPanel { Children = { control } } });
            return control.Measure();
        }

        private static ScreenEngine Engine(Game game, float minimum)
        {
            var theme = PortableTheme.CreateDefault();
            theme.MinimumControlSize = minimum;
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, Theme = theme });
            engine.SetScreenSize(400, 600);
            return engine;
        }

        [TestMethod]
        public void Interactive_controls_grow_to_the_theme_minimum()
        {
            using var game = new Game();
            var engine = Engine(game, 44);

            Assert.AreEqual(44, Measured(engine, new TextButton("S")).Height, 0.01f);
            Assert.AreEqual(44, Measured(engine, new TextButton("S")).Width, 0.01f, "a short label still gets a 44 wide target");
            Assert.AreEqual(44, Measured(engine, new CheckBox { Text = "x" }).Height, 0.01f);
            Assert.AreEqual(44, Measured(engine, new TextBox { Text = "x" }).Height, 0.01f);
            Assert.AreEqual(44, Measured(engine, new RadioButton { Text = "x" }).Height, 0.01f);
            Assert.IsTrue(Measured(engine, new TextBlock { Text = "x" }).Height < 44, "plain text is not a touch target");
        }

        [TestMethod]
        public void An_explicit_minimum_and_list_rows_ignore_the_theme_minimum()
        {
            using var game = new Game();
            var engine = Engine(game, 44);
            var list = new ListBox { ItemHeight = 20, Height = 200 };
            list.Items.Add("Row");
            var screen = new TestScreen { Content = list };
            engine.NavigateToScreen(screen);
            engine.Update(new GameTime(System.TimeSpan.FromMilliseconds(16), System.TimeSpan.FromMilliseconds(16)));

            var explicitMinimum = new TextButton("S") { MinHeight = 20, Padding = new Thickness(0) };
            engine.NavigateToScreen(new TestScreen { Content = new StackPanel { Children = { explicitMinimum } } });
            Assert.IsTrue(explicitMinimum.Measure().Height < 44, "the control's own MinHeight wins");
            Assert.IsTrue(list.ItemButtons.First().ClippingRect.Height < 44, $"row {list.ItemButtons.First().ClippingRect.Height}");
        }

        [TestMethod]
        public void An_explicit_size_wins_on_its_axis()
        {
            using var game = new Game();
            var engine = Engine(game, 44);

            var size = Measured(engine, new TextButton("S") { Height = 38 });
            Assert.AreEqual(38, size.Height, 0.01f, "a fixed Height is not raised");
            Assert.AreEqual(44, size.Width, 0.01f, "the other axis still gets the minimum");
            Assert.AreEqual(30, Measured(engine, new TextButton("S") { Width = 30 }).Width, 0.01f);
            Assert.AreEqual(44, Measured(engine, new ComboBox()).Height, 0.01f, "a height seeded from the theme is not the app's");
        }

        [TestMethod]
        public void No_minimum_by_default()
        {
            using var game = new Game();
            var engine = Engine(game, 0);

            Assert.IsTrue(Measured(engine, new TextButton("S") { Padding = new Thickness(0) }).Height < 44);
        }
    }
}
