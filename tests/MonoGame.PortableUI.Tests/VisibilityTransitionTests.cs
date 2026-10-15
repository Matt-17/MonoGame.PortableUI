using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>Enter/exit transitions on IsVisible/IsGone (control.VisibilityTransition).</summary>
    [TestClass]
    public class VisibilityTransitionTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private static readonly VisibilityTransition LinearFade = new VisibilityTransition(
            new Transition(TimeSpan.FromMilliseconds(100), Easings.Linear))
        {
            Scale = new Vector2(0.5f),
            Offset = new Vector2(0, 20)
        };

        [TestInitialize]
        public void ResetTime()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
        }

        private static (TestScreen Screen, Border First, Border Second) Create(Game game)
        {
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 300);
            var first = new Border { Height = 40, VisibilityTransition = LinearFade };
            var second = new Border { Height = 40 };
            var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
            panel.AddChild(first);
            panel.AddChild(second);
            var screen = new TestScreen { Content = panel };
            engine.NavigateToScreen(screen);
            screen.Update();
            return (screen, first, second);
        }

        private static void At(Screen screen, double milliseconds)
        {
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(milliseconds);
            screen.Update();
        }

        [TestMethod]
        public void Gone_plays_the_exit_before_the_control_leaves_the_layout()
        {
            using var game = new Game();
            var (screen, first, second) = Create(game);
            Assert.AreEqual(40f, second.BoundingRect.Top, 0.01f);

            first.IsGone = true;
            Assert.IsTrue(first.IsGone, "the property changes at once");
            Assert.IsFalse(first.IsEffectivelyInteractive, "no input while it exits");
            Assert.IsTrue(first.IsPresenceAnimating);

            At(screen, 50);
            Assert.AreEqual(40f, second.BoundingRect.Top, 0.01f, "keeps its space while it exits");
            Assert.AreEqual(0.5f, first.PresenceOpacity, 0.01f);
            Assert.AreEqual(0.75f, first.PresenceScale.X, 0.01f);
            Assert.AreEqual(10f, first.PresenceOffset.Y, 0.01f);

            At(screen, 120);
            At(screen, 140);
            Assert.IsFalse(first.IsPresenceAnimating);
            Assert.AreEqual(0f, second.BoundingRect.Top, 0.01f, "collapsed once the exit ended");
            Assert.AreEqual(1f, first.PresenceOpacity, "the pose resets for the next enter");
        }

        [TestMethod]
        public void Showing_plays_the_enter_from_the_hidden_pose()
        {
            using var game = new Game();
            var (screen, first, _) = Create(game);
            first.VisibilityTransition = null;
            first.IsGone = true;
            first.VisibilityTransition = LinearFade;
            screen.Update();

            first.IsGone = false;
            Assert.AreEqual(0f, first.PresenceOpacity, 0.001f, "starts hidden");
            Assert.AreEqual(0.5f, first.PresenceScale.X, 0.001f);
            At(screen, 50);
            Assert.AreEqual(0.5f, first.PresenceOpacity, 0.01f);
            At(screen, 100);
            At(screen, 110);
            Assert.IsFalse(first.IsPresenceAnimating);
            Assert.AreEqual(Vector2.One, first.PresenceScale);
            Assert.AreEqual(1f, first.PresenceOpacity);
        }

        [TestMethod]
        public void Showing_during_the_exit_reverses_from_where_it_is()
        {
            using var game = new Game();
            var (screen, first, second) = Create(game);
            first.IsVisible = false;
            At(screen, 60);
            Assert.IsTrue(first.IsVisibleShown, "still drawn while it exits");
            Assert.AreEqual(0.4f, first.PresenceOpacity, 0.01f);

            first.IsVisible = true;
            Assert.AreEqual(0.4f, first.PresenceOpacity, 0.01f, "no jump");
            At(screen, 80);
            Assert.IsTrue(first.PresenceOpacity > 0.4f, "heading back in");
            Assert.AreEqual(40f, second.BoundingRect.Top, 0.01f);
        }

        [TestMethod]
        public void A_hidden_control_is_no_longer_drawn_after_its_exit()
        {
            using var game = new Game();
            var (screen, first, _) = Create(game);
            first.IsVisible = false;
            At(screen, 50);
            Assert.IsTrue(first.IsVisibleShown);
            At(screen, 150);
            At(screen, 160);
            Assert.IsFalse(first.IsVisibleShown);
            Assert.IsFalse(first.IsVisible);
        }

        [TestMethod]
        public void After_an_exit_showing_plays_the_enter_again()
        {
            using var game = new Game();
            var (screen, first, _) = Create(game);
            first.IsVisible = false;
            At(screen, 50);
            At(screen, 150);
            At(screen, 160);
            Assert.IsFalse(first.IsVisibleShown);

            first.IsVisible = true;
            Assert.IsTrue(first.IsVisibleShown);
            Assert.AreEqual(0f, first.PresenceOpacity, 0.001f);
            At(screen, 210);
            Assert.AreEqual(0.5f, first.PresenceOpacity, 0.01f);
            At(screen, 300);
            Assert.AreEqual(1f, first.PresenceOpacity);
        }

        [TestMethod]
        public void Removing_the_transition_mid_exit_removes_the_control_at_once()
        {
            using var game = new Game();
            var (screen, first, second) = Create(game);
            first.IsGone = true;
            At(screen, 30);
            first.VisibilityTransition = null;
            screen.Update();
            Assert.AreEqual(0f, second.BoundingRect.Top, 0.01f);
            Assert.AreEqual(1f, first.PresenceOpacity);
        }

        [TestMethod]
        public void CompleteTransitions_ends_an_exit()
        {
            using var game = new Game();
            var (screen, first, second) = Create(game);
            first.IsGone = true;
            first.CompleteTransitions();
            screen.Update();
            Assert.IsFalse(first.IsPresenceAnimating);
            Assert.AreEqual(0f, second.BoundingRect.Top, 0.01f);
        }

        [TestMethod]
        public void Off_screen_a_control_shows_and_hides_at_once()
        {
            var control = new Border { VisibilityTransition = LinearFade };
            control.IsGone = true;
            Assert.IsTrue(control.IsGoneShown);
            Assert.IsFalse(control.IsPresenceAnimating);
            control.IsGone = false;
            Assert.AreEqual(1f, control.PresenceOpacity);
        }

        [TestMethod]
        public void A_spring_enter_gets_a_quick_eased_exit_by_default()
        {
            var transition = VisibilityTransition.Pop;
            Assert.IsTrue(transition.Enter.IsSpring);
            Assert.IsFalse(transition.Exit.IsSpring);
            Assert.AreSame(LinearFade.Enter, LinearFade.Exit, "an eased enter is reused for the exit");
        }
    }
}
