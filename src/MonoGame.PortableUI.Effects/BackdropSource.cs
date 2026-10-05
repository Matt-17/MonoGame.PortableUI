using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Effects
{
    /// <summary>
    ///     Publishes the blurred backdrop of the frame that is currently being drawn so glass brushes
    ///     (<see cref="Media.FrostedGlassBrush"/> and derived) can sample it with screen-space coordinates.
    ///     Set by <see cref="Screen"/> at the start of a frame when any visible brush requires a backdrop.
    /// </summary>
    public static class BackdropSource
    {
        private static readonly Dictionary<GraphicsDevice, Entry> Entries = new Dictionary<GraphicsDevice, Entry>();

        internal static void Set(GraphicsDevice device, Texture2D texture, Rect screenRect, Texture2D? sharp = null)
        {
            Entries[device] = new Entry(texture, screenRect, sharp);
        }

        /// <summary>
        ///     The unblurred backdrop of the current frame (same screen mapping as the blurred one), for
        ///     brushes that refract the scene rather than frost it (<see cref="Media.LiquidGlassBrush"/>).
        /// </summary>
        public static bool TryGetSharp(GraphicsDevice? device, out Texture2D? texture, out Texture2D? blurred, out Rect screenRect)
        {
            texture = null;
            blurred = null;
            screenRect = Rect.Empty;
            if (device == null || !Entries.TryGetValue(device, out var entry) || entry.Sharp == null || entry.Sharp.IsDisposed || entry.Texture.IsDisposed)
                return false;

            texture = entry.Sharp;
            blurred = entry.Texture;
            screenRect = entry.ScreenRect;
            return true;
        }

        internal static void Clear(GraphicsDevice device)
        {
            Entries.Remove(device);
        }

        public static bool TryGet(GraphicsDevice? device, out Texture2D? texture, out Rect screenRect)
        {
            texture = null;
            screenRect = Rect.Empty;
            if (device == null || !Entries.TryGetValue(device, out var entry) || entry.Texture.IsDisposed)
                return false;

            texture = entry.Texture;
            screenRect = entry.ScreenRect;
            return true;
        }

        private readonly struct Entry
        {
            public Entry(Texture2D texture, Rect screenRect, Texture2D? sharp)
            {
                Texture = texture;
                ScreenRect = screenRect;
                Sharp = sharp;
            }

            public Texture2D? Sharp { get; }

            public Texture2D Texture { get; }

            public Rect ScreenRect { get; }
        }
    }
}
