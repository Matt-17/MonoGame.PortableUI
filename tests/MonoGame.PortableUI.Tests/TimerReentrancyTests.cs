using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class TimerReentrancyTests
    {
        private sealed class TestScreen : Screen
        {
        }

        [TestMethod]
        public void A_timer_callback_that_walks_the_tree_does_not_break_the_timer_update()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 300);
            var screen = new TestScreen { InputSource = new VirtualInputSource() };
            var panel = new StackPanel();
            var first = new Button { Content = new TextBlock { Text = "A" } };
            var second = new Button { Content = new TextBlock { Text = "B" } };
            panel.AddChild(first);
            panel.AddChild(second);
            screen.Content = panel;
            engine.NavigateToScreen(screen);
            screen.Update();

            // Completing on the timer walk: moving focus walks the tree with the screen's scratch list.
            first.Animate().FadeTo(0.5).Duration(TimeSpan.FromMilliseconds(50)).OnCompleted(() => screen.FocusNextTabStop()).Start();
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(100);
            screen.Update();
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(120);
            screen.Update();

            Assert.AreEqual(0.5, first.Opacity, 0.001);
        }
    }
}
