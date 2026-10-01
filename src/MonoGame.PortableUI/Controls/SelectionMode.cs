namespace MonoGame.PortableUI.Controls
{
    /// <summary>How many items a <see cref="ListBox"/> can select.</summary>
    public enum SelectionMode
    {
        /// <summary>One item; a click selects it.</summary>
        Single,
        /// <summary>Any number; a click (or Space) toggles an item.</summary>
        Multiple,
        /// <summary>A click selects one item, Ctrl+click toggles, Shift+click selects a range (desktop style).</summary>
        Extended
    }
}
