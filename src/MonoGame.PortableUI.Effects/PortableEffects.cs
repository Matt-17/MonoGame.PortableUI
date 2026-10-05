using System.Runtime.CompilerServices;

namespace MonoGame.PortableUI.Effects
{
    /// <summary>
    ///     Installs this package's renderers into the core (<see cref="EffectRenderers"/>): post effects
    ///     and the backdrop blur glass brushes sample. It installs itself as soon as anything in the
    ///     package is used (a post effect, a glass brush, a theme that has them); call
    ///     <see cref="Install"/> to install it before that, e.g. at startup.
    /// </summary>
    public static class PortableEffects
    {
        /// <summary>Installs the renderers. Idempotent.</summary>
        public static void Install()
        {
            EffectRenderers.PostEffects ??= static device => new PostProcessManager(device);
            EffectRenderers.Backdrop ??= static device => new BackdropRenderer(device);
        }

#pragma warning disable CA2255 // A library installing itself: referencing a post effect or glass brush must be enough.
        [ModuleInitializer]
        internal static void InstallOnLoad() => Install();
#pragma warning restore CA2255
    }
}
