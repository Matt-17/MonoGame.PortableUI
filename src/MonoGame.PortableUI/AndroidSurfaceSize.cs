#if ANDROID
using System;
using Android.Views;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;

namespace MonoGame.PortableUI
{
    /// <summary>
    ///     Keeps MonoGame's back buffer equal to the game view's real size on Android.
    /// </summary>
    /// <remarks>
    ///     MonoGame letter-boxes the preferred back buffer into the view (its <c>DisplayMode</c>): a
    ///     preferred size with another aspect ratio than the view yields a viewport with a negative
    ///     offset (e.g. <c>Viewport(0, -134, 1080, 2072)</c> on an edge-to-edge window that is 2340
    ///     high), so drawing shifts while scissor rectangles do not and clipped text disappears.
    ///     The view's size is only right once it is laid out and changes with edge-to-edge, rotation,
    ///     split screen or system bars, so the back buffer follows it here. Call
    ///     <see cref="Follow"/> from <c>Game.Initialize</c>.
    /// </remarks>
    public static class AndroidSurfaceSize
    {
        private static bool IsWindowFullScreen(View view)
            => (view.Context as Android.App.Activity)?.Window?.Attributes is { } attributes
                && (attributes.Flags & WindowManagerFlags.Fullscreen) != 0;

        public static void Follow(View gameView, GraphicsDeviceManager graphics, ScreenEngine engine)
        {
            if (gameView == null)
                throw new ArgumentNullException(nameof(gameView));
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));

            void Apply(int width, int height)
            {
                if (width <= 0 || height <= 0)
                    return;
                if (graphics.PreferredBackBufferWidth == width && graphics.PreferredBackBufferHeight == height)
                    return;
                graphics.PreferredBackBufferWidth = width;
                graphics.PreferredBackBufferHeight = height;
                graphics.ApplyChanges();
                TouchPanel.DisplayWidth = width;
                TouchPanel.DisplayHeight = height;
            }

            // Already laid out (the usual case by Game.Initialize, which runs on the game thread):
            // no LayoutChange follows, so apply the current size right away.
            Apply(gameView.Width, gameView.Height);

            gameView.LayoutChange += (_, e) =>
            {
                var width = e.Right - e.Left;
                var height = e.Bottom - e.Top;
                engine.InvokeOnGameThread(() => Apply(width, height));
            };

            // MonoGame's Android presentation parameters say IsFullScreen = true ("full screen as
            // default") even while the window is not full screen, and AndroidGameActivity.OnResume
            // applies that as WindowManager FLAG_FULLSCREEN - which its non-fullscreen branch never
            // clears again: the status bar was gone after every resume. The window's own flag before
            // the pause is the real state; write it into the parameters OnResume reads. Clearing the
            // flag afterwards instead would make Android show the bars again over an app's own
            // immersive mode.
            var fullScreenBeforePause = false;
            AndroidGameActivity.Paused += (_, _) =>
            {
                fullScreenBeforePause = IsWindowFullScreen(gameView);
                if (graphics.GraphicsDevice?.PresentationParameters is { } parameters)
                    parameters.IsFullScreen = fullScreenBeforePause;
            };
            AndroidGameActivity.Resumed += (_, _) => gameView.Post(() =>
            {
                // Fallback if the flag was set anyway (e.g. parameters recreated before OnResume).
                if (fullScreenBeforePause || !IsWindowFullScreen(gameView) || (gameView.Context as Android.App.Activity)?.Window is not { } window)
                    return;
                window.ClearFlags(WindowManagerFlags.Fullscreen);
            });
        }
    }
}
#endif
