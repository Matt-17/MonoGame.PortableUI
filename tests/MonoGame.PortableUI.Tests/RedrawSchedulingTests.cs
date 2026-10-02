using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>RenderMode.OnDemand bookkeeping: what asks for a frame, and that an idle UI stops asking.</summary>
    [TestClass]
    public class RedrawSchedulingTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private sealed class StillInputSource : IInputSource
        {
            public TouchCollection NextTouches = new TouchCollection(Array.Empty<TouchLocation>());
            public PointF MousePosition => new PointF(-1, -1);
            public IReadOnlyCollection<MouseButton> PressedMouseButtons => Array.Empty<MouseButton>();
            public int ScrollWheelValue => 0;
            public KeyboardState KeyboardState => default;
            public TouchCollection Touches
            {
                get
                {
                    var touches = NextTouches;
                    NextTouches = new TouchCollection(Array.Empty<TouchLocation>());
                    return touches;
                }
            }
        }

        [TestInitialize]
        public void Reset()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
            if (ScreenEngine.Instance != null) ScreenEngine.Instance.FocusedControl = null;
        }

        private static (ScreenEngine engine, Button button, StillInputSource input) CreateIdleUi(Game game)
        {
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, RenderMode = RenderMode.OnDemand });
            engine.SetScreenSize(300, 300);
            var button = new Button { Text = "Tap", Width = 100, Height = 50, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var input = new StillInputSource();
            engine.NavigateToScreen(new TestScreen { Content = button, InputSource = input });
            Advance(engine, 16);
            Assert.IsTrue(engine.ConsumeRedrawRequest(), "the first frame is always drawn");
            Advance(engine, 500);
            engine.ConsumeRedrawRequest();
            Advance(engine, 16);
            Assert.IsFalse(engine.ConsumeRedrawRequest(), "nothing changed: no frame");
            return (engine, button, input);
        }

        private static void Advance(ScreenEngine engine, double milliseconds)
        {
            var total = ScreenSystem.TotalTime + TimeSpan.FromMilliseconds(milliseconds);
            engine.Update(new GameTime(total, TimeSpan.FromMilliseconds(milliseconds)));
        }

        [TestMethod]
        public void A_property_change_asks_for_one_frame()
        {
            using var game = new Game();
            var (engine, button, _) = CreateIdleUi(game);

            button.Text = "Changed";
            Advance(engine, 16);
            Assert.IsTrue(engine.ConsumeRedrawRequest());
            Advance(engine, 16);
            Assert.IsFalse(engine.ConsumeRedrawRequest());
        }

        [TestMethod]
        public void A_visual_only_change_asks_for_a_frame()
        {
            using var game = new Game();
            var (engine, button, _) = CreateIdleUi(game);

            button.InvalidateLayout(false);
            Assert.IsTrue(engine.ConsumeRedrawRequest());
        }

        [TestMethod]
        public void Touch_input_draws_and_keeps_drawing_for_the_grace_period()
        {
            using var game = new Game();
            var (engine, _, input) = CreateIdleUi(game);

            input.NextTouches = new TouchCollection(new[] { new TouchLocation(1, TouchLocationState.Pressed, new Vector2(250, 250)) });
            Advance(engine, 16);
            Assert.IsTrue(engine.ConsumeRedrawRequest());
            Advance(engine, 100);
            Assert.IsTrue(engine.ConsumeRedrawRequest(), "within the grace period after input");
            Advance(engine, 300);
            Assert.IsFalse(engine.ConsumeRedrawRequest());
        }

        [TestMethod]
        public void A_running_animation_asks_for_frames_until_it_finished()
        {
            using var game = new Game();
            var (engine, button, _) = CreateIdleUi(game);

            button.Animate().FadeTo(0.5f).Duration(TimeSpan.FromMilliseconds(100)).Ease(Easings.Linear).Start();
            engine.ConsumeRedrawRequest();
            Advance(engine, 50);
            Assert.IsTrue(engine.ConsumeRedrawRequest(), "mid-animation");
            Advance(engine, 60);
            Assert.IsTrue(engine.ConsumeRedrawRequest(), "the frame the animation finished on");
            Advance(engine, 16);
            Assert.IsFalse(engine.ConsumeRedrawRequest(), "finished");
        }

        [TestMethod]
        public void A_scheduled_frame_is_drawn_when_due()
        {
            using var game = new Game();
            var (engine, _, _) = CreateIdleUi(game);

            engine.RequestRedrawAt(ScreenSystem.TotalTime + TimeSpan.FromMilliseconds(200));
            Advance(engine, 100);
            Assert.IsFalse(engine.ConsumeRedrawRequest());
            Advance(engine, 120);
            Assert.IsTrue(engine.ConsumeRedrawRequest());
            Advance(engine, 16);
            Assert.IsFalse(engine.ConsumeRedrawRequest());
        }

        [TestMethod]
        public void Animation_frame_requests_only_count_while_drawing()
        {
            using var game = new Game();
            var (engine, _, _) = CreateIdleUi(game);

            ScreenEngine.RequestAnimationFrame();
            Assert.IsFalse(engine.ConsumeRedrawRequest(), "outside a draw nobody is asked");
        }
    }
}
