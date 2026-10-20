using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class RadioButtonLabelTests
    {
        [TestMethod]
        public void A_radio_label_keeps_its_color_while_hovered_and_pressed()
        {
            // The button's pressed text color is meant for a filled face; on a radio it whitened the label.
            using var game = new Game();
            var theme = PortableTheme.CreateDefault();
            theme.ButtonPressedTextColor = Color.White;
            theme.ButtonHoverTextColor = Color.White;
            ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, Theme = theme });

            var radio = new RadioButton { Text = "Small" };

            Assert.IsNull(radio.PressedTextColor);
            Assert.IsNull(radio.HoverTextColor);
        }
    }
}
