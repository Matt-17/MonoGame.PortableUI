using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Effects
{
    /// <summary>
    ///     Owns the render targets for the backdrop-blur pipeline (R8). When the compiled Blur
    ///     effect is available (R3), a separable Gaussian runs at quarter resolution; otherwise the
    ///     shader-free bilinear down/upsample chain (full → ½ → ¼ → ⅛ → 1/16 → ⅛ → ¼) approximates
    ///     a wide Gaussian and works on every device.
    ///     Callers are responsible for restoring their render targets after <see cref="Blur"/>.
    /// </summary>
    public sealed class BackdropManager : IDisposable
    {
        private readonly GraphicsDevice _graphicsDevice;
        private RenderTarget2D? _sceneTarget;
        private RenderTarget2D? _half;
        private RenderTarget2D? _quarter;
        private RenderTarget2D? _eighth;
        private RenderTarget2D? _sixteenth;
        private RenderTarget2D? _pingPong;

        public BackdropManager(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        }

        /// <summary>The graphics device this manager's render targets are bound to.</summary>
        public GraphicsDevice GraphicsDevice => _graphicsDevice;

        public int BlurPassesThisFrame { get; private set; }

        public void BeginFrame()
        {
            BlurPassesThisFrame = 0;
        }

        /// <summary>Full-resolution scene target the backdrop content is rendered into before blurring.</summary>
        public RenderTarget2D EnsureSceneTarget(int width, int height)
        {
            return EnsureTarget(ref _sceneTarget, Math.Max(1, width), Math.Max(1, height));
        }

        /// <summary>
        ///     Blurs <paramref name="source"/> and returns a quarter-resolution result target.
        ///     Switches render targets internally and leaves the last one bound.
        /// </summary>
        public RenderTarget2D Blur(SpriteBatch spriteBatch, Texture2D source)
        {
            if (spriteBatch == null)
                throw new ArgumentNullException(nameof(spriteBatch));
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var width = Math.Max(1, source.Width);
            var height = Math.Max(1, source.Height);
            var half = EnsureTarget(ref _half, Math.Max(1, width / 2), Math.Max(1, height / 2));
            var quarter = EnsureTarget(ref _quarter, Math.Max(1, width / 4), Math.Max(1, height / 4));

            if (EffectCache.TryGetEffect(_graphicsDevice, EffectNames.Blur, out var blurEffect) && blurEffect != null)
            {
                // Shader path: downsample to quarter res, then separable Gaussian H + V.
                var pingPong = EnsureTarget(ref _pingPong, quarter.Width, quarter.Height);
                BlurPass(spriteBatch, source, half);
                BlurPass(spriteBatch, half, quarter);

                var direction = blurEffect.Parameters["Direction"];
                const float spread = 1.6f;
                direction?.SetValue(new Vector2(spread / quarter.Width, 0));
                BlurPass(spriteBatch, quarter, pingPong, blurEffect);
                direction?.SetValue(new Vector2(0, spread / quarter.Height));
                BlurPass(spriteBatch, pingPong, quarter, blurEffect);
                return quarter;
            }

            var eighth = EnsureTarget(ref _eighth, Math.Max(1, width / 8), Math.Max(1, height / 8));
            var sixteenth = EnsureTarget(ref _sixteenth, Math.Max(1, width / 16), Math.Max(1, height / 16));

            BlurPass(spriteBatch, source, half);
            BlurPass(spriteBatch, half, quarter);
            BlurPass(spriteBatch, quarter, eighth);
            BlurPass(spriteBatch, eighth, sixteenth);
            BlurPass(spriteBatch, sixteenth, eighth);
            BlurPass(spriteBatch, eighth, quarter);
            return quarter;
        }

        public void Dispose()
        {
            _sceneTarget?.Dispose();
            _half?.Dispose();
            _quarter?.Dispose();
            _eighth?.Dispose();
            _sixteenth?.Dispose();
            _pingPong?.Dispose();
        }

        private void BlurPass(SpriteBatch spriteBatch, Texture2D source, RenderTarget2D destination, Effect? effect = null)
        {
            _graphicsDevice.SetRenderTarget(destination);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, effect: effect);
            spriteBatch.Draw(source, new Rectangle(0, 0, destination.Width, destination.Height), Color.White);
            spriteBatch.End();
            BlurPassesThisFrame++;
        }

        private RenderTarget2D EnsureTarget(ref RenderTarget2D? target, int width, int height)
        {
            return RenderTargetHelper.EnsureTarget(_graphicsDevice, ref target, width, height);
        }
    }
}
