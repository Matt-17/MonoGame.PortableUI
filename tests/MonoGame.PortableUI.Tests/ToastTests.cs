using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ToastTests
    {
        private sealed class TestScreen : Screen
        {
        }

        [TestInitialize]
        public void Reset()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
            ScreenEngine.FocusedControl = null;
        }

        private static ScreenEngine CreateEngine(Game game)
        {
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 300);
            engine.Toasts.AnimationDuration = TimeSpan.Zero;
            return engine;
        }

        private static void Advance(ScreenEngine engine, double milliseconds)
        {
            var total = ScreenSystem.TotalTime + TimeSpan.FromMilliseconds(milliseconds);
            engine.Update(new GameTime(total, TimeSpan.FromMilliseconds(milliseconds)));
        }

        [TestMethod]
        public void Messages_are_shown_one_after_another_in_order()
        {
            using var game = new Game();
            var engine = CreateEngine(game);
            var shown = new List<string>();
            engine.Toasts.Shown += (_, message) => shown.Add(message);

            engine.Toasts.Show("first");
            engine.Toasts.Show("second");
            engine.Toasts.Show("third");

            Advance(engine, 1);
            Assert.AreEqual("first", engine.Toasts.CurrentMessage);
            Assert.AreEqual(2, engine.Toasts.QueuedCount);

            Advance(engine, 2100); // short duration (2 s) elapsed
            Advance(engine, 1);
            Assert.AreEqual("second", engine.Toasts.CurrentMessage);

            Advance(engine, 2100);
            Advance(engine, 1);
            CollectionAssert.AreEqual(new[] { "first", "second", "third" }, shown);
        }

        [TestMethod]
        public void Long_messages_stay_longer_than_short_ones()
        {
            using var game = new Game();
            var engine = CreateEngine(game);

            engine.Toasts.Show("long", ToastDuration.Long);
            Advance(engine, 1);
            Advance(engine, 2500);

            Assert.AreEqual("long", engine.Toasts.CurrentMessage, "a long toast outlives the short duration");
            Advance(engine, 1200);
            Assert.IsNull(engine.Toasts.CurrentMessage);
        }

        [TestMethod]
        public void A_flood_is_capped_and_drops_the_oldest_waiting_message()
        {
            using var game = new Game();
            var engine = CreateEngine(game);
            engine.Toasts.MaxQueued = 2;

            engine.Toasts.Show("1");
            Advance(engine, 1); // "1" is now visible
            engine.Toasts.Show("2");
            engine.Toasts.Show("3");
            engine.Toasts.Show("4");

            Assert.AreEqual(2, engine.Toasts.QueuedCount);
            var shown = new List<string>();
            engine.Toasts.Shown += (_, message) => shown.Add(message);
            for (var i = 0; i < 3; i++)
            {
                Advance(engine, 2100);
                Advance(engine, 1);
            }
            CollectionAssert.AreEqual(new[] { "3", "4" }, shown);
        }

        [TestMethod]
        public void Drop_newest_policy_ignores_messages_beyond_the_cap()
        {
            using var game = new Game();
            var engine = CreateEngine(game);
            engine.Toasts.MaxQueued = 1;
            engine.Toasts.Overflow = ToastOverflowPolicy.DropNewest;

            engine.Toasts.Show("kept");
            engine.Toasts.Show("dropped");

            Advance(engine, 1);
            Assert.AreEqual("kept", engine.Toasts.CurrentMessage);
            Assert.AreEqual(0, engine.Toasts.QueuedCount);
        }

        [TestMethod]
        public void Input_reaches_the_screen_while_a_toast_is_visible_and_toasts_survive_navigation()
        {
            using var game = new Game();
            var engine = CreateEngine(game);
            var screen = new TestScreen();
            var source = new VirtualInputSource();
            screen.InputSource = source;
            var button = new Button { Text = "Under", HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            var clicks = 0;
            button.Click += (_, _) => clicks++;
            screen.Content = button;
            engine.NavigateToScreen(screen);
            engine.Toasts.Show("Saved");
            Advance(engine, 1);

            var underToast = new PointF(200, 270);
            source.SetPointer(underToast);
            Advance(engine, 16);
            source.SetPointer(underToast, leftDown: true);
            Advance(engine, 16);
            source.SetPointer(underToast);
            Advance(engine, 16);

            Assert.AreEqual(1, clicks);
            engine.NavigateToScreen(new TestScreen());
            Assert.AreEqual("Saved", engine.Toasts.CurrentMessage);

            engine.Toasts.Clear();
            Assert.IsNull(engine.Toasts.CurrentMessage);
        }
    }
}
