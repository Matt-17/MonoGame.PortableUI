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
        }
    }
}
#endif
