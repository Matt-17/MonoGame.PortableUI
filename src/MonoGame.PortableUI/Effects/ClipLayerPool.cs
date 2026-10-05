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

        /// <summary>Rents the targets from <see cref="RenderTargetPool"/> and gives them back in
        /// <see cref="ReleaseShared"/> after each draw (surface engines: many, drawn one after another).</summary>
        public bool SharesTargets { get; init; }

        public Layer Get(int depth, int width, int height)
        {
            while (_layers.Count <= depth)
                _layers.Add(new Layer());
            var layer = _layers[depth];
            layer.Content = Ensure(ref layer.Content, width, height);
            layer.Mask = Ensure(ref layer.Mask, width, height);
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
            return Ensure(ref target, (width + 63) / 64 * 64, (height + 63) / 64 * 64);
        }

        private RenderTarget2D Ensure(ref RenderTarget2D? target, int width, int height)
        {
            if (!SharesTargets)
                return RenderTargetHelper.EnsureTarget(_device, ref target, width, height);
            width = Math.Max(1, width);
            height = Math.Max(1, height);
            if (target is { IsDisposed: false } existing && existing.Width == width && existing.Height == height)
                return existing;
            if (target != null)
                RenderTargetPool.Return(_device, target);
            return target = RenderTargetPool.Rent(_device, width, height);
        }

        /// <summary>Gives rented targets back to the shared pool (no-op unless <see cref="SharesTargets"/>).</summary>
        public void ReleaseShared()
        {
            if (!SharesTargets)
                return;
            foreach (var layer in _layers)
            {
                if (layer.Content != null)
                    RenderTargetPool.Return(_device, layer.Content);
                if (layer.Mask != null)
                    RenderTargetPool.Return(_device, layer.Mask);
                layer.Content = null;
                layer.Mask = null;
            }
        }

        public void Dispose()
        {
            if (SharesTargets)
            {
                ReleaseShared();
                _layers.Clear();
                return;
            }
            foreach (var layer in _layers)
            {
                layer.Content?.Dispose();
                layer.Mask?.Dispose();
            }
            _layers.Clear();
        }
    }
}
