using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class FloatingHintTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private Game _game = null!;
        private ScreenEngine _engine = null!;
        private TestScreen _screen = null!;

        [TestInitialize]
        public void SetUp()
        {
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(120);
            _game = new Game();
            _engine = ScreenEngine.Initialize(_game, new ScreenEngineOptions { AddComponentToGame = false });
            _engine.SetScreenSize(400, 600);
        }

        [TestCleanup]
        public void TearDown()
        {
            _engine.Dispose();
            _game.Dispose();
        }

        private TextBox Show(TextBox box)
        {
            _screen = new TestScreen { Content = new StackPanel { Children = { box } } };
            _engine.NavigateToScreen(_screen);
            Frames(1);
            return box;
        }

        private void Frames(int count)
        {
            for (var i = 0; i < count; i++)
            {
                ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
                _screen.Update();
            }
        }

        [TestMethod]
        public void The_label_rises_on_focus_and_settles_on_the_way_back()
        {
            var box = Show(new TextBox { HintText = "Who?", FloatingHint = true, Height = 64 });
            Assert.AreEqual(0, box.HintLift);

            box.Focus();
            Frames(3);
            Assert.IsTrue(box.HintLift > 0.2f && box.HintLift < 1, $"on its way ({box.HintLift})");
            Frames(30);
            Assert.AreEqual(1, box.HintLift);

            _engine.FocusedControl = null;
            Frames(30);
            Assert.AreEqual(0, box.HintLift, "empty again: back in place of the text");
        }

        [TestMethod]
        public void A_box_with_text_keeps_its_label_raised()
        {
            var box = Show(new TextBox { HintText = "What?", Text = "Drill", FloatingHint = true, Height = 64 });

            Assert.AreEqual(1, box.HintLift, "raised from the start, no animation on open");
            box.Focus();
            _engine.FocusedControl = null;
            Frames(30);
            Assert.AreEqual(1, box.HintLift);
        }
    }
}
