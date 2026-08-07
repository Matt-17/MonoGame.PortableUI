using System;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Effects
{
    /// <summary>
    /// Shared "recreate if the requested size changed" logic for pooled render targets, previously
    /// duplicated across <see cref="PostProcessManager"/> and <see cref="BackdropManager"/>.
    /// </summary>
    internal static class RenderTargetHelper
    {
        public static RenderTarget2D EnsureTarget(GraphicsDevice graphicsDevice, ref RenderTarget2D? target, int width, int height)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);
            if (target != null && !target.IsDisposed && target.Width == width && target.Height == height)
                return target;

            target?.Dispose();
            target = new RenderTarget2D(graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            return target;
        }

        /// <summary>
        /// Snapshot of the currently bound render targets without allocating in the common case:
        /// UI passes almost always start on the backbuffer, where <see cref="GraphicsDevice.GetRenderTargets()"/>
        /// would still allocate an empty array every frame.
        /// </summary>
        public static RenderTargetBinding[] SnapshotRenderTargets(GraphicsDevice graphicsDevice)
        {
            return graphicsDevice.RenderTargetCount == 0
                ? Array.Empty<RenderTargetBinding>()
                : graphicsDevice.GetRenderTargets();
        }
    }
}
