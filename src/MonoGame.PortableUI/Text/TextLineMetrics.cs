namespace MonoGame.PortableUI.Text
{
    /// <summary>How a <see cref="FontStashUIFont"/> sizes a line of text and places its baseline.</summary>
    public enum TextLineMetrics
    {
        /// <summary>FontStashSharp's metrics: the font's <c>hhea</c> ascent + descent + line gap, unrounded.</summary>
        Font,

        /// <summary>
        ///     Like an Android <c>TextView</c> (<c>includeFontPadding</c>, the default), in whole device pixels:
        ///     a line's box reaches up and down to the larger of <c>hhea</c> and the glyph box (<c>head</c>
        ///     yMax/yMin), each rounded up, with the baseline that far below the top; wrapped lines are the
        ///     rounded hhea ascent + descent apart. Text then lines up with native Android (and .NET MAUI)
        ///     labels within a pixel.
        /// </summary>
        Android
    }
}
