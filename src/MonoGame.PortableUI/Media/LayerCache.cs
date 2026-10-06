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

        /// <summary>Re-renders in consecutive frames (animation, scrolling inside, anything that changes
        /// every frame). Re-rendering every frame costs more than drawing live, so after a few the
        /// control is drawn live for <see cref="LiveSpan"/> frames.</summary>
        public int ConsecutiveRebuilds;

        public long LastRebuildFrame = long.MinValue;

        /// <summary>Length of the next live period; doubles while the content keeps changing.</summary>
        public int LiveSpan;

        /// <summary>Frames composited from a valid texture in a row; a stable stretch resets <see cref="LiveSpan"/>.</summary>
        public int ValidStreak;

        public long LiveUntilFrame;
        public long LastUsedFrame;

        /// <summary>Tick of the shared surface budget when last drawn (comparable across surface
        /// engines, unlike <see cref="LastUsedFrame"/>), for least-recently-used eviction.</summary>
        public long LastUsedTick;

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
