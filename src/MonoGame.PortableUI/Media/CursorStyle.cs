using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    ///     A themed software mouse pointer: a small bitmap ('#' outline, '.' fill, ' ' transparent)
    ///     drawn from solid rects, so it needs no texture and scales crisply. Shown when
    ///     <see cref="ScreenEngineOptions.ShowSoftwareCursor"/> is on; the screen draws it as part of
    ///     the UI, so screen effects (CRT curvature) bend it like everything else.
    /// </summary>
    public sealed class CursorStyle
    {
        public CursorStyle(string[] rows, Color fill, Color outline, float pixelSize = 1)
        {
            Rows = rows ?? Array.Empty<string>();
            Fill = fill;
            Outline = outline;
            PixelSize = pixelSize > 0 ? pixelSize : 1;
        }

        /// <summary>Bitmap rows, top to bottom; the hotspot is the top-left pixel.</summary>
        public string[] Rows { get; }

        public Color Fill { get; }

        public Color Outline { get; }

        /// <summary>Layout units per bitmap pixel (2 = chunky pixel-art pointer).</summary>
        public float PixelSize { get; }

        private static readonly string[] ArrowRows =
        {
            "#",
            "##",
            "#.#",
            "#..#",
            "#...#",
            "#....#",
            "#.....#",
            "#......#",
            "#.......#",
            "#........#",
            "#.........#",
            "#......#####",
            "#...#..#",
            "#..##..#",
            "#.#  #..#",
            "##   #..#",
            "#     #..#",
            "      #..#",
            "       ##"
        };

        private static readonly string[] PixelArrowRows =
        {
            "#",
            "##",
            "#.#",
            "#..#",
            "#...#",
            "#..##",
            "##.#",
            "   #"
        };

        /// <summary>The classic desktop arrow (12×19).</summary>
        public static CursorStyle Arrow(Color fill, Color outline) => new(ArrowRows, fill, outline);

        /// <summary>A small 8-bit style arrow drawn with 2×2 pixels — for retro/pixel themes.</summary>
        public static CursorStyle PixelArrow(Color fill, Color outline) => new(PixelArrowRows, fill, outline, 2);

        /// <summary>
        ///     Draws the pointer with its hotspot at <paramref name="position"/> (render pixels);
        ///     <paramref name="scale"/> is render pixels per layout unit.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Vector2 position, float scale, float opacity = 1)
        {
            var size = PixelSize * scale;
            var pixel = Primitives.Pixel(spriteBatch);
            var fill = Brush.ApplyOpacity(Fill, opacity);
            var outline = Brush.ApplyOpacity(Outline, opacity);
            for (var row = 0; row < Rows.Length; row++)
            {
                var line = Rows[row];
                var start = 0;
                while (start < line.Length)
                {
                    var c = line[start];
                    var end = start;
                    while (end < line.Length && line[end] == c)
                        end++;
                    if (c != ' ')
                        spriteBatch.Draw(pixel, new Rect(position.X + start * size, position.Y + row * size, (end - start) * size, size), c == '#' ? outline : fill);
                    start = end;
                }
            }
        }
    }
}
