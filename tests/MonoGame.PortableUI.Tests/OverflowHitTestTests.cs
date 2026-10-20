using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>
    ///     Input reaches what is drawn: a child that overflows a parent which does not clip it is drawn there, so it
    ///     must be clickable there too; a clipping parent (scroll viewer) cuts both off.
    /// </summary>
    [TestClass]
    [DoNotParallelize] // drives the global ScreenSystem.TotalTime clock
    public class OverflowHitTestTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private sealed class Host : IDisposable
        {
            private readonly Game _game = new Game();
            private TimeSpan _time = TimeSpan.FromSeconds(1);
            public readonly VirtualInputSource Input = new VirtualInputSource();
            public readonly ScreenEngine Engine;

            public Host(Control content)
            {
                Engine = ScreenEngine.CreateSurfaceEngine(_game, new ScreenEngineOptions { AddComponentToGame = false, ScreenSizeMode = ScreenSizeMode.Manual, DebugOverlayKey = null });
                Engine.SetScreenSize(400, 300);
                Input.SetPointer(new PointF(390, 290));
                Engine.NavigateToScreen(new TestScreen { Content = content, InputSource = Input });
                Step();
            }

            public void Step()
            {
                _time += TimeSpan.FromMilliseconds(16);
                ScreenSystem.TotalTime = _time;
                Engine.Update(new GameTime(_time, TimeSpan.FromMilliseconds(16)));
            }

            public void Click(float x, float y)
            {
                Input.SetPointer(new PointF(x, y));
                Step();
                Input.SetPointer(new PointF(x, y), leftDown: true);
                Step();
                Input.SetPointer(new PointF(x, y));
                Step();
            }

            public void Dispose()
            {
                Engine.Dispose();
                _game.Dispose();
            }
        }

        private static Button Button(Action clicked)
        {
            var button = new Button { Width = 80, Height = 30 };
            button.Click += (_, _) => clicked();
            return button;
        }

        [TestMethod]
        public void A_button_overflowing_its_row_is_clickable_where_it_is_drawn()
        {
            var clicks = 0;
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Width = 100,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            row.AddChild(Button(() => { }));
            var overflowing = Button(() => clicks++);
            row.AddChild(overflowing);
            using var host = new Host(row);
            Assert.AreEqual(80, overflowing.ClippingRect.Left, "starts inside the row");
            Assert.IsTrue(overflowing.ClippingRect.Right > row.ClippingRect.Right, "and reaches past it");

            host.Click(90, 15);
            Assert.AreEqual(1, clicks, "inside the row");
            host.Click(140, 15);
            Assert.AreEqual(2, clicks, "past the row's edge, where it is still drawn");
        }

        [TestMethod]
        public void A_clipping_parent_cuts_input_off_like_drawing()
        {
            var clicks = 0;
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.AddChild(Button(() => { }));
            row.AddChild(Button(() => clicks++));
            var viewer = new ScrollViewer
            {
                Content = row,
                Width = 100,
                Height = 40,
                ScrollOrientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            using var host = new Host(viewer);

            host.Click(140, 15);
            Assert.AreEqual(0, clicks, "outside the viewport: not drawn, not clickable");
            host.Click(90, 15);
            Assert.AreEqual(1, clicks);
        }
    }
}
