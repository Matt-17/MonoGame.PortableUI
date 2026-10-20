using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>#138: an application can draw its own pointer - CursorStyle.None and screen/engine cursor overrides.</summary>
    [TestClass]
    public class SoftwareCursorTests
    {
        private sealed class TestScreen : Screen
        {
            public TestScreen()
            {
                Content = new Border();
                InputSource = NullInputSource.Instance;
            }
        }

        private static (Game Game, ScreenEngine Engine, TestScreen Screen) Create(PortableTheme theme)
        {
            var game = new Game();
            var engine = ScreenEngine.CreateSurfaceEngine(game, new ScreenEngineOptions { AddComponentToGame = false, ScreenSizeMode = ScreenSizeMode.Manual, DebugOverlayKey = null, Theme = theme });
            engine.SetScreenSize(200, 100);
            var screen = new TestScreen();
            engine.NavigateToScreen(screen);
            return (game, engine, screen);
        }

        [TestMethod]
        public void The_screen_wins_over_the_engine_which_wins_over_the_theme()
        {
            var theme = new PortableTheme { Cursor = CursorStyle.TextCell() };
            var (game, engine, screen) = Create(theme);
            try
            {
                Assert.AreSame(theme.Cursor, screen.ResolveSoftwareCursor(), "the theme's (a terminal-era text cell)");

                var arrow = CursorStyle.Arrow(Color.White, Color.Black);
                engine.Options.Cursor = arrow;
                Assert.AreSame(arrow, screen.ResolveSoftwareCursor());

                var pixel = CursorStyle.PixelArrow(Color.White, Color.Black);
                screen.Cursor = pixel;
                Assert.AreSame(pixel, screen.ResolveSoftwareCursor());
            }
            finally
            {
                engine.Dispose();
                game.Dispose();
            }
        }

        [TestMethod]
        public void None_draws_no_pointer_whatever_the_theme_would_show()
        {
            var (game, engine, screen) = Create(new PortableTheme { Cursor = CursorStyle.TextCell() });
            try
            {
                engine.Options.ShowSoftwareCursor = true; // as SurfacePointerCapture.Capture does
                screen.Cursor = CursorStyle.None;
                Assert.IsNull(screen.ResolveSoftwareCursor());

                screen.Cursor = null;
                engine.Options.Cursor = CursorStyle.None;
                Assert.IsNull(screen.ResolveSoftwareCursor(), "engine-wide too");

                var theme = new PortableTheme { Cursor = CursorStyle.None };
                Assert.IsTrue(theme.Cursor.IsNone, "a theme may use it as well");
            }
            finally
            {
                engine.Dispose();
                game.Dispose();
            }
        }
    }
}
