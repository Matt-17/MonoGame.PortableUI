using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class SegmentedControlTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private static double _now;

        private static ScreenEngine Show(Game game, Control content)
        {
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, Theme = PortableTheme.CreateDefault() });
            engine.SetScreenSize(400, 300);
            engine.NavigateToScreen(new TestScreen { Content = new StackPanel { Children = { content } } });
            Step(engine, 0);
            return engine;
        }

        private static void Step(ScreenEngine engine, double seconds)
        {
            _now += seconds;
            engine.Update(new GameTime(TimeSpan.FromSeconds(_now), TimeSpan.FromSeconds(seconds)));
        }

        private static SegmentedControl Make(PartMotion motion, Orientation orientation = Orientation.Horizontal)
        {
            var segmented = new SegmentedControl { Width = 300, IndicatorInset = 0, IndicatorMotion = motion, Orientation = orientation, SelectedIndex = 0 };
            segmented.Add("Sun").Add("System").Add("Light", "from 18");
            return segmented;
        }

        [TestMethod]
        public void Options_share_the_width_and_the_indicator_rests_on_the_first()
        {
            using var game = new Game();
            var segmented = Make(PartMotion.None);
            Show(game, segmented);

            Assert.AreEqual(3, segmented.Segments.Count);
            Assert.AreEqual(100, segmented.Indicator.ClippingRect.Width, 0.01f);
            Assert.AreEqual(Vector2.Zero, segmented.Indicator.ShownTranslation);
            Assert.IsFalse(segmented.Indicator.IsHitTestVisible);
        }

        [TestMethod]
        public void Tapping_an_option_selects_it_and_the_indicator_goes_there()
        {
            using var game = new Game();
            var segmented = Make(PartMotion.None);
            var engine = Show(game, segmented);
            var changes = 0;
            segmented.SelectionChanged += (_, _) => changes++;

            var third = segmented.GetDescendants().OfType<Control>().Skip(3).First();
            third.OnClick();
            Step(engine, 0.016);

            Assert.AreEqual(2, segmented.SelectedIndex);
            Assert.AreEqual(1, changes);
            Assert.AreEqual("Light", segmented.SelectedSegment!.Text);
            Assert.AreEqual(200, segmented.Indicator.ShownTranslation.X, 0.01f);
        }

        [TestMethod]
        public void A_spring_carries_the_indicator_over_and_it_settles_on_the_option()
        {
            using var game = new Game();
            var segmented = Make(PartMotion.Spring, Orientation.Vertical);
            var engine = Show(game, segmented);
            var row = segmented.Indicator.ClippingRect.Height;

            segmented.SelectedIndex = 1;
            Step(engine, 0.016);
            Step(engine, 0.05);
            var midway = segmented.Indicator.ShownTranslation.Y;
            Assert.IsTrue(midway > 0 && midway < row, $"midway {midway} of {row}");
            for (var i = 0; i < 60; i++)
                Step(engine, 0.03);
            Assert.AreEqual(row, segmented.Indicator.ShownTranslation.Y, 0.01f);
            Assert.AreEqual(0, segmented.Indicator.ShownTranslation.X, 0.01f);
        }

        [TestMethod]
        public void Arrow_keys_move_the_selection_along_the_orientation()
        {
            using var game = new Game();
            var segmented = Make(PartMotion.None);
            Show(game, segmented);

            segmented.RaiseKeyDown(new KeyEventArgs(Microsoft.Xna.Framework.Input.Keys.Right, KeyboardModifiers.None));
            Assert.AreEqual(1, segmented.SelectedIndex);
            segmented.RaiseKeyDown(new KeyEventArgs(Microsoft.Xna.Framework.Input.Keys.Down, KeyboardModifiers.None));
            Assert.AreEqual(1, segmented.SelectedIndex, "down does not move a side-by-side control");
        }

        [TestMethod]
        public void The_theme_motion_gives_the_indicator_its_own_drop()
        {
            using var game = new Game();
            var segmented = new SegmentedControl();
            segmented.Add("A").Add("B");
            var engine = Show(game, segmented);
            Step(engine, 0.016);

            Assert.IsNotNull(segmented.Indicator.Deformation);
            Assert.AreNotSame(PartMotion.Drop.Deformation, segmented.Indicator.Deformation);
        }
    }
}
