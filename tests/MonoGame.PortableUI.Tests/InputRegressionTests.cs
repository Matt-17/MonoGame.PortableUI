using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class InputRegressionTests
    {
        [TestMethod]
        public void Double_click_event_uses_configured_time_window()
        {
            var button = new Button { Text = "Double" };
            var doubleClicks = 0;
            button.DoubleClick += (sender, args) => doubleClicks++;

            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(100);
            button.OnClick();
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(250);
            button.OnClick();
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(1000);
            button.OnClick();

            Assert.AreEqual(1, doubleClicks);
        }

        [TestMethod]
        public void Timer_elapsed_is_driven_by_screen_time()
        {
            var timer = new Timer(300);
            var elapsed = 0;
            timer.Elapsed += (sender, args) => elapsed++;

            ScreenSystem.TotalTime = TimeSpan.Zero;
            timer.Start();
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(299);
            timer.Update();
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(300);
            timer.Update();

            Assert.AreEqual(1, elapsed);
            Assert.IsFalse(timer.IsRunning);
        }
    }
}
