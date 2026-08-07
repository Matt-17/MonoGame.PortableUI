using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Media
{
    internal static class BrushTextureCache
    {
        private static readonly object SyncRoot = new object();
        private static readonly ConditionalWeakTable<GraphicsDevice, DeviceCache> Caches = new ConditionalWeakTable<GraphicsDevice, DeviceCache>();

        // Some entries are keyed by pixel size (rounded gradients), so animated/resizing controls
        // would grow the cache without bound. On overflow the current generation is retired and
        // only disposed on the next overflow, so textures already recorded in an unflushed
        // SpriteBatch survive the frame they were drawn in.
        private const int MaxEntries = 256;

        public static Texture2D GetOrCreate(GraphicsDevice graphicsDevice, BrushTextureCacheKey key, Func<GraphicsDevice, Texture2D> factory)
        {
            if (graphicsDevice == null)
                throw new ArgumentNullException(nameof(graphicsDevice));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            lock (SyncRoot)
            {
                var cache = Caches.GetValue(graphicsDevice, CreateCache);
                if (cache.Textures.TryGetValue(key, out var texture))
                    return texture;

                if (cache.Textures.Count >= MaxEntries)
                {
                    foreach (var retired in cache.Retired)
                        retired.Dispose();
                    cache.Retired.Clear();
                    cache.Retired.AddRange(cache.Textures.Values);
                    cache.Textures.Clear();
                }

                texture = factory(graphicsDevice);
                cache.Textures[key] = texture;
                return texture;
            }
        }

        internal static void Clear(GraphicsDevice graphicsDevice)
        {
            lock (SyncRoot)
            {
                if (Caches.TryGetValue(graphicsDevice, out var cache))
                    Clear(cache);
            }
        }

        private static DeviceCache CreateCache(GraphicsDevice graphicsDevice)
        {
            var cache = new DeviceCache();
            graphicsDevice.DeviceReset += (_, _) => Clear(cache);
            graphicsDevice.Disposing += (_, _) => Clear(cache);
            return cache;
        }

        private static void Clear(DeviceCache cache)
        {
            foreach (var texture in cache.Textures.Values)
                texture.Dispose();
            cache.Textures.Clear();
            foreach (var texture in cache.Retired)
                texture.Dispose();
            cache.Retired.Clear();
        }

        private sealed class DeviceCache
        {
            public Dictionary<BrushTextureCacheKey, Texture2D> Textures { get; } = new Dictionary<BrushTextureCacheKey, Texture2D>();
            public List<Texture2D> Retired { get; } = new List<Texture2D>();
        }
    }

    internal readonly struct BrushTextureCacheKey : IEquatable<BrushTextureCacheKey>
    {
        public BrushTextureCacheKey(string kind, int first, int second = 0, int third = 0, int fourth = 0)
        {
            Kind = kind ?? "";
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
        }

        public string Kind { get; }

        public int First { get; }

        public int Second { get; }

        public int Third { get; }

        public int Fourth { get; }

        public bool Equals(BrushTextureCacheKey other)
        {
            return Kind == other.Kind
                && First == other.First
                && Second == other.Second
                && Third == other.Third
                && Fourth == other.Fourth;
        }

        public override bool Equals(object? obj)
        {
            return obj is BrushTextureCacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, First, Second, Third, Fourth);
        }
    }
}
