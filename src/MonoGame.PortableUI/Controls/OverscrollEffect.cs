namespace MonoGame.PortableUI.Controls
{
    /// <summary>How a <see cref="ScrollViewer"/> shows a drag past the end of its content.</summary>
    public enum OverscrollEffect
    {
        /// <summary>The content moves past the edge and springs back (rubber band, iOS style).</summary>
        Shift,
        /// <summary>The content stays at the edge and stretches away from it, then relaxes (Android 12+ style).</summary>
        Stretch
    }
}
