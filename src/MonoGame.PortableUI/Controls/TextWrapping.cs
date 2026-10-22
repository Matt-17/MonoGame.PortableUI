namespace MonoGame.PortableUI.Controls
{
    public enum TextWrapping
    {
        NoWrap,

        /// <summary>Fills each line as far as it goes; the last line takes the rest.</summary>
        Wrap,

        /// <summary>As few lines as <see cref="Wrap"/>, filled from the bottom: each lower line as long as it fits,
        /// the top line the shortest ("Deep" over "Purple Magic Sour") - for headings.</summary>
        WrapBottomHeavy,

        /// <summary>As few lines as <see cref="Wrap"/>, as even as possible: the longest line as short as it can be.</summary>
        WrapBalanced
    }

    public enum TextTrimming
    {
        None,

        /// <summary>Trim overflowing text (NoWrap only) and append "..." so it fits the width.</summary>
        Ellipsis
    }
}
