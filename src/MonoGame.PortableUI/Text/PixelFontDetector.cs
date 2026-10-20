using System;
using System.Collections.Generic;

namespace MonoGame.PortableUI.Text
{
    /// <summary>
    ///     Recognizes pixel-art TrueType fonts (Press Start 2P, Px437, Silkscreen, VT323...): their outlines
    ///     sit on a coarse grid of font units. Reads the glyph outlines directly (head, hhea, maxp, loca and
    ///     glyf tables) - no rasterizer, no unsafe code.
    /// </summary>
    /// <summary>Vertical font metrics in ems; descents are positive below the baseline.</summary>
    internal readonly record struct FontVerticalMetrics(float Ascent, float Descent, float Top, float Bottom);

    internal static class PixelFontDetector
    {
        /// <summary>
        ///     The pixel height (as FontStashSharp sizes fonts: ascent minus descent) at which one font pixel
        ///     covers exactly one screen pixel - the font is crisp at whole multiples of it. 0 when the font
        ///     is not a pixel font or cannot be read.
        /// </summary>
        public static float DetectGrid(byte[] font)
        {
            try
            {
                return Detect(font);
            }
            catch (Exception exception) when (exception is IndexOutOfRangeException or ArgumentException or OverflowException)
            {
                return 0;
            }
        }

        /// <summary>
        ///     FontStashSharp sizes a font by its full height (ascent minus descent), a SpriteFont by its em:
        ///     this is height / em, so an em size times it gives the FontStashSharp size. 1 when unreadable.
        /// </summary>
        public static float HeightPerEm(byte[] font)
        {
            try
            {
                if (!TryFindTable(font, "head", out var head) || !TryFindTable(font, "hhea", out var hhea))
                    return 1;
                var unitsPerEm = U16(font, head + 18);
                var height = S16(font, hhea + 4) - S16(font, hhea + 6);
                return unitsPerEm > 0 && height > 0 ? height / (float)unitsPerEm : 1;
            }
            catch (Exception exception) when (exception is IndexOutOfRangeException or ArgumentException)
            {
                return 1;
            }
        }

        /// <summary>
        ///     The font's vertical metrics in ems: hhea ascent/descent (descent positive downwards) and the
        ///     glyph bounding box top/bottom from head (yMax, -yMin). False when the tables are unreadable.
        /// </summary>
        public static bool TryReadVerticalMetrics(byte[] font, out FontVerticalMetrics metrics)
        {
            metrics = default;
            try
            {
                if (!TryFindTable(font, "head", out var head) || !TryFindTable(font, "hhea", out var hhea))
                    return false;
                var unitsPerEm = (float)U16(font, head + 18);
                if (unitsPerEm <= 0)
                    return false;
                metrics = new FontVerticalMetrics(
                    S16(font, hhea + 4) / unitsPerEm,
                    -S16(font, hhea + 6) / unitsPerEm,
                    S16(font, head + 42) / unitsPerEm,
                    -S16(font, head + 38) / unitsPerEm);
                return true;
            }
            catch (Exception exception) when (exception is IndexOutOfRangeException or ArgumentException)
            {
                return false;
            }
        }

        private static float Detect(byte[] font)
        {
            if (!TryFindTable(font, "head", out var head) || !TryFindTable(font, "hhea", out var hhea)
                || !TryFindTable(font, "maxp", out var maxp) || !TryFindTable(font, "loca", out var loca)
                || !TryFindTable(font, "glyf", out var glyf))
                return 0;

            var unitsPerEm = U16(font, head + 18);
            var longOffsets = S16(font, head + 50) != 0;
            var ascender = S16(font, hhea + 4);
            var descender = S16(font, hhea + 6);
            var glyphCount = U16(font, maxp + 4);
            if (unitsPerEm == 0 || ascender - descender <= 0)
                return 0;

            var coordinates = new List<int>(4096);
            for (var glyph = 0; glyph < glyphCount && coordinates.Count < 20000; glyph++)
            {
                var start = longOffsets ? (int)U32(font, loca + glyph * 4) : U16(font, loca + glyph * 2) * 2;
                var end = longOffsets ? (int)U32(font, loca + glyph * 4 + 4) : U16(font, loca + glyph * 2 + 2) * 2;
                if (end > start)
                    ReadSimpleGlyph(font, glyf + start, coordinates);
            }

            if (coordinates.Count < 50)
                return 0;

            // The largest unit most coordinates are multiples of; a pixel font has a handful of pixels per em.
            var minUnit = Math.Max(2, unitsPerEm / 40);
            for (var unit = unitsPerEm / 4; unit >= minUnit; unit--)
            {
                var onGrid = 0;
                foreach (var value in coordinates)
                {
                    if (value % unit == 0)
                        onGrid++;
                }

                if (onGrid >= coordinates.Count * 0.95)
                    return (ascender - descender) / (float)unit;
            }

            return 0;
        }

        // Appends the absolute coordinates of a simple glyph (composites are skipped).
        private static void ReadSimpleGlyph(byte[] font, int offset, List<int> coordinates)
        {
            var contours = S16(font, offset);
            if (contours <= 0)
                return;

            var cursor = offset + 10;
            var points = U16(font, cursor + (contours - 1) * 2) + 1;
            cursor += contours * 2;
            cursor += 2 + U16(font, cursor);

            var flags = new byte[points];
            for (var i = 0; i < points;)
            {
                var flag = font[cursor++];
                flags[i++] = flag;
                if ((flag & 8) != 0)
                {
                    var repeat = font[cursor++];
                    for (var r = 0; r < repeat && i < points; r++)
                        flags[i++] = flag;
                }
            }

            var x = 0;
            for (var i = 0; i < points; i++)
            {
                var flag = flags[i];
                if ((flag & 2) != 0)
                    x += (flag & 16) != 0 ? font[cursor++] : -font[cursor++];
                else if ((flag & 16) == 0)
                {
                    x += S16(font, cursor);
                    cursor += 2;
                }

                coordinates.Add(x);
            }

            var y = 0;
            for (var i = 0; i < points; i++)
            {
                var flag = flags[i];
                if ((flag & 4) != 0)
                    y += (flag & 32) != 0 ? font[cursor++] : -font[cursor++];
                else if ((flag & 32) == 0)
                {
                    y += S16(font, cursor);
                    cursor += 2;
                }

                coordinates.Add(y);
            }
        }

        private static bool TryFindTable(byte[] font, string tag, out int offset)
        {
            var tables = U16(font, 4);
            for (var i = 0; i < tables; i++)
            {
                var record = 12 + i * 16;
                if (font[record] == tag[0] && font[record + 1] == tag[1] && font[record + 2] == tag[2] && font[record + 3] == tag[3])
                {
                    offset = (int)U32(font, record + 8);
                    return true;
                }
            }

            offset = 0;
            return false;
        }

        private static int U16(byte[] data, int offset) => data[offset] << 8 | data[offset + 1];

        private static int S16(byte[] data, int offset) => (short)(data[offset] << 8 | data[offset + 1]);

        private static uint U32(byte[] data, int offset) => (uint)(data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3]);
    }
}
