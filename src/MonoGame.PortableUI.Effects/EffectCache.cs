using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Effects
{
    public static class EffectCache
    {
        private static readonly Dictionary<GraphicsDevice, Dictionary<string, Effect?>> EffectsByDevice = new Dictionary<GraphicsDevice, Dictionary<string, Effect?>>();

        public static bool TryGetEffect(GraphicsDevice graphicsDevice, string name, out Effect? effect)
        {
            effect = null;
            if (graphicsDevice == null || string.IsNullOrWhiteSpace(name))
                return false;

            if (!EffectsByDevice.TryGetValue(graphicsDevice, out var deviceEffects))
            {
                deviceEffects = new Dictionary<string, Effect?>(StringComparer.OrdinalIgnoreCase);
                EffectsByDevice[graphicsDevice] = deviceEffects;
            }

            if (deviceEffects.TryGetValue(name, out effect))
                return effect != null;

            effect = LoadEffect(graphicsDevice, name);
            deviceEffects[name] = effect;
            return effect != null;
        }

        private static Effect? LoadEffect(GraphicsDevice graphicsDevice, string name)
        {
            var assembly = typeof(EffectCache).GetTypeInfo().Assembly;
            var resourceName = $"MonoGame.PortableUI.Effects.compiled.{name}.ogl.mgfxo";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return null;

            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            try
            {
                return new Effect(graphicsDevice, memory.ToArray());
            }
            catch
            {
                return null;
            }
        }
    }
}
