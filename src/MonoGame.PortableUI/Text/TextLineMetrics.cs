namespace MonoGame.PortableUI.Text
{
    /// <summary>How a <see cref="FontStashUIFont"/> sizes a line of text and places its baseline.</summary>
    public enum TextLineMetrics
    {
        /// <summary>FontStashSharp's metrics: the font's <c>hhea</c> ascent + descent + line gap, unrounded.</summary>
        Font,

        /// <summary>
        ///     Like an Android <c>TextView</c> (<c>includeFontPadding</c>, the default): ascent and descent are
        ///     the larger of <c>hhea</c> and the glyph bounding box (<c>head</c> yMax/yMin), each rounded up to
        ///     whole device pixels; the baseline sits that rounded ascent below the line's top. Text then lines
        ///     up with native Android (and .NET MAUI) labels to the pixel.
        /// </summary>
        Android
    }
}
