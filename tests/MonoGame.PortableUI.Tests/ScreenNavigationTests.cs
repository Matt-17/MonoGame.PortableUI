using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ScreenNavigationTests
    {
        private sealed class RecordingScreen : Screen
        {
            private readonly List<string> _log;
            private readonly string _name;

            public RecordingScreen(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            protected override void OnNavigatedTo() => _log.Add(_name + ":to");

            protected override void OnNavigatedFrom() => _log.Add(_name + ":from");
        }

        [TestInitialize]
        public void Reset()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
            if (ScreenEngine.Instance != null) ScreenEngine.Instance.FocusedControl = null;
        }

        private static ScreenEngine CreateEngine(Game game)
        {
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 300);
            return engine;
        }

        private static void Tick(ScreenEngine engine, double milliseconds)
        {
            var total = ScreenSystem.TotalTime + TimeSpan.FromMilliseconds(milliseconds);
            engine.Update(new GameTime(total, TimeSpan.FromMilliseconds(milliseconds)));
        }

        [TestMethod]
        public void Overlay_keeps_the_screen_below_visible_but_only_the_top_one_is_active()
        {
            using var game = new Game();
            var engine = CreateEngine(game);
            var log = new List<string>();
            var hud = new RecordingScreen("hud", log);
            var pause = new RecordingScreen("pause", log);
            engine.NavigateToScreen(hud);

            engine.PushOverlay(pause, ScreenTransition.None);

            CollectionAssert.AreEqual(new Screen[] { hud, pause }, engine.VisibleScreens.ToArray());
            Assert.AreSame(pause, engine.ActiveScreen);

            engine.NavigateBack();
            CollectionAssert.AreEqual(new Screen[] { hud }, engine.VisibleScreens.ToArray());
        }

        [TestMethod]
        public void A_normal_navigation_hides_the_screens_below()
        {
            using var game = new Game();
            var engine = CreateEngine(game);
            var log = new List<string>();
            var first = new RecordingScreen("first", log);
            var second = new RecordingScreen("second", log);
            engine.NavigateToScreen(first);
            engine.PushOverlay(new RecordingScreen("overlay", log), ScreenTransition.None);

            engine.NavigateToScreen(second);

            CollectionAssert.AreEqual(new Screen[] { second }, engine.VisibleScreens.ToArray());
        }

        [TestMethod]
        public void Lifecycle_hooks_fire_in_navigation_order()
        {
            using var game = new Game();
            var engine = CreateEngine(game);
            var log = new List<string>();
            var a = new RecordingScreen("a", log);
            var b = new RecordingScreen("b", log);

            engine.NavigateToScreen(a);
            engine.PushOverlay(b, ScreenTransition.None);
            engine.NavigateBack();

            CollectionAssert.AreEqual(new[] { "a:to", "a:from", "b:to", "b:from", "a:to" }, log);
        }

        [TestMethod]
        public void Navigating_back_restores_the_focus_the_screen_had()
        {
            using var game = new Game();
            var engine = CreateEngine(game);
            var log = new List<string>();
            var hud = new RecordingScreen("hud", log);
            var button = new Button { Text = "Inventory" };
            hud.Content = button;
            engine.NavigateToScreen(hud);
            button.Focus();

            engine.PushOverlay(new RecordingScreen("pause", log), ScreenTransition.None);
            Assert.IsNull(ScreenEngine.Instance!.FocusedControl);

            engine.NavigateBack();
            Assert.AreSame(button, ScreenEngine.Instance!.FocusedControl);
        }

        [TestMethod]
        public void Fade_transition_animates_in_and_the_popped_screen_stays_drawn_until_it_finished()
        {
            using var game = new Game();
            var engine = CreateEngine(game);
            engine.TransitionDuration = TimeSpan.FromMilliseconds(200);
            var log = new List<string>();
            var hud = new RecordingScreen("hud", log);
            var pause = new RecordingScreen("pause", log);
            engine.NavigateToScreen(hud);

            engine.PushOverlay(pause, ScreenTransition.Fade);
            Assert.IsTrue(engine.IsTransitioning);
            Tick(engine, 100);
            Assert.IsTrue(engine.IsTransitioning);
            Tick(engine, 150);
            Assert.IsFalse(engine.IsTransitioning);

            engine.NavigateBack();
            Assert.AreSame(pause, engine.LeavingScreen, "the popped overlay fades out on top");
            Assert.AreSame(hud, engine.ActiveScreen);
            Tick(engine, 250);
            Assert.IsNull(engine.LeavingScreen);
        }
    }
}
