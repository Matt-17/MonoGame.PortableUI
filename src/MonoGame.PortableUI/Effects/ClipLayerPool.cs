using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Effects
{
    /// <summary>
    ///     Offscreen layers for non-rectangular clips: one content + mask target pair per nesting
    ///     depth. Inside clips use control-sized layers (<see cref="GetAtLeast"/>), outside clips
    ///     full-frame ones (<see cref="Get"/>). Targets are reused across frames and recreated when
    ///     the size or device changes.
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

        /// <summary>
        ///     A layer pair at least <paramref name="width"/> x <paramref name="height"/>: sizes grow in
        ///     64-pixel steps and a target up to twice as large is reused, so a control that moves or
        ///     changes by a pixel does not recreate its targets every frame.
        /// </summary>
        public Layer GetAtLeast(int depth, int width, int height)
        {
            while (_layers.Count <= depth)
                _layers.Add(new Layer());
            var layer = _layers[depth];
            layer.Content = EnsureAtLeast(ref layer.Content, width, height);
            layer.Mask = EnsureAtLeast(ref layer.Mask, width, height);
            return layer;
        }

        private RenderTarget2D EnsureAtLeast(ref RenderTarget2D? target, int width, int height)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);
            if (target is { IsDisposed: false } existing
                && existing.Width >= width && existing.Height >= height
                && existing.Width <= width * 2 + 64 && existing.Height <= height * 2 + 64)
                return existing;
            return RenderTargetHelper.EnsureTarget(_device, ref target, (width + 63) / 64 * 64, (height + 63) / 64 * 64);
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
