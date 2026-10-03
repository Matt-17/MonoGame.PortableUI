namespace MonoGame.PortableUI.Controls
{
    /// <summary>How a text box shows its caret (the text cursor, not the mouse pointer).</summary>
    public enum CaretStyle
    {
        /// <summary>A thin vertical bar (modern systems).</summary>
        Bar,

        /// <summary>
        ///     Text-mode caret: a two-scanline underline under the character cell in insert mode, a full
        ///     block in overwrite mode (Insert key toggles), blinking fast as on DOS (about 3.75 Hz).
        /// </summary>
        TextMode
    }
}
