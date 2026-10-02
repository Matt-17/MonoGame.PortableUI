namespace MonoGame.PortableUI
{
    /// <summary>How a <see cref="ScreenEngine"/> paces drawing (<see cref="ScreenEngineOptions.RenderMode"/>).</summary>
    public enum RenderMode
    {
        /// <summary>Draws every frame, like a game. The default: the host's own drawing is never touched.</summary>
        Continuous,

        /// <summary>
        ///     Draws only when something changed: input, a property change, a layout pass, a running
        ///     animation or transition, or a visual that asked for its next frame
        ///     (<see cref="ScreenEngine.RequestAnimationFrame"/>). While nothing changes the game's
        ///     draw is suppressed and updates are throttled to
        ///     <see cref="ScreenEngineOptions.IdleUpdateInterval"/>, so CPU and GPU can rest — the mode
        ///     for app-style hosts where PortableUI owns the whole frame. A host that draws its own
        ///     moving content calls <see cref="ScreenEngine.RequestRedraw"/> each frame it needs.
        /// </summary>
        OnDemand
    }
}
