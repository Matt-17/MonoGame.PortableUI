using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class PinchTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private Game _game = null!;
        private ScreenEngine _engine = null!;
        private TestScreen _screen = null!;
        private VirtualInputSource _input = null!;
        private Border _map = null!;
        private Button _button = null!;
        private float _zoom;
        private float _turn;
        private int _started;
        private int _ended;
        private int _clicks;

        [TestInitialize]
        public void SetUp()
        {
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(200);
            _game = new Game();
            _engine = ScreenEngine.Initialize(_game, new ScreenEngineOptions { AddComponentToGame = false });
            _engine.SetScreenSize(400, 600);
            _zoom = 1;
            _turn = 0;
            _started = _ended = _clicks = 0;
            _button = new Button { Text = "On the map", Width = 120, Height = 60, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            _button.Click += (_, _) => _clicks++;
            _map = new Border { Content = _button };
            _map.PinchStarted += (_, _) => _started++;
            _map.Pinch += (_, e) =>
            {
                _zoom *= e.Scale;
                _turn += e.Rotation;
            };
            _map.PinchEnded += (_, _) => _ended++;
            _input = new VirtualInputSource();
            _screen = new TestScreen { Content = _map, InputSource = _input };
            _engine.NavigateToScreen(_screen);
            Step();
        }

        [TestCleanup]
        public void TearDown()
        {
            _engine.Dispose();
            _game.Dispose();
        }

        private void Step(params (int Id, TouchLocationState State, float X, float Y)[] touches)
        {
            var locations = new TouchLocation[touches.Length];
            for (var i = 0; i < touches.Length; i++)
                locations[i] = new TouchLocation(touches[i].Id, touches[i].State, new Vector2(touches[i].X, touches[i].Y));
            _input.SetTouches(new TouchCollection(locations));
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
            _screen.Update();
        }

        [TestMethod]
        public void Two_fingers_moving_apart_zoom_and_turning_rotates()
        {
            Step((1, TouchLocationState.Pressed, 150, 300));
            Step((1, TouchLocationState.Moved, 150, 300), (2, TouchLocationState.Pressed, 250, 300));
            // Twice as far apart.
            Step((1, TouchLocationState.Moved, 100, 300), (2, TouchLocationState.Moved, 300, 300));
            Assert.AreEqual(1, _started);
            Assert.AreEqual(2, _zoom, 0.001f);

            // Turned a quarter clockwise around the middle.
            Step((1, TouchLocationState.Moved, 200, 200), (2, TouchLocationState.Moved, 200, 400));
            Assert.AreEqual(MathF.PI / 2, _turn, 0.001f);

            Step((1, TouchLocationState.Released, 200, 200), (2, TouchLocationState.Moved, 200, 400));
            Assert.AreEqual(1, _ended);
        }

        [TestMethod]
        public void A_pinch_that_started_on_a_button_does_not_click_it()
        {
            Step((1, TouchLocationState.Pressed, 200, 300));
            Step((1, TouchLocationState.Moved, 200, 300), (2, TouchLocationState.Pressed, 300, 320));
            Step((1, TouchLocationState.Moved, 190, 300), (2, TouchLocationState.Moved, 320, 330));
            Step((1, TouchLocationState.Moved, 190, 300), (2, TouchLocationState.Released, 320, 330));
            // The finger left over is no tap either.
            Step((1, TouchLocationState.Released, 190, 300));
            Step();

            Assert.AreEqual(0, _clicks);
            Assert.AreEqual(1, _ended);
        }

        [TestMethod]
        public void One_finger_still_taps()
        {
            Step((1, TouchLocationState.Pressed, 200, 300));
            Step((1, TouchLocationState.Released, 200, 300));
            Step();

            Assert.AreEqual(1, _clicks);
            Assert.AreEqual(0, _started);
        }
    }
}
