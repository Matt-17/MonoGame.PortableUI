using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SampleApp;

namespace SampleClient
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var game = new SampleGame())
            {

                //var config = new Config()
                //{
                //    PlatformOS = PlatformOS.Windows,
                //    IsFullscreen = false,
                //    IsMouseAvailable = true,
                //};
                game.Window.AllowUserResizing = true;
                game.Initialize();
                game.Window.ClientSizeChanged += (object sender, EventArgs e) =>
                {
                    game.SetClientSize(game.Window.ClientBounds.Width, game.Window.ClientBounds.Height);
                };
                game.Run();

            }
        }
    }
}
