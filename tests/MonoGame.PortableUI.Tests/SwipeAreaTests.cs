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
    public class SwipeAreaTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private Game _game = null!;
        private TestScreen _screen = null!;
        private VirtualInputSource _input = null!;
        private SwipeArea _area = null!;
        private Button _button = null!;
        private Border _rightAction = null!;
        private int _clicks;
        private SwipeDirection? _swiped;

        [TestInitialize]
        public void SetUp()
        {
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(50);
            _game = new Game();
            var engine = ScreenEngine.Initialize(_game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 600);
            _button = new Button { Text = "Row", Height = 80 };
            _clicks = 0;
            _button.Click += (_, _) => _clicks++;
            _rightAction = new Border();
            _area = new SwipeArea { Content = _button, RightAction = _rightAction, FollowsFinger = true, Height = 80, VerticalAlignment = VerticalAlignment.Top };
            _swiped = null;
            _area.Swiped += (_, e) => _swiped = e.Direction;
            _input = new VirtualInputSource();
            _screen = new TestScreen { Content = _area, InputSource = _input };
            engine.NavigateToScreen(_screen);
            Frame();
        }

        [TestCleanup]
        public void TearDown() => _game.Dispose();

        private void Frame()
        {
            _input.SetTouches(new TouchCollection(Array.Empty<TouchLocation>()));
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
            _screen.Update();
        }

        private void Touch(TouchLocationState state, float x, float y)
        {
            _input.SetTouches(new TouchCollection(new[] { new TouchLocation(1, state, new Vector2(x, y)) }));
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
            _screen.Update();
        }

        private void Drag(float fromX, float toX, float y, int steps = 8, float toY = float.NaN)
        {
            toY = float.IsNaN(toY) ? y : toY;
            Touch(TouchLocationState.Pressed, fromX, y);
            for (var i = 1; i <= steps; i++)
                Touch(TouchLocationState.Moved, fromX + (toX - fromX) * i / steps, y + (toY - y) * i / steps);
            Touch(TouchLocationState.Released, toX, toY);
            Frame();
        }

        [TestMethod]
        public void A_tap_clicks_the_content_and_is_no_swipe()
        {
            Touch(TouchLocationState.Pressed, 200, 40);
            Touch(TouchLocationState.Released, 200, 40);
            Frame();

            Assert.AreEqual(1, _clicks);
            Assert.IsNull(_swiped);
        }

        [TestMethod]
        public void A_long_swipe_left_is_reported_and_does_not_click()
        {
            Drag(300, 120, 40);

            Assert.AreEqual(SwipeDirection.Left, _swiped);
            Assert.AreEqual(0, _clicks);
        }

        [TestMethod]
        public void A_short_slow_drag_springs_back_without_a_swipe()
        {
            // 40 units over many frames: neither far (96) nor fast enough.
            Drag(200, 160, 40, steps: 30);

            Assert.IsNull(_swiped);
            Assert.AreEqual(0, _clicks, "a drag is no tap either");
        }

        [TestMethod]
        public void A_quick_flick_counts_even_when_short()
        {
            Drag(200, 150, 40, steps: 2);

            Assert.AreEqual(SwipeDirection.Left, _swiped);
        }

        [TestMethod]
        public void A_vertical_drag_is_left_alone()
        {
            Drag(200, 205, 10, toY: 70);

            Assert.IsNull(_swiped);
        }

        [TestMethod]
        public void The_content_follows_the_finger_and_reveals_the_action_behind()
        {
            Touch(TouchLocationState.Pressed, 300, 40);
            for (var x = 290; x >= 230; x -= 10)
                Touch(TouchLocationState.Moved, x, 40);

            Assert.AreEqual(-70, _button.Translation.X, 0.01f);
            Assert.IsTrue(_rightAction.IsVisible);

            Touch(TouchLocationState.Released, 230, 40);
        }

        [TestMethod]
        public void A_blocked_direction_only_gives_way_a_little()
        {
            _area.CanSwipeRight = false;
            Touch(TouchLocationState.Pressed, 100, 40);
            for (var x = 110; x <= 300; x += 10)
                Touch(TouchLocationState.Moved, x, 40);

            Assert.AreEqual(200 * 0.15f, _button.Translation.X, 0.01f);
            Touch(TouchLocationState.Released, 300, 40);
            Frame();
            Assert.IsNull(_swiped);
        }
    }
}
