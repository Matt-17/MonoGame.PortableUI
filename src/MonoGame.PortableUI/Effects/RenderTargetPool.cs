using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Effects
{
    /// <summary>
    ///     PreserveContents render targets shared per device and size, rented for one draw and returned:
    ///     many surface engines (an arcade hall of UISurfaces) draw one after another and need their
    ///     offscreen targets only while they draw. A draw nested in another (a surface drawn while one
    ///     draws) rents a second target instead of overwriting the first.
    /// </summary>
    internal static class RenderTargetPool
    {
        private static readonly ConditionalWeakTable<GraphicsDevice, Dictionary<(int, int), Stack<RenderTarget2D>>> Pools = new();

        public static RenderTarget2D Rent(GraphicsDevice device, int width, int height)
        {
            width = width < 1 ? 1 : width;
            height = height < 1 ? 1 : height;
            var pool = Pool(device, width, height);
            while (pool.Count > 0)
            {
                var target = pool.Pop();
                if (!target.IsDisposed)
                    return target;
            }
            // PreserveContents: offscreen passes switch targets mid-frame and come back.
            return new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }

        public static void Return(GraphicsDevice device, RenderTarget2D target)
        {
            if (!target.IsDisposed && ReferenceEquals(target.GraphicsDevice, device))
                Pool(device, target.Width, target.Height).Push(target);
        }

        private static Stack<RenderTarget2D> Pool(GraphicsDevice device, int width, int height)
        {
            var pools = Pools.GetValue(device, static _ => new Dictionary<(int, int), Stack<RenderTarget2D>>());
            if (!pools.TryGetValue((width, height), out var pool))
                pools[(width, height)] = pool = new Stack<RenderTarget2D>();
            return pool;
        }
    }
}
