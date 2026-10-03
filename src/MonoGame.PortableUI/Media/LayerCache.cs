using System;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    ///     The texture behind <see cref="Controls.Control.CacheMode"/>: a control's subtree drawn once and
    ///     reused while it stays valid. Owned and evicted by the <see cref="ScreenEngine"/> that draws it.
    /// </summary>
    internal sealed class LayerCache : IDisposable
    {
        /// <summary>Set by any visual invalidation inside the subtree (the InvalidateLayout bubble path).</summary>
        public bool Dirty = true;

        public RenderTarget2D? Target;

        /// <summary><see cref="ScreenEngine.LayerCacheGeneration"/> the texture was drawn in; a global
        /// change (theme, text scale, quality, lost surface) bumps it.</summary>
        public int Generation = -1;

        public int Width;
        public int Height;

        /// <summary>Where the subtree's bounds start relative to the control's render rect: while the
        /// cache is valid nothing inside changed, so the bounds follow the control without a walk.</summary>
        public float OffsetX;
        public float OffsetY;
        public float ScaleX;
        public float ScaleY;

        /// <summary>A visual inside asked for a frame at this time (caret blink): re-render then.</summary>
        public TimeSpan RefreshAt = TimeSpan.MaxValue;

        /// <summary>Consecutive re-renders forced by animation requests; caching animated content only
        /// costs, so after a few the control is drawn live for a while.</summary>
        public int AnimatedRebuilds;

        public long LiveUntilFrame;
        public long LastUsedFrame;

        public long Pixels => (long)Width * Height;

        public void Dispose()
        {
            Target?.Dispose();
            Target = null;
            Width = Height = 0;
            Dirty = true;
        }
    }
}
