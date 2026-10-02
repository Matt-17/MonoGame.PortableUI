using System;
using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Demo.Android
{
    [Activity(
        Label = "PortableUI Demo",
        MainLauncher = true,
        Icon = "@android:drawable/sym_def_app_icon",
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize | ConfigChanges.FontScale)]
    public class MainActivity : AndroidGameActivity
    {
        private AndroidDemoGame? _game;
        private View? _view;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            TextScaling.Source = TextScaleSource.System;
            AndroidTextScaling.Update(this);
            // Edge to edge (enforced from Android 15 anyway): the UI draws under the status and
            // navigation bars and the camera cutout; SafeAreaPanel keeps the content clear of them.
            // AndroidSurfaceSize keeps the back buffer equal to the full-screen view.
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
                EnableEdgeToEdge();
            if (OperatingSystem.IsAndroidVersionAtLeast(28) && Window?.Attributes is { } attributes)
            {
                attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
                Window.Attributes = attributes;
            }

            _game = new AndroidDemoGame(SetFullscreen);
            _view = _game.Services.GetService(typeof(View)) as View;
            if (_view != null)
                SetContentView(_view);

            // After SetContentView (the decor view must exist): transparent bars over the white UI,
            // with dark icons so the clock shows.
            if (Window != null)
            {
#pragma warning disable CA1422 // bar colors are ignored (always transparent) from API 35
                Window.SetStatusBarColor(global::Android.Graphics.Color.Transparent);
                Window.SetNavigationBarColor(global::Android.Graphics.Color.Transparent);
#pragma warning restore CA1422
                if (OperatingSystem.IsAndroidVersionAtLeast(30))
                    Window.InsetsController?.SetSystemBarsAppearance(
                        (int)(WindowInsetsControllerAppearance.LightStatusBars | WindowInsetsControllerAppearance.LightNavigationBars),
                        (int)(WindowInsetsControllerAppearance.LightStatusBars | WindowInsetsControllerAppearance.LightNavigationBars));
            }

            _game.Run();
        }

        /// <summary>Hides or shows the status and navigation bars at runtime (called from the game thread).</summary>
        private void SetFullscreen(bool fullscreen)
        {
            RunOnUiThread(() =>
            {
                if (!OperatingSystem.IsAndroidVersionAtLeast(30) || Window?.InsetsController is not { } controller)
                    return;
                if (fullscreen)
                {
                    // Bars come back briefly on an edge swipe; the insets listener updates the safe area.
                    controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                    controller.Hide(WindowInsets.Type.SystemBars());
                }
                else
                {
                    controller.Show(WindowInsets.Type.SystemBars());
                }
            });
        }

        [System.Runtime.Versioning.SupportedOSPlatform("android30.0")]
        private void EnableEdgeToEdge()
        {
#pragma warning disable CA1422 // deprecated in API 35, where edge to edge is already the default
            Window?.SetDecorFitsSystemWindows(false);
#pragma warning restore CA1422
        }

        protected override void OnResume()
        {
            base.OnResume();
            AndroidTextScaling.Update(this);
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            AndroidTextScaling.Update(newConfig);
        }
    }
}
