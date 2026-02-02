using System;

namespace MonoGame.PortableUI.Demo
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            using var game = new DemoGame();
            game.Run();
        }
    }
}
