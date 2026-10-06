using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>#102: surfaces share one layer-cache budget, and surfaces in view evict the caches of
    /// surfaces that were not drawn for the longest time (an unseen surface never ages its own).</summary>
    [TestClass]
    [DoNotParallelize] // UISurface.SharedLayerCacheBudgetPixels is process-wide
    public class SharedLayerBudgetTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private long _previousBudget;

        [TestInitialize]
        public void Remember() => _previousBudget = UISurface.SharedLayerCacheBudgetPixels;

        [TestCleanup]
        public void Restore() => UISurface.SharedLayerCacheBudgetPixels = _previousBudget;

        /// <summary>Registers a cache of <paramref name="pixels"/> for a new control, as RenderLayer does.</summary>
        private static (Control Owner, LayerCache Cache) Cache(ScreenEngine engine, int pixels, long tick)
        {
            var control = new Border { CacheMode = CacheMode.Bitmap };
            var cache = engine.AcquireLayerCache(control);
            Assert.IsTrue(engine.LayerCacheFits(cache, pixels));
            engine.TrackLayerCachePixels(pixels - cache.Pixels);
            cache.Width = pixels;
            cache.Height = 1;
            cache.LastUsedTick = tick;
            return (control, cache);
        }

        [TestMethod]
        public void Surfaces_count_against_one_shared_budget()
        {
            using var game = new Game();
            UISurface.SharedLayerCacheBudgetPixels = 1000;
            using var a = new UISurface(game, new TestScreen(), 320, 200);
            using var b = new UISurface(game, new TestScreen(), 320, 200);

            Cache(a.Engine, 600, tick: a.Engine.LayerCacheTick);

            Assert.AreEqual(1000, b.Engine.LayerCacheBudgetPixels);
            Assert.AreEqual(600, b.Engine.LayerCacheBudgetUsedPixels, "a's pixels count for b too");
        }

        [TestMethod]
        public void A_surface_in_view_evicts_the_least_recently_drawn_caches_of_others()
        {
            using var game = new Game();
            UISurface.SharedLayerCacheBudgetPixels = 1000;
            using var unseen = new UISurface(game, new TestScreen(), 320, 200);
            using var older = new UISurface(game, new TestScreen(), 320, 200);
            using var seen = new UISurface(game, new TestScreen(), 320, 200);
            var oldest = Cache(unseen.Engine, 400, tick: -10);
            var old = Cache(older.Engine, 400, tick: -5);

            var fresh = new Border { CacheMode = CacheMode.Bitmap };
            var cache = seen.Engine.AcquireLayerCache(fresh);
            Assert.IsTrue(seen.Engine.LayerCacheFits(cache, 500), "room is made");

            Assert.IsNull(oldest.Owner.LayerCache, "the least recently drawn cache went first");
            Assert.IsNotNull(old.Owner.LayerCache, "and only as many as needed");
            Assert.AreEqual(400, seen.Engine.LayerCacheBudgetUsedPixels);
        }

        [TestMethod]
        public void Caches_drawn_in_the_current_tick_are_never_evicted()
        {
            using var game = new Game();
            UISurface.SharedLayerCacheBudgetPixels = 1000;
            using var other = new UISurface(game, new TestScreen(), 320, 200);
            using var seen = new UISurface(game, new TestScreen(), 320, 200);
            var onScreen = Cache(other.Engine, 800, tick: other.Engine.LayerCacheTick);

            var cache = seen.Engine.AcquireLayerCache(new Border { CacheMode = CacheMode.Bitmap });

            Assert.IsFalse(seen.Engine.LayerCacheFits(cache, 500), "the control draws live instead");
            Assert.IsNotNull(onScreen.Owner.LayerCache);
        }

        [TestMethod]
        public void A_disposed_surface_gives_its_pixels_back()
        {
            using var game = new Game();
            UISurface.SharedLayerCacheBudgetPixels = 1000;
            var gone = new UISurface(game, new TestScreen(), 320, 200);
            using var stays = new UISurface(game, new TestScreen(), 320, 200);
            Cache(gone.Engine, 700, tick: 0);

            gone.Dispose();

            Assert.AreEqual(0, stays.Engine.LayerCacheBudgetUsedPixels);
        }

        [TestMethod]
        public void The_main_engine_keeps_its_own_budget()
        {
            using var game = new Game();
            UISurface.SharedLayerCacheBudgetPixels = 1000;
            var main = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            main.SetScreenSize(800, 600);
            using var surface = new UISurface(game, new TestScreen(), 320, 200);
            Cache(surface.Engine, 900, tick: 0);

            Assert.AreEqual(1920L * 1080L * 3, main.LayerCacheBudgetPixels);
            Assert.AreEqual(0, main.LayerCacheBudgetUsedPixels);
            main.Dispose();
        }
    }
}
