using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class ToggleSwitchPartsTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private static (ScreenEngine Engine, ToggleSwitch Switch) Show(Game game, PartMotion? motion)
        {
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, Theme = PortableTheme.CreateDefault() });
            engine.SetScreenSize(400, 300);
            var toggle = new ToggleSwitch { Width = 60, Height = 30, TrackWidth = 48, TrackHeight = 16, KnobSize = 24, KnobTravelInset = 8, KnobMotion = motion };
            engine.NavigateToScreen(new TestScreen { Content = new StackPanel { Children = { toggle } } });
            Step(engine, 0);
            return (engine, toggle);
        }

        private static double _now;

        private static void Step(ScreenEngine engine, double seconds)
        {
            _now += seconds;
            engine.Update(new GameTime(TimeSpan.FromSeconds(_now), TimeSpan.FromSeconds(seconds)));
        }

        private static float KnobOffset(ToggleSwitch toggle) => toggle.Knob.ShownTranslation.X;

        [TestMethod]
        public void Track_and_knob_are_children_that_leave_input_to_the_switch()
        {
            using var game = new Game();
            var (_, toggle) = Show(game, PartMotion.None);

            CollectionAssert.AreEqual(new[] { toggle.Track, toggle.Knob }, toggle.GetDescendants().ToArray());
            Assert.AreSame(toggle, toggle.Knob.Parent);
            Assert.IsFalse(toggle.Knob.IsHitTestVisible);
            Assert.IsFalse(toggle.Track.IsHitTestVisible);
            Assert.AreEqual(24, toggle.Knob.ClippingRect.Width, 0.01f);
            Assert.AreEqual(48, toggle.Track.ClippingRect.Width, 0.01f);
        }

        [TestMethod]
        public void Without_motion_the_knob_jumps_to_the_on_end()
        {
            using var game = new Game();
            var (engine, toggle) = Show(game, PartMotion.None);

            toggle.IsOn = true;
            Step(engine, 0.016);
            Assert.AreEqual(32, KnobOffset(toggle), 0.01f, "travel = track 48 minus twice the inset 8");
        }

        [TestMethod]
        public void An_eased_knob_arrives_after_its_duration()
        {
            using var game = new Game();
            var (engine, toggle) = Show(game, PartMotion.Ease with { Duration = 0.2f });

            toggle.IsOn = true;
            Step(engine, 0.016);
            Step(engine, 0.05);
            var midway = KnobOffset(toggle);
            Assert.IsTrue(midway > 0 && midway < 32, $"midway {midway}");
            for (var i = 0; i < 10; i++)
                Step(engine, 0.03);
            Assert.AreEqual(32, KnobOffset(toggle), 0.01f);
        }

        [TestMethod]
        public void A_lift_grows_the_knob_on_the_way_and_settles_it_back()
        {
            using var game = new Game();
            var (engine, toggle) = Show(game, PartMotion.Ease with { Duration = 0.2f, LiftScale = 0.2f });

            toggle.IsOn = true;
            Step(engine, 0.016);
            Step(engine, 0.08);
            Assert.IsTrue(toggle.Knob.ShownScale.X > 1.05f, $"scale {toggle.Knob.ShownScale.X}");
            for (var i = 0; i < 10; i++)
                Step(engine, 0.03);
            Assert.AreEqual(1, toggle.Knob.ShownScale.X, 0.001f);
        }

        [TestMethod]
        public void Each_switch_gets_its_own_copy_of_the_motion_deformation_and_an_own_one_stays()
        {
            using var game = new Game();
            var (engine, _) = Show(game, PartMotion.None);
            var toggle = new ToggleSwitch { KnobMotion = PartMotion.KnobDrop };
            var other = new ToggleSwitch { KnobMotion = PartMotion.KnobDrop };
            var own = new Deformation(4, 4);
            var custom = new ToggleSwitch { KnobMotion = PartMotion.KnobDrop };
            custom.Knob.Deformation = own;
            engine.NavigateToScreen(new TestScreen { Content = new StackPanel { Children = { toggle, other, custom } } });
            Step(engine, 0.016);

            Assert.IsNotNull(toggle.Knob.Deformation);
            Assert.AreNotSame(PartMotion.KnobDrop.Deformation, toggle.Knob.Deformation);
            Assert.AreNotSame(toggle.Knob.Deformation, other.Knob.Deformation);
            Assert.AreEqual(18f, toggle.Knob.Deformation!.Trail);
            Assert.AreSame(own, custom.Knob.Deformation);

            toggle.KnobMotion = PartMotion.Spring;
            Step(engine, 0.016);
            Assert.IsNull(toggle.Knob.Deformation, "a motion without one takes its copy off again");
        }

        [TestMethod]
        public void Motions_compose_with_with()
        {
            var both = PartMotion.KnobDrop with { LiftScale = 0.15f };

            Assert.AreEqual(0.15f, both.LiftScale);
            Assert.IsNotNull(both.TravelSpring);
            Assert.AreSame(PartMotion.KnobDrop.Deformation, both.Deformation);
            Assert.AreEqual(0f, PartMotion.KnobDrop.LiftScale);
        }
    }
}
