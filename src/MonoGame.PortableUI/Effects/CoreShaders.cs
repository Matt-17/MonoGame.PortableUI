using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Effects
{
    /// <summary>
    ///     Shaders of core visuals (<see cref="Controls.ProgressiveBlur"/>, the backdrop blur and liquid glass), embedded in this assembly so they work with
    ///     no install and no optional package. Loaded once per device; a shader that cannot load (no resource, a
    ///     graphics backend the OpenGL bytecode does not fit) is reported once and its visual draws without it.
    /// </summary>
    internal static class CoreShaders
    {
        public const string ProgressiveBlur = "ProgressiveBlur";
        public const string Blur = "Blur";
        public const string LiquidGlass = "LiquidGlass";

        private static readonly ConditionalWeakTable<GraphicsDevice, Dictionary<string, Effect?>> Loaded = new();
        private static readonly HashSet<string> Reported = new();

        public static bool TryGet(GraphicsDevice device, string name, out Effect? effect)
        {
            var loaded = Loaded.GetValue(device, static _ => new Dictionary<string, Effect?>(StringComparer.Ordinal));
            if (!loaded.TryGetValue(name, out effect))
                loaded[name] = effect = Load(device, name);
            return effect != null;
        }

        private static Effect? Load(GraphicsDevice device, string name)
        {
            var resource = $"MonoGame.PortableUI.Shaders.{name}.ogl.mgfxo";
            try
            {
                using var stream = typeof(CoreShaders).Assembly.GetManifestResourceStream(resource);
                if (stream == null)
                {
                    Report(name, $"resource '{resource}' is missing");
                    return null;
                }
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                return new Effect(device, memory.ToArray());
            }
            catch (Exception exception)
            {
                Report(name, exception.Message);
                return null;
            }
        }

        private static void Report(string name, string reason)
        {
            lock (Reported)
            {
                if (!Reported.Add(name))
                    return;
            }
            System.Diagnostics.Trace.TraceWarning($"MonoGame.PortableUI: shader '{name}' could not be loaded ({reason}); its visual draws without it.");
        }
    }
}
