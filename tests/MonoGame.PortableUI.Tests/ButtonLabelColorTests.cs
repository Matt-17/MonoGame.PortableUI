using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ButtonLabelColorTests
    {
        [TestMethod]
        public void Explicit_label_color_survives_state_changes()
        {
            var label = new TextBlock { Text = "Go", TextColor = Color.Navy };
            var button = new Button { Content = label, PressedTextColor = Color.White };

            button.ForcePressedVisual = true;
            button.OnFrameUpdate();
            button.ForcePressedVisual = false;
            button.OnFrameUpdate();

            Assert.AreEqual(Color.Navy, label.TextColor);
        }

        [TestMethod]
        public void Unset_label_takes_button_colors_from_the_start()
        {
            var label = new TextBlock { Text = "Go" };
            var button = new Button { TextColor = Color.Orange, PressedTextColor = Color.White, Content = label };

            Assert.AreEqual(Color.Orange, label.TextColor, "before any state change");
            button.ForcePressedVisual = true;
            button.OnFrameUpdate();
            Assert.AreEqual(Color.White, label.TextColor);
            button.ForcePressedVisual = false;
            button.OnFrameUpdate();
            Assert.AreEqual(Color.Orange, label.TextColor);
        }

        [TestMethod]
        public void Explicit_button_text_color_still_drives_an_explicit_label()
        {
            var label = new TextBlock { Text = "Go", TextColor = Color.Navy };
            var button = new Button { Content = label };

            button.TextColor = Color.Orange;

            Assert.AreEqual(Color.Orange, label.TextColor);
        }
    }
}
