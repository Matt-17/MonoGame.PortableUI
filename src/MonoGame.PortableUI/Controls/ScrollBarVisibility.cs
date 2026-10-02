namespace MonoGame.PortableUI.Controls
{
    /// <summary>When a <see cref="ScrollViewer"/> shows its scroll bars.</summary>
    public enum ScrollBarVisibility
    {
        /// <summary>Always shown; the bars take their own space next to the content (desktop default).</summary>
        Visible,
        /// <summary>Shown while scrolling, faded out after a short idle time; drawn over the content (mobile style).</summary>
        AutoHide,
        /// <summary>Never shown; scrolling still works.</summary>
        Hidden
    }
}
