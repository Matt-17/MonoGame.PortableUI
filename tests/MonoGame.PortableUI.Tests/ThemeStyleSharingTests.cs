using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ThemeStyleSharingTests
    {
        [TestMethod]
        public void Restyling_one_control_type_leaves_the_others_alone()
        {
            // FromPalette shared one style between text, check and combo boxes: a theme that set the
            // combo box's focused border also replaced the text box's focused underline.
            var theme = PortableTheme.FromPalette(new ThemePalette());
            var textBoxFocused = theme.TextBox.Focused.BorderBrush;

            theme.ComboBox.Focused.BorderBrush = new SolidColorBrush(Color.Red);
            theme.Tab.Normal.BorderBrush = new SolidColorBrush(Color.Red);

            Assert.AreSame(textBoxFocused, theme.TextBox.Focused.BorderBrush);
            Assert.AreNotSame(theme.TextBox, theme.CheckBox);
            Assert.AreNotSame(theme.Button, theme.Tab);
            Assert.AreNotSame(theme.Button.Normal.BorderBrush, theme.Tab.Normal.BorderBrush);
        }
    }
}
