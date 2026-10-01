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
            if (OperatingSystem.IsAndroidVersionAtLeast(30) && !OperatingSystem.IsAndroidVersionAtLeast(35)) // 35+ is edge to edge by default
                Window?.SetDecorFitsSystemWindows(false);
            if (OperatingSystem.IsAndroidVersionAtLeast(28) && Window?.Attributes is { } attributes)
            {
                attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
                Window.Attributes = attributes;
            }

            _game = new AndroidDemoGame();
            _view = _game.Services.GetService(typeof(View)) as View;
            if (_view != null)
                SetContentView(_view);

            _game.Run();
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
