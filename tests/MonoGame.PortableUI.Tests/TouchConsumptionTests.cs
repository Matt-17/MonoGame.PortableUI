using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class TouchConsumptionTests
    {
        private sealed class TestScreen : Screen
        {
        }

        /// <summary>Behaves like TouchPanel.GetState(): queued events are handed out once, later reads in
        /// the same frame see nothing.</summary>
        private sealed class ConsumingTouchSource : IInputSource
        {
            private readonly Queue<TouchCollection> _pending = new Queue<TouchCollection>();

            public void Queue(TouchLocationState state, Vector2 position)
                => _pending.Enqueue(new TouchCollection(new[] { new TouchLocation(1, state, position) }));

            public PointF MousePosition => new PointF(-1, -1);
            public IReadOnlyCollection<MouseButton> PressedMouseButtons => Array.Empty<MouseButton>();
            public int ScrollWheelValue => 0;
            public KeyboardState KeyboardState => default;
            public TouchCollection Touches => _pending.Count > 0 ? _pending.Dequeue() : new TouchCollection(Array.Empty<TouchLocation>());
            public void EndFrame() => _pending.Clear();
        }

        [TestMethod]
        public void A_tap_is_seen_when_the_touch_state_can_only_be_read_once_per_frame()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(300, 300);
            var clicks = 0;
            var button = new Button { Text = "Tap", Width = 100, Height = 50, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            button.Click += (s, e) => clicks++;
            var source = new ConsumingTouchSource();
            var screen = new TestScreen { Content = button, InputSource = source };
            engine.NavigateToScreen(screen);
            screen.Update();

            source.Queue(TouchLocationState.Pressed, new Vector2(20, 20));
            screen.Update();
            source.EndFrame();
            source.Queue(TouchLocationState.Released, new Vector2(20, 20));
            screen.Update();
            source.EndFrame();
            screen.Update();

            Assert.AreEqual(1, clicks);
        }
    }
}
