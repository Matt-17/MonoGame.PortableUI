using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>
    ///     Detached controls (not on a screen) focus and resolve through <see cref="ScreenEngine.Instance"/>. Tests that
    ///     rely on it call <see cref="Reset"/> in their <c>[TestInitialize]</c> and get a fresh engine, instead of whatever
    ///     an earlier test left there: none when the class runs alone, a disposed one, or one with focus still set.
    /// </summary>
    internal static class FallbackEngine
    {
        private static Game? _game;
        private static ScreenEngine? _engine;

        public static ScreenEngine Reset()
        {
            _engine?.Dispose();
            _game ??= new Game();
            _engine = ScreenEngine.Initialize(_game, new ScreenEngineOptions { AddComponentToGame = false });
            return _engine;
        }
    }
}
