using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using SampleApp;

namespace SampleClient.Droid
{
    [Activity(Label = "SampleClient.Android"
        , MainLauncher = true
        , Icon = "@drawable/icon"
        , Theme = "@style/Theme.Splash"
        , AlwaysRetainTaskState = true
        , LaunchMode = LaunchMode.SingleInstance
        , ScreenOrientation = ScreenOrientation.SensorLandscape
        , ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    public class MainActivity : Microsoft.Xna.Framework.AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            var g = new SampleGame();
            g.Window.AllowUserResizing = true;
            g.Initialize();
            g.Window.ClientSizeChanged += (object sender, EventArgs e) =>
            {
                g.SetClientSize(g.Window.ClientBounds.Width, g.Window.ClientBounds.Height);
            };
            SetContentView((View)g.Services.GetService(typeof(View)));
            g.Run();
        }
    }
}

