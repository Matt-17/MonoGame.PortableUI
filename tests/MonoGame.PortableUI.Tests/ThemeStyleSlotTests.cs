using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ThemeStyleSlotTests
    {
        private static ControlStyle Row(Color fill, CornerRadius radius)
            => new ControlStyle { Normal = new StateStyle { Background = new SolidColorBrush(fill), CornerRadius = radius } };

        [TestMethod]
        public void StyleKey_takes_the_theme_slot_and_follows_theme_switches()
        {
            var first = PortableTheme.CreateDefault();
            first.Styles["menu-row"] = Row(Color.Red, 4);
            var second = PortableTheme.CreateDefault();
            second.Styles["menu-row"] = Row(Color.Blue, 8);

            var row = new Button { Text = "Options", StyleKey = "menu-row" };
            var island = new ThemeIsland { Theme = first, Content = row };
            Assert.AreEqual(Color.Red, ((SolidColorBrush)row.BackgroundBrush!).Color);

            island.Theme = second;
            Assert.AreEqual(Color.Blue, ((SolidColorBrush)row.BackgroundBrush!).Color);
            Assert.AreEqual(8f, row.CornerRadius.TopLeft);
        }

        [TestMethod]
        public void Missing_key_falls_back_to_the_built_in_slot_and_explicit_style_wins()
        {
            var theme = PortableTheme.CreateDefault();
            var row = new Button { StyleKey = "nowhere" };
            var island = new ThemeIsland { Theme = theme, Content = row };
            Assert.AreSame(theme.Button.Normal.Background ?? theme.ButtonBackgroundBrush, row.BackgroundBrush);

            theme.Styles["nowhere"] = Row(Color.Red, 0);
            var own = Row(Color.Green, 0);
            row.Style = own;
            Assert.AreEqual(Color.Green, ((SolidColorBrush)row.BackgroundBrush!).Color);
            Assert.IsNotNull(island);
        }

        [TestMethod]
        public void Full_corner_radius_is_half_the_shorter_side()
        {
            var pill = new Border { CornerRadius = CornerRadius.Full, Width = 120, Height = 30 };
            pill.UpdateLayout(new Rect(0, 0, 200, 100));
            Assert.AreEqual(15f, pill.CornerRadius.TopLeft);
            Assert.AreEqual(15f, pill.CornerRadius.BottomRight);

            Assert.AreEqual(new CornerRadius(5, 10, 10, 10), new CornerRadius(5, float.PositiveInfinity, 40, 10).ClampTo(40, 20));
            Assert.IsTrue(CornerRadius.Full.HasFullCorner);
        }
    }
}
