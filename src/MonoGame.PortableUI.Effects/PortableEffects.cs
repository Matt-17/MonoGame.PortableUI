using System.Runtime.CompilerServices;

namespace MonoGame.PortableUI.Effects
{
    /// <summary>
    ///     Installs this package's renderer into the core (<see cref="EffectRenderers"/>): the decorative post
    ///     effects (glass and blur are part of the core). It installs itself as soon as anything in the
    ///     package is used (a post effect, a theme that has them); call
    ///     <see cref="Install"/> to install it before that, e.g. at startup.
    /// </summary>
    public static class PortableEffects
    {
        /// <summary>Installs the renderers. Idempotent.</summary>
        public static void Install()
        {
            EffectRenderers.PostEffects ??= static device => new PostProcessManager(device);
        }

#pragma warning disable CA2255 // A library installing itself: referencing a post effect must be enough.
        [ModuleInitializer]
        internal static void InstallOnLoad() => Install();
#pragma warning restore CA2255
    }
}
