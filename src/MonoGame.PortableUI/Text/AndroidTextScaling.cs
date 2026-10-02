#if ANDROID
using Android.Content;
using Android.Content.Res;

namespace MonoGame.PortableUI.Text
{
    /// <summary>Feeds Android's font scale into <see cref="TextScaling.SystemScale"/>. Display density belongs in
    /// <see cref="ScreenEngineOptions.LayoutScale"/> (whole UI in dp), not in the text factor.</summary>
    /// <remarks>
    ///     Call <see cref="Update(Context)"/> in <c>OnCreate</c> and <c>OnResume</c> (the user may change
    ///     the setting while the app is in the background), and <see cref="Update(Configuration)"/> from
    ///     <c>OnConfigurationChanged</c> when the activity declares <c>ConfigChanges.FontScale</c>.
    /// </remarks>
    public static class AndroidTextScaling
    {
        public static void Update(Context context)
        {
            if (context.Resources?.Configuration is { } configuration)
                Update(configuration);
        }

        public static void Update(Configuration configuration)
        {
            TextScaling.SystemScale = configuration.FontScale;
        }
    }
}
#endif
