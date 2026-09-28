using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Effects
{
    /// <summary>
    ///     Offscreen layers for non-rectangular clips: one content + mask target pair per nesting
    ///     depth, full-frame sized so the clipped subtree keeps drawing at screen coordinates.
    ///     Targets are reused across frames and recreated when the size or device changes.
    /// </summary>
    internal sealed class ClipLayerPool : IDisposable
    {
        internal sealed class Layer
        {
            public RenderTarget2D? Content;
            public RenderTarget2D? Mask;
            public RenderTargetBinding[]? PreviousTargets;
        }

        private readonly GraphicsDevice _device;
        private readonly List<Layer> _layers = new List<Layer>();

        public ClipLayerPool(GraphicsDevice device)
        {
            _device = device;
        }

        public GraphicsDevice GraphicsDevice => _device;

        public Layer Get(int depth, int width, int height)
        {
            while (_layers.Count <= depth)
                _layers.Add(new Layer());
            var layer = _layers[depth];
            layer.Content = RenderTargetHelper.EnsureTarget(_device, ref layer.Content, width, height);
            layer.Mask = RenderTargetHelper.EnsureTarget(_device, ref layer.Mask, width, height);
            return layer;
        }

        public void Dispose()
        {
            foreach (var layer in _layers)
            {
                layer.Content?.Dispose();
                layer.Mask?.Dispose();
            }
            _layers.Clear();
        }
    }
}
