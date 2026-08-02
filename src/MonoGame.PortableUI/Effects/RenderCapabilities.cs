using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Effects
{
    public sealed class RenderCapabilities
    {
        public RenderCapabilities(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice;
        }

        public GraphicsDevice GraphicsDevice { get; }
        public bool ShadersAvailable => EffectCache.TryGetEffect(GraphicsDevice, EffectNames.Primitives, out _);
        public bool BackdropBlurAvailable => EffectCache.TryGetEffect(GraphicsDevice, EffectNames.Blur, out _);
        public bool PostEffectsAvailable => EffectCache.TryGetEffect(GraphicsDevice, EffectNames.PostFx, out _);
    }

    public static class EffectNames
    {
        public const string Primitives = "Primitives";
        public const string Blur = "Blur";
        public const string PostFx = "PostFx";
    }
}
