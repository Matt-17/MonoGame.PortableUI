namespace MonoGame.PortableUI.Controls
{
    public enum TextWrapping
    {
        NoWrap,
        Wrap
    }

    public enum TextTrimming
    {
        None,

        /// <summary>Trim overflowing text (NoWrap only) and append "..." so it fits the width.</summary>
        Ellipsis
    }
}
