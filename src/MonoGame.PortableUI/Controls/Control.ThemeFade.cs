namespace MonoGame.PortableUI.Controls
{
    /// <summary>How an element changes over in a <see cref="ThemeTransitionStyle.ElementFade"/>.</summary>
    public enum ThemeFadeMode
    {
        /// <summary>As its parent (the root: <see cref="Slow"/>).</summary>
        Inherit,

        /// <summary>Evenly over its parent's time, gently in and out: surfaces, cards, most content.</summary>
        Slow,

        /// <summary>
        ///     Stays near its old look, flips quickly when its parent passes the middle and settles, within 80 % of the
        ///     parent's time: for what contrasts strongly with its background (a selection indicator and its labels, a
        ///     knob, high-contrast text and icons), so it is never grey on grey for more than a moment.
        /// </summary>
        Flip
    }

    public partial class Control
    {
        /// <summary>How this element (and, unless they set their own, its descendants) changes over in an
        /// <see cref="ThemeTransitionStyle.ElementFade"/>. <see cref="ThemeFadeMode.Inherit"/> by default;
        /// <see cref="ThemeFadeMode.Flip"/> for a <see cref="TextBlock"/> (not a <see cref="TextBox"/>).</summary>
        public ThemeFadeMode ThemeFade { get; set; }
    }
}
