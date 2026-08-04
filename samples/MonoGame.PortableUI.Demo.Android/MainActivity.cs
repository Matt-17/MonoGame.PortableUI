using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Demo.Android
{
    [Activity(
        Label = "PortableUI Demo",
        MainLauncher = true,
        Icon = "@android:drawable/sym_def_app_icon",
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    public class MainActivity : AndroidGameActivity
    {
        private AndroidDemoGame? _game;
        private View? _view;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _game = new AndroidDemoGame();
            _view = _game.Services.GetService(typeof(View)) as View;
            if (_view != null)
                SetContentView(_view);

            _game.Run();
        }
    }
}
