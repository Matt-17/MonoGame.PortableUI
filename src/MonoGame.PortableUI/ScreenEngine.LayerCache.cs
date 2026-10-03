using System;
using System.Collections.Generic;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI
{
    // Registry and budget of the CacheMode.Bitmap textures this engine draws.
    public partial class ScreenEngine
    {
        /// <summary>Caches not drawn for this many frames are released (scrolled away, removed, hidden).</summary>
        internal const int LayerCacheIdleFrames = 120;

        private readonly List<Control> _layerCacheOwners = new List<Control>();
        private long _layerCachePixels;

        /// <summary>Bumped by changes that invalidate every cached layer at once (theme, text scale,
        /// localization, render quality, lost surface, viewport size).</summary>
        internal int LayerCacheGeneration { get; private set; }

        /// <summary>Counts <see cref="DrawStack"/> calls, for cache aging.</summary>
        internal long DrawFrameNumber { get; private set; }

        /// <summary><see cref="RequestAnimationFrame"/> calls so far: a layer whose rendering raised it shows
        /// something animated and must be drawn again next frame.</summary>
        internal int AnimationFrameRequests;

        /// <summary>Earliest <see cref="RequestAnimationFrameAt"/> time raised in the current layer scope.</summary>
        internal TimeSpan EarliestAnimationFrameAt = TimeSpan.MaxValue;

        /// <summary>Total cached pixels allowed: three screens' worth.</summary>
        internal long LayerCacheBudgetPixels
        {
            get
            {
                var pixels = (long)(ScreenRect.Width * RenderScale) * (long)(ScreenRect.Height * RenderScale);
                return Math.Max(pixels, 1920L * 1080L) * 3;
            }
        }

        /// <summary>Drops every cached layer's content (re-rendered on next use).</summary>
        internal void InvalidateLayerCaches() => LayerCacheGeneration++;

        /// <summary>The cache of <paramref name="control"/>, registered for aging and disposal.</summary>
        internal LayerCache AcquireLayerCache(Control control)
        {
            if (control.LayerCache == null)
            {
                control.LayerCache = new LayerCache();
                _layerCacheOwners.Add(control);
            }
            return control.LayerCache;
        }

        /// <summary>Whether a texture of <paramref name="pixels"/> fits the budget next to the others.</summary>
        internal bool LayerCacheFits(LayerCache cache, long pixels)
            => _layerCachePixels - cache.Pixels + pixels <= LayerCacheBudgetPixels;

        internal void TrackLayerCachePixels(long delta) => _layerCachePixels += delta;

        /// <summary>Releases caches that were not drawn recently or whose control no longer wants one.</summary>
        private void TrimLayerCaches()
        {
            DrawFrameNumber++;
            for (var i = _layerCacheOwners.Count - 1; i >= 0; i--)
            {
                var control = _layerCacheOwners[i];
                var cache = control.LayerCache;
                if (cache != null && control.CacheMode == CacheMode.Bitmap
                    && DrawFrameNumber - cache.LastUsedFrame <= LayerCacheIdleFrames)
                    continue;
                if (cache != null)
                {
                    _layerCachePixels -= cache.Pixels;
                    cache.Dispose();
                }
                control.LayerCache = null;
                _layerCacheOwners.RemoveAt(i);
            }
        }

        private void DisposeLayerCaches()
        {
            foreach (var control in _layerCacheOwners)
            {
                control.LayerCache?.Dispose();
                control.LayerCache = null;
            }
            _layerCacheOwners.Clear();
            _layerCachePixels = 0;
        }
    }
}
