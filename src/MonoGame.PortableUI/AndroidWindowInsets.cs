#if ANDROID
using Android.Views;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI
{
    /// <summary>
    ///     Feeds Android window insets (system bars, display cutout, IME) into
    ///     <see cref="ScreenEngine.SetSystemInsets"/> and <see cref="ScreenEngine.SetKeyboardInset"/>.
    /// </summary>
    /// <remarks>
    ///     Call <see cref="Attach"/> once with the game view after <c>SetContentView</c>. For insets to
    ///     reach the view the activity must draw edge to edge (for example
    ///     <c>Window.SetDecorFitsSystemWindows(false)</c> and
    ///     <c>LayoutInDisplayCutoutMode = ShortEdges</c>); otherwise Android keeps the content clear of
    ///     the bars itself and the reported insets are zero.
    /// </remarks>
    public static class AndroidWindowInsets
    {
        public static void Attach(View view, ScreenEngine engine)
        {
            view.SetOnApplyWindowInsetsListener(new Listener(engine));
            view.RequestApplyInsets();
        }

        private sealed class Listener : Java.Lang.Object, View.IOnApplyWindowInsetsListener
        {
            private readonly ScreenEngine _engine;

            public Listener(ScreenEngine engine)
            {
                _engine = engine;
            }

            public WindowInsets OnApplyWindowInsets(View view, WindowInsets insets)
            {
                if (System.OperatingSystem.IsAndroidVersionAtLeast(30))
                {
                    var system = insets.GetInsets(WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout());
                    var ime = insets.GetInsets(WindowInsets.Type.Ime());
                    _engine.SetSystemInsets(new Thickness(system.Left, system.Top, system.Right, system.Bottom));
                    _engine.SetKeyboardInset(ime.Bottom);
                }
                else
                {
#pragma warning disable CA1422 // pre-API-30 fallback
                    _engine.SetSystemInsets(new Thickness(insets.SystemWindowInsetLeft, insets.SystemWindowInsetTop, insets.SystemWindowInsetRight, insets.SystemWindowInsetBottom));
#pragma warning restore CA1422
                }
                return view.OnApplyWindowInsets(insets) ?? insets;
            }
        }
    }
}
#endif
