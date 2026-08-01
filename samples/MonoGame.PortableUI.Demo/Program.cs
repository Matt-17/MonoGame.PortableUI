using System;

namespace MonoGame.PortableUI.Demo
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            using var game = new DemoGame(DemoThemeRegistry.ResolveStartupTheme(args));
            game.Run();
        }
    }
}
