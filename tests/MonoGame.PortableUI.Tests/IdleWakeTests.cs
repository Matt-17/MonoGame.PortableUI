using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>Thread-safe touch queue (Android input bridge) and waking the idle on-demand loop.</summary>
    [TestClass]
    public class IdleWakeTests
    {
        private static TouchLocation Single(TouchCollection touches)
        {
            Assert.AreEqual(1, touches.Count);
            return touches[0];
        }

        [TestMethod]
        public void A_held_touch_is_pressed_once_then_moved_then_released_once()
        {
            var queue = new TouchEventQueue();
            queue.Add(TouchEventQueue.Kind.Down, 7, new Vector2(10, 20));
            Assert.AreEqual(TouchLocationState.Pressed, Single(queue.Read()).State);

            Assert.AreEqual(TouchLocationState.Moved, Single(queue.Read()).State, "held without moving still reports Moved");

            queue.Add(TouchEventQueue.Kind.Move, 7, new Vector2(15, 25));
            var moved = Single(queue.Read());
            Assert.AreEqual(TouchLocationState.Moved, moved.State);
            Assert.AreEqual(new Vector2(15, 25), moved.Position);

            queue.Add(TouchEventQueue.Kind.Up, 7, new Vector2(16, 26));
            Assert.AreEqual(TouchLocationState.Released, Single(queue.Read()).State);
            Assert.AreEqual(0, queue.Read().Count);
        }

        [TestMethod]
        public void A_tap_between_two_frames_is_pressed_then_released_next_frame()
        {
            var queue = new TouchEventQueue();
            queue.Add(TouchEventQueue.Kind.Down, 1, new Vector2(5, 5));
            queue.Add(TouchEventQueue.Kind.Up, 1, new Vector2(5, 5));

            Assert.AreEqual(TouchLocationState.Pressed, Single(queue.Read()).State);
            Assert.AreEqual(TouchLocationState.Released, Single(queue.Read()).State);
            Assert.AreEqual(0, queue.Read().Count);
        }

        [TestMethod]
        public void The_first_finger_stays_the_primary_touch()
        {
            var queue = new TouchEventQueue();
            queue.Add(TouchEventQueue.Kind.Down, 3, new Vector2(1, 1));
            queue.Read();
            queue.Add(TouchEventQueue.Kind.Down, 0, new Vector2(2, 2));
            var touches = queue.Read();

            Assert.AreEqual(2, touches.Count);
            Assert.AreEqual(3, touches[0].Id, "insertion order, not id order");
            Assert.AreEqual(TouchLocationState.Pressed, touches[1].State);
        }

        [TestMethod]
        public void Events_from_another_thread_are_all_delivered()
        {
            var queue = new TouchEventQueue();
            Parallel.For(0, 200, i =>
            {
                queue.Add(TouchEventQueue.Kind.Down, i, new Vector2(i, i));
            });
            Assert.AreEqual(200, queue.Read().Count);
        }

        [TestMethod]
        public void WakeUp_ends_an_idle_wait_early()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.WaitIdle(TimeSpan.FromMilliseconds(1)); // drop a stale signal

            var watch = Stopwatch.StartNew();
            var waker = Task.Run(async () =>
            {
                await Task.Delay(50);
                engine.WakeUp();
            });
            engine.WaitIdle(TimeSpan.FromSeconds(5));
            waker.Wait();

            Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(2), $"woke after {watch.ElapsedMilliseconds} ms");
        }

        [TestMethod]
        public void A_redraw_request_or_game_thread_action_from_another_thread_wakes_the_loop()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.Update(new GameTime(TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16))); // marks the game thread
            engine.WaitIdle(TimeSpan.FromMilliseconds(1));

            Task.Run(() => engine.RequestRedraw()).Wait();
            var watch = Stopwatch.StartNew();
            engine.WaitIdle(TimeSpan.FromSeconds(5));
            Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(2), "RequestRedraw from another thread wakes");

            Task.Run(() => engine.InvokeOnGameThread(() => { })).Wait();
            watch.Restart();
            engine.WaitIdle(TimeSpan.FromSeconds(5));
            Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(2), "InvokeOnGameThread wakes");
        }
    }
}
