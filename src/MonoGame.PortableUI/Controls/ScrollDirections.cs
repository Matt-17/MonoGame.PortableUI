namespace MonoGame.PortableUI.Controls
{
    /// <summary>Axes a <see cref="ScrollViewer"/> scrolls along.</summary>
    public enum ScrollDirections
    {
        Vertical,
        Horizontal,
        Both,
        /// <summary>No scrolling: the content is laid out within the viewport (e.g. a data grid
        /// whose columns fit).</summary>
        None
    }
}
