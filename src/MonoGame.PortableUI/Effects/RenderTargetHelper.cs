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
    }
}
