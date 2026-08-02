using System;

namespace MonoGame.PortableUI.Demo
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            using var game = new DemoGame(DemoRunOptions.Parse(args));
            game.Run();
        }
    }
}
