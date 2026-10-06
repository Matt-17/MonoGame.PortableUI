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

        /// <summary>Total cached pixels allowed: three screens' worth, or for surface engines the
        /// budget they share (<see cref="UISurface.SharedLayerCacheBudgetPixels"/>).</summary>
        internal long LayerCacheBudgetPixels
        {
            get
            {
                if (_sharedLayerBudget != null)
                    return UISurface.SharedLayerCacheBudgetPixels;
                var pixels = (long)(ScreenRect.Width * RenderScale) * (long)(ScreenRect.Height * RenderScale);
                return Math.Max(pixels, 1920L * 1080L) * 3;
            }
        }

        /// <summary>
        ///     The layer-cache budget all surface engines of one game share. A surface only ages its caches
        ///     while it is drawn, so an unseen one would keep them for good; with one shared budget the
        ///     surfaces in view take the space back from those not drawn for the longest time.
        /// </summary>
        private sealed class SharedLayerBudget
        {
            public readonly List<ScreenEngine> Members = new List<ScreenEngine>();
            public long Pixels;
            public long Tick;
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Microsoft.Xna.Framework.Game, SharedLayerBudget> SharedLayerBudgets = new();

        private SharedLayerBudget? _sharedLayerBudget;

        /// <summary>Global draw tick for least-recently-used eviction across engines (this engine's own
        /// frame counter when it has no shared budget).</summary>
        internal long LayerCacheTick => _sharedLayerBudget?.Tick ?? DrawFrameNumber;

        private void JoinSharedLayerBudget()
        {
            _sharedLayerBudget = SharedLayerBudgets.GetValue(Game, static _ => new SharedLayerBudget());
            _sharedLayerBudget.Members.Add(this);
        }

        private void LeaveSharedLayerBudget()
        {
            _sharedLayerBudget?.Members.Remove(this);
            _sharedLayerBudget = null;
        }

        /// <summary>Cached pixels counted against this engine's budget (shared or own).</summary>
        internal long LayerCacheBudgetUsedPixels => _sharedLayerBudget?.Pixels ?? _layerCachePixels;

        /// <summary>Drops every cached layer's content (re-rendered on next use).</summary>
        internal void InvalidateLayerCaches() => LayerCacheGeneration++;

        private bool _layerCachesStaleAfterUpdate;

        /// <summary>
        ///     A press, release, key or text input, or a game-thread action happened: app code may have
        ///     changed properties whose setters do not invalidate, so every cached layer is re-rendered
        ///     once after this update. Pointer moves (scrolling) do not count.
        /// </summary>
        internal void NoteDiscreteInput() => _layerCachesStaleAfterUpdate = true;

        private void ApplyDiscreteInputToLayerCaches()
        {
            if (!_layerCachesStaleAfterUpdate)
                return;
            _layerCachesStaleAfterUpdate = false;
            InvalidateLayerCaches();
        }

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

        /// <summary>Whether a texture of <paramref name="pixels"/> fits the budget next to the others.
        /// With a shared budget, caches of any surface not drawn this tick are evicted, least recently
        /// used first, until it fits.</summary>
        internal bool LayerCacheFits(LayerCache cache, long pixels)
        {
            var budget = LayerCacheBudgetPixels;
            if (LayerCacheBudgetUsedPixels - cache.Pixels + pixels <= budget)
                return true;
            if (_sharedLayerBudget is not { } shared)
                return false;
            while (shared.Pixels - cache.Pixels + pixels > budget)
            {
                if (!EvictLeastRecentlyUsed(shared, cache))
                    return false;
            }
            return true;
        }

        private static bool EvictLeastRecentlyUsed(SharedLayerBudget shared, LayerCache keep)
        {
            ScreenEngine? oldestEngine = null;
            var oldestIndex = -1;
            var oldestTick = shared.Tick;
            foreach (var engine in shared.Members)
            {
                var owners = engine._layerCacheOwners;
                for (var i = 0; i < owners.Count; i++)
                {
                    var cache = owners[i].LayerCache;
                    // Never what is on screen in this tick, never the cache being made room for.
                    if (cache == null || ReferenceEquals(cache, keep) || cache.Pixels == 0 || cache.LastUsedTick >= oldestTick)
                        continue;
                    oldestEngine = engine;
                    oldestIndex = i;
                    oldestTick = cache.LastUsedTick;
                }
            }
            if (oldestEngine == null)
                return false;
            oldestEngine.ReleaseLayerCacheAt(oldestIndex);
            return true;
        }

        internal void TrackLayerCachePixels(long delta)
        {
            _layerCachePixels += delta;
            if (_sharedLayerBudget != null)
                _sharedLayerBudget.Pixels += delta;
        }

        private void ReleaseLayerCacheAt(int index)
        {
            var control = _layerCacheOwners[index];
            if (control.LayerCache is { } cache)
            {
                TrackLayerCachePixels(-cache.Pixels);
                cache.Dispose();
            }
            control.LayerCache = null;
            _layerCacheOwners.RemoveAt(index);
        }

        /// <summary>Advances the shared tick at the start of a draw of this engine.</summary>
        private void AdvanceLayerCacheTick()
        {
            if (_sharedLayerBudget != null)
                _sharedLayerBudget.Tick++;
        }

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
                ReleaseLayerCacheAt(i);
            }
        }

        private void DisposeLayerCaches()
        {
            for (var i = _layerCacheOwners.Count - 1; i >= 0; i--)
                ReleaseLayerCacheAt(i);
            _layerCachePixels = 0;
            LeaveSharedLayerBudget();
        }
    }
}
