namespace MonoGame.PortableUI
{
    /// <summary>
    ///     How much rendering effort the UI spends (<see cref="ScreenEngineOptions.RenderQuality"/>): a
    ///     device and battery policy, independent of the theme. Themes keep their look; lower levels
    ///     drop the expensive or never-resting parts of it.
    /// </summary>
    public enum RenderQuality
    {
        /// <summary><see cref="Low"/> while the device is in battery saver mode (Android), otherwise <see cref="High"/>.</summary>
        Auto,

        /// <summary>Everything the theme asks for.</summary>
        High,

        /// <summary>
        ///     No perpetual animations: glass sweeps and film-grain noise stand still, so an idle screen
        ///     needs no frames in <see cref="RenderMode.OnDemand"/>. Blur and post effects stay.
        /// </summary>
        Balanced,

        /// <summary>
        ///     <see cref="Balanced"/>, plus no backdrop blur (glass brushes fall back to their tint), no
        ///     theme post effects (display effects of an in-world screen stay), lighter shadows and at most
        ///     30 frames per second unless <see cref="ScreenEngineOptions.MaxFrameRate"/> says otherwise.
        /// </summary>
        Low
    }
}
