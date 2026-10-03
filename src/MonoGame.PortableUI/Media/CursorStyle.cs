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

        private CursorStyle(PathGeometry shape, Vector2 size, Vector2 hotspot, Color fill, Color outline, float outlineWidth)
        {
            Rows = Array.Empty<string>();
            Shape = shape;
            ShapeSize = size;
            Hotspot = hotspot;
            Fill = fill;
            Outline = outline;
            OutlineWidth = outlineWidth;
            PixelSize = 1;
        }

        private CursorStyle(bool invert, Color fill)
        {
            Rows = Array.Empty<string>();
            IsTextCell = true;
            InvertsCell = invert;
            Fill = fill;
            Outline = Color.Transparent;
            PixelSize = 1;
        }

        /// <summary>
        ///     A text-mode pointer: a whole character cell instead of an arrow (see
        ///     <see cref="ScreenEngineOptions.TextGrid"/>; without a grid the cell size comes from the
        ///     theme's font).
        /// </summary>
        public bool IsTextCell { get; }

        /// <summary>The text-mode cell is inverted (DOS); otherwise it is filled with <see cref="Fill"/>.</summary>
        public bool InvertsCell { get; }

        /// <summary>
        ///     The DOS text-mode mouse: the cell under the pointer shown inverted. (The real driver
        ///     XORed the cell's colour attribute; a host with a character buffer can draw exactly that
        ///     via <see cref="ScreenEngineOptions.TextCellCursorRenderer"/>.)
        /// </summary>
        public static CursorStyle TextCell() => new(true, Color.White);

        /// <summary>A text-mode pointer that fills the cell with a colour (e.g. a C64-style block).</summary>
        public static CursorStyle TextBlock(Color fill) => new(false, fill);

        /// <summary>Vector outline for smooth (anti-aliased) pointers; null for bitmap pointers.</summary>
        public PathGeometry? Shape { get; }

        /// <summary>Size of <see cref="Shape"/> in layout units (its view box maps onto it).</summary>
        public Vector2 ShapeSize { get; }

        /// <summary>Offset of the pointing tip inside <see cref="ShapeSize"/>, in layout units.</summary>
        public Vector2 Hotspot { get; }

        /// <summary>Outline width of <see cref="Shape"/> in view units.</summary>
        public float OutlineWidth { get; }

        /// <summary>
        ///     The modern, fine pointer of current desktops: smooth edges, rasterized at the screen's
        ///     real pixel density, so it stays thin on HiDPI instead of turning blocky.
        /// </summary>
        public static CursorStyle ModernArrow(Color fill, Color outline)
        {
            // 12×19 arrow in a view box padded by one unit for the outline.
            var shape = new PathGeometry(14, 21);
            shape.ViewBox = new RectangleF(-1, -1, 14, 21);
            shape.MoveTo(0, 0).LineTo(0, 16.2f).LineTo(3.9f, 12.6f).LineTo(6.5f, 18.6f).LineTo(9.1f, 17.5f)
                .LineTo(6.6f, 11.6f).LineTo(11.8f, 11.6f).Close();
            return new CursorStyle(shape, new Vector2(14, 21), new Vector2(1, 1), fill, outline, 1.1f);
        }

        /// <summary>The classic 1-bit desktop arrow (12×19), drawn pixel for pixel.</summary>
        public static CursorStyle Arrow(Color fill, Color outline) => new(ArrowRows, fill, outline);

        /// <summary>A small 8-bit style arrow drawn with 2×2 pixels — for retro/pixel themes.</summary>
        public static CursorStyle PixelArrow(Color fill, Color outline) => new(PixelArrowRows, fill, outline, 2);

        private Texture2D? _texture;
        private GraphicsDevice? _textureDevice;
        private int _textureGeneration;
        private Point _textureSize;

        /// <summary>
        ///     Draws the pointer with its hotspot at <paramref name="position"/> (render pixels);
        ///     <paramref name="scale"/> is render pixels per layout unit.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Vector2 position, float scale, float opacity = 1)
        {
            if (Shape != null)
            {
                DrawShape(spriteBatch, position, scale, opacity);
                return;
            }

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

        private void DrawShape(SpriteBatch spriteBatch, Vector2 position, float scale, float opacity)
        {
            // Rasterized once per pixel size (and again after a device reset): real-resolution edges.
            var device = spriteBatch.GraphicsDevice;
            var size = new Point(Math.Max(1, (int)MathF.Round(ShapeSize.X * scale)), Math.Max(1, (int)MathF.Round(ShapeSize.Y * scale)));
            var generation = PathRasterizer.GetResetGeneration(device);
            if (_texture == null || _texture.IsDisposed || !ReferenceEquals(_textureDevice, device) || _textureGeneration != generation || _textureSize != size)
            {
                _texture?.Dispose();
                _texture = PathRasterizer.CreateTexture(device, Shape!, size.X, size.Y, Fill, Outline, OutlineWidth);
                _textureDevice = device;
                _textureGeneration = generation;
                _textureSize = size;
            }

            var origin = new Vector2(MathF.Round(position.X - Hotspot.X * scale), MathF.Round(position.Y - Hotspot.Y * scale));
            spriteBatch.Draw(_texture, origin, Color.White * MathHelper.Clamp(opacity, 0, 1));
        }
    }
}
