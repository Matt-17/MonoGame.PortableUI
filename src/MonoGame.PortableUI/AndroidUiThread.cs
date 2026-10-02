#if ANDROID
using System;
using Android.OS;
using Android.Views;

namespace MonoGame.PortableUI
{
    /// <summary>
    ///     Runs view work on Android's UI thread. The game loop runs on the UI thread by default
    ///     (<c>AndroidGameActivity.RenderOnUIThread</c>) but on its own thread when that is false, where
    ///     touching a view throws <c>CalledFromWrongThreadException</c>.
    /// </summary>
    internal static class AndroidUiThread
    {
        public static void Run(View view, Action action)
        {
            if (Looper.MyLooper() == Looper.MainLooper)
                action();
            else
                view.Post(action);
        }
    }
}
#endif
