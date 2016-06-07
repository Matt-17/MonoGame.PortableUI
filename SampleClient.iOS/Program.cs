using System;
using Foundation;
using SampleApp;
using UIKit;

namespace SampleClient.iOS
{
    [Register("AppDelegate")]
    class Program : UIApplicationDelegate
    {
        private static SampleGame g;

        internal static void RunGame()
        {
            g = new SampleGame();
            g.Window.AllowUserResizing = true;
            g.Initialize();
            g.Window.ClientSizeChanged += (object sender, EventArgs e) =>
            {
                g.SetClientSize(g.Window.ClientBounds.Width, g.Window.ClientBounds.Height);
            };
            g.Run();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, "AppDelegate");
        }

        public override void FinishedLaunching(UIApplication app)
        {
            RunGame();
        }
    }
}
