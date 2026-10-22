using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Effects
{
    /// <summary>
    ///     Blurs the bands of a <see cref="Controls.ProgressiveBlur"/>: the band is scaled down once (so the blur
    ///     has at most 24 texels of radius), blurred by a separable Gaussian whose taps spread with each row's radius,
    ///     and mixed into the sharp content where the radius runs out. A Gaussian on one fixed downscale stays still
    ///     while content scrolls through it; a deep pyramid of halvings shimmered, every pixel of movement landing
    ///     differently in its coarse levels. Targets are rented per draw.
    /// </summary>
    public sealed class ProgressiveBlurRenderer : IProgressiveBlurRenderer
    {
        /// <summary>Most radius, in texels of the downscaled band, the 12 taps per side cover without gaps.</summary>
        private const float MaxTexelRadius = 24;

        private readonly Slot[] _slots = { new Slot(), new Slot() };

        public ProgressiveBlurRenderer(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        }

        public GraphicsDevice GraphicsDevice { get; }

        public void Prepare(SpriteBatch spriteBatch, Texture2D content, Rectangle band, int edgeRow, int length, float radius, Color tint, int slot)
        {
            var state = _slots[slot];
            Return(state);
            if (band.Width <= 0 || band.Height <= 0 || length <= 0
                || !EffectCache.TryGetEffect(GraphicsDevice, EffectNames.ProgressiveBlur, out var effect) || effect == null)
                return;

            // Halvings until the radius fits the taps; each one a 2x2 box.
            var scale = 1;
            while (radius / scale > MaxTexelRadius && scale < 16)
                scale *= 2;
            Texture2D source = content;
            var sourceRect = band;
            var width = band.Width;
            var height = band.Height;
            for (var s = 1; s < scale; s *= 2)
            {
                width = Math.Max(1, width / 2);
                height = Math.Max(1, height / 2);
                var half = RenderTargetPool.Rent(GraphicsDevice, width, height);
                Pass(spriteBatch, source, sourceRect, half, null);
                if (!ReferenceEquals(source, content))
                    RenderTargetPool.Return(GraphicsDevice, (RenderTarget2D)source);
                source = half;
                sourceRect = half.Bounds;
            }

            var edgeV = (edgeRow - band.Top) / (float)band.Height;
            var slope = band.Height / (float)length;
            effect.CurrentTechnique = effect.Techniques["Blur"];
            effect.Parameters["EdgeV"].SetValue(edgeV);
            effect.Parameters["Slope"].SetValue(slope);
            effect.Parameters["MaxRadius"].SetValue(radius / scale);

            var first = RenderTargetPool.Rent(GraphicsDevice, width, height);
            effect.Parameters["Direction"].SetValue(new Vector2(1f / width, 0));
            Pass(spriteBatch, source, sourceRect, first, effect);
            if (!ReferenceEquals(source, content))
                RenderTargetPool.Return(GraphicsDevice, (RenderTarget2D)source);
            var second = RenderTargetPool.Rent(GraphicsDevice, width, height);
            effect.Parameters["Direction"].SetValue(new Vector2(0, 1f / height));
            Pass(spriteBatch, first, first.Bounds, second, effect);
            RenderTargetPool.Return(GraphicsDevice, first);

            state.Blurred = second;
            state.Content = content;
            state.Band = band;
            state.EdgeV = edgeV;
            state.Slope = slope;
            state.Radius = radius;
            state.Tint = tint.ToVector4();
            // Fully blurred from two downscaled texels on: below that the downscale itself would show.
            state.FadeRadius = Math.Max(2f, 2f * scale);
        }

        public void Draw(SpriteBatch spriteBatch, int slot, RasterizerState rasterizer)
        {
            var state = _slots[slot];
            if (state.Blurred == null || state.Content == null
                || !EffectCache.TryGetEffect(GraphicsDevice, EffectNames.ProgressiveBlur, out var effect) || effect == null)
                return;

            var content = state.Content;
            var band = state.Band;
            effect.CurrentTechnique = effect.Techniques["Compose"];
            effect.Parameters["EdgeV"].SetValue(state.EdgeV);
            effect.Parameters["Slope"].SetValue(state.Slope);
            effect.Parameters["MaxRadius"].SetValue(state.Radius);
            effect.Parameters["FadeRadius"].SetValue(state.FadeRadius);
            effect.Parameters["Tint"].SetValue(state.Tint);
            effect.Parameters["SharpTexture"].SetValue(content);
            effect.Parameters["SharpRect"].SetValue(new Vector4(
                band.Left / (float)content.Width, band.Top / (float)content.Height,
                band.Width / (float)content.Width, band.Height / (float)content.Height));
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, rasterizerState: rasterizer, effect: effect);
            spriteBatch.Draw(state.Blurred, band, Color.White);
            spriteBatch.End();
        }

        public void Release()
        {
            foreach (var slot in _slots)
                Return(slot);
        }

        public void Dispose() => Release();

        private void Return(Slot slot)
        {
            if (slot.Blurred != null)
                RenderTargetPool.Return(GraphicsDevice, slot.Blurred);
            slot.Blurred = null;
            slot.Content = null;
        }

        private static void Pass(SpriteBatch spriteBatch, Texture2D source, Rectangle sourceRect, RenderTarget2D target, Effect? effect)
        {
            spriteBatch.GraphicsDevice.SetRenderTarget(target);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, effect: effect);
            spriteBatch.Draw(source, target.Bounds, sourceRect, Color.White);
            spriteBatch.End();
        }

        private sealed class Slot
        {
            public RenderTarget2D? Blurred;
            public Texture2D? Content;
            public Rectangle Band;
            public float EdgeV;
            public float Slope;
            public float Radius;
            public float FadeRadius;
            public Vector4 Tint;
        }
    }
}
