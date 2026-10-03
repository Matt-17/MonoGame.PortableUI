using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>CacheMode.Bitmap invalidation: every visual change inside a cached subtree must mark it dirty.</summary>
    [TestClass]
    public class LayerCacheTests
    {
        private sealed class TestScreen : Screen
        {
        }

        [TestInitialize]
        public void Reset()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
        }

        private static (Border cached, Button button, LayerCache cache) CreateCachedTree()
        {
            var button = new Button { Text = "A", Width = 80, Height = 40 };
            var cached = new Border { Content = button, CacheMode = CacheMode.Bitmap };
            var cache = new LayerCache { Dirty = false };
            cached.LayerCache = cache;
            return (cached, button, cache);
        }

        [TestMethod]
        public void A_property_change_deep_inside_marks_the_layer_dirty()
        {
            var (_, button, cache) = CreateCachedTree();
            button.Text = "B";
            Assert.IsTrue(cache.Dirty);
        }

        [TestMethod]
        public void Hover_and_touch_state_changes_mark_the_layer_dirty()
        {
            var (_, button, cache) = CreateCachedTree();
            button.OnMouseEnter(new MouseEventArgs(new PointF(1, 1), new System.Collections.Generic.List<MouseButton>()));
            Assert.IsTrue(cache.Dirty, "hover");

            cache.Dirty = false;
            button.OnTouchDown(new TouchEventArgs(new PointF(1, 1)));
            Assert.IsTrue(cache.Dirty, "pressed");
        }

        [TestMethod]
        public void Opacity_scale_and_translation_changes_mark_the_layer_dirty()
        {
            var (_, button, cache) = CreateCachedTree();
            button.Opacity = 0.5;
            Assert.IsTrue(cache.Dirty, "opacity");
            cache.Dirty = false;
            button.Scale = new Vector2(2, 2);
            Assert.IsTrue(cache.Dirty, "scale");
            cache.Dirty = false;
            button.Translation = new Vector2(3, 0);
            Assert.IsTrue(cache.Dirty, "translation");
            cache.Dirty = false;
            button.Translation = new Vector2(3, 0);
            Assert.IsFalse(cache.Dirty, "setting the same value is no change");
        }

        [TestMethod]
        public void Focus_changes_mark_the_layer_dirty()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            var (cached, button, cache) = CreateCachedTree();
            engine.NavigateToScreen(new TestScreen { Content = cached });
            cache.Dirty = false;

            engine.FocusedControl = button;
            Assert.IsTrue(cache.Dirty, "got focus");
            cache.Dirty = false;
            engine.KeyboardNavigationActive = !engine.KeyboardNavigationActive;
            Assert.IsTrue(cache.Dirty, "focus ring visibility follows the navigation mode");
        }

        [TestMethod]
        public void Scrolling_marks_cached_ancestors_dirty_but_not_cached_content()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(200, 200);
            var inner = new Border { Height = 600, CacheMode = CacheMode.Bitmap };
            var viewer = new ScrollViewer { Content = inner, Height = 200 };
            var outer = new Border { Content = viewer, CacheMode = CacheMode.Bitmap };
            var screen = new TestScreen { Content = outer };
            engine.NavigateToScreen(screen);
            screen.PerformLayoutIfDirty();
            var innerCache = new LayerCache { Dirty = false };
            var outerCache = new LayerCache { Dirty = false };
            inner.LayerCache = innerCache;
            outer.LayerCache = outerCache;

            viewer.ScrollTo(new PointF(0, 100));

            Assert.IsTrue(outerCache.Dirty, "the viewer's picture changed");
            Assert.IsFalse(innerCache.Dirty, "the scrolled content itself did not: it is only moved");
        }

        [TestMethod]
        public void Global_changes_bump_the_cache_generation()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, RenderQuality = RenderQuality.High });
            engine.UpdateRenderQuality();
            var generation = engine.LayerCacheGeneration;

            engine.Options.RenderQuality = RenderQuality.Low;
            engine.UpdateRenderQuality();

            Assert.AreNotEqual(generation, engine.LayerCacheGeneration);
        }
    }
}
