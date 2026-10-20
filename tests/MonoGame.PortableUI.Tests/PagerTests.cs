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
    public class PagerTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private Game _game = null!;
        private TestScreen _screen = null!;
        private VirtualInputSource _input = null!;
        private Pager _pager = null!;
        private Button _button = null!;
        private ScrollViewer _list = null!;
        private int _clicks;
        private int _pageChanges;

        [TestInitialize]
        public void SetUp()
        {
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(80);
            _game = new Game();
            var engine = ScreenEngine.Initialize(_game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 600);
            _button = new Button { Text = "On page 1", Height = 100, VerticalAlignment = VerticalAlignment.Top };
            _clicks = 0;
            _button.Click += (_, _) => _clicks++;
            var tall = new StackPanel();
            for (var i = 0; i < 30; i++)
                tall.AddChild(new Border { Height = 50 });
            _list = new ScrollViewer { Content = tall };
            _pager = new Pager();
            _pager.Pages.Add(_button);
            _pager.Pages.Add(_list);
            _pageChanges = 0;
            _pager.PageChanged += (_, _) => _pageChanges++;
            _input = new VirtualInputSource();
            _screen = new TestScreen { Content = _pager, InputSource = _input };
            engine.NavigateToScreen(_screen);
            Frame();
        }

        [TestCleanup]
        public void TearDown() => _game.Dispose();

        private void Frame(int count = 1)
        {
            for (var i = 0; i < count; i++)
            {
                _input.SetTouches(new TouchCollection(Array.Empty<TouchLocation>()));
                ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
                _screen.Update();
            }
        }

        private void Touch(TouchLocationState state, float x, float y)
        {
            _input.SetTouches(new TouchCollection(new[] { new TouchLocation(1, state, new Vector2(x, y)) }));
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
            _screen.Update();
        }

        // A drag, then the finger rests before it lifts (no flick).
        private void Drag(float fromX, float toX, float y = 50, int steps = 10, bool rest = true, float toY = float.NaN)
        {
            toY = float.IsNaN(toY) ? y : toY;
            Touch(TouchLocationState.Pressed, fromX, y);
            for (var i = 1; i <= steps; i++)
                Touch(TouchLocationState.Moved, fromX + (toX - fromX) * i / steps, y + (toY - y) * i / steps);
            if (rest)
                ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(300);
            Touch(TouchLocationState.Released, toX, toY);
        }

        [TestMethod]
        public void Pages_are_as_wide_as_the_pager_and_sit_side_by_side()
        {
            Assert.AreEqual(400, _button.ClippingRect.Width, 0.01f);
            Assert.AreEqual(400, _list.ClippingRect.Left, 0.01f);
            Assert.AreEqual(0, _pager.PageIndex);
        }

        [TestMethod]
        public void A_slow_drag_past_half_settles_on_the_next_page_and_follows_the_finger_meanwhile()
        {
            Touch(TouchLocationState.Pressed, 350, 50);
            for (var x = 340; x >= 100; x -= 10)
                Touch(TouchLocationState.Moved, x, 50);
            Assert.AreEqual(250 / 400f, _pager.PagePosition, 0.03f, "the next page is part way in");

            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(300);
            Touch(TouchLocationState.Released, 100, 50);
            Frame(90);

            Assert.AreEqual(1, _pager.PageIndex);
            Assert.AreEqual(1, _pager.PagePosition, 0.001f);
            Assert.AreEqual(1, _pageChanges);
            Assert.AreEqual(0, _clicks, "the button under the swipe does not click");
        }

        [TestMethod]
        public void A_slow_short_drag_goes_back()
        {
            Drag(300, 200);
            Frame(90);

            Assert.AreEqual(0, _pager.PagePosition, 0.001f);
        }

        [TestMethod]
        public void A_flick_moves_one_page_even_when_short()
        {
            Drag(300, 220, steps: 3, rest: false);
            Frame(90);

            Assert.AreEqual(1, _pager.PageIndex);
        }

        [TestMethod]
        public void Past_the_first_page_it_only_stretches_and_comes_back()
        {
            Touch(TouchLocationState.Pressed, 50, 50);
            for (var x = 60; x <= 250; x += 10)
                Touch(TouchLocationState.Moved, x, 50);
            Assert.IsTrue(_pager.Offset.X < 0, "pulled past the start");

            Touch(TouchLocationState.Released, 250, 50);
            Frame(90);

            Assert.AreEqual(0, _pager.PagePosition, 0.001f);
            Assert.AreEqual(0, _pager.PageIndex);
        }

        [TestMethod]
        public void GoTo_glides_to_a_page()
        {
            _pager.GoTo(1);
            Frame(5);
            Assert.IsTrue(_pager.PagePosition > 0 && _pager.PagePosition < 1, "on the way");
            Frame(90);

            Assert.AreEqual(1, _pager.PagePosition, 0.001f);
            Assert.AreEqual(1, _pageChanges);
        }

        [TestMethod]
        public void A_page_still_scrolls_vertically()
        {
            _pager.GoTo(1, animate: false);
            Frame();
            Drag(200, 205, y: 500, toY: 200);
            Frame(5);

            Assert.IsTrue(_list.Offset.Y > 100, $"the list scrolled ({_list.Offset.Y})");
            Assert.AreEqual(1, _pager.PagePosition, 0.001f, "the pager did not move");
        }
    }
}
