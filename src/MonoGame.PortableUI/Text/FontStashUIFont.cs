using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Text
{
    /// <summary>
    ///     <see cref="UIFont"/> backed by a FontStashSharp <see cref="FontSystem"/>: glyphs are
    ///     rasterized at the requested size on first use, so text is sharp at any size and every
    ///     character the font files cover renders.
    /// </summary>
    /// <example>
    ///     <code>
    ///     var font = FontStashUIFont.FromFiles(16, "Content/Fonts/NotoSans-Regular.ttf");
    ///     FontManager.DefaultDynamicFont = font;      // every text control
    ///     label.DynamicFont = font;                   // or a single one
    ///     </code>
    /// </example>
    public sealed class FontStashUIFont : UIFont, IDisposable
    {
        // Sizes are rounded to 1/4 px so animated scales do not create an atlas entry per frame.
        private const float SizeStep = 0.25f;
        private const float MinSize = 1f;
        private const float MaxSize = 512f;

        private readonly Dictionary<float, SpriteFontBase> _sizes = new Dictionary<float, SpriteFontBase>();
        private readonly bool _ownsSystem;

        public FontStashUIFont(FontSystem fontSystem, float defaultSize = 16, bool ownsFontSystem = false)
        {
            FontSystem = fontSystem ?? throw new ArgumentNullException(nameof(fontSystem));
            DefaultSize = defaultSize > 0 ? defaultSize : 16;
            _ownsSystem = ownsFontSystem;
        }

        public FontSystem FontSystem { get; }

        /// <summary>
        ///     Converts the toolkit's text sizes to FontStashSharp pixel sizes. Sizes are points like a
        ///     SpriteFont's <c>Size</c> (MonoGame bakes 1 pt = 96/72 px), so the default makes TextSize 24
        ///     look the same in both backends. Set 1 to treat sizes as pixels.
        /// </summary>
        public float PixelsPerPoint { get; init; } = 96f / 72f;

        public override float DefaultSize { get; }

        /// <summary>
        ///     The font's full height (ascent to descent) per em. Text sizes are em sizes like a SpriteFont's;
        ///     FontStashSharp sizes by the full height, so sizes are multiplied by this to match. 1 = treat
        ///     sizes as FontStashSharp heights. <see cref="FromData"/> reads it from the font.
        /// </summary>
        public float HeightPerEm { get; init; } = 1;

        /// <summary>
        ///     For pixel-art fonts: the pixel height at which one font pixel is one screen pixel (8 for
        ///     Press Start 2P, 16 for Px437 VGA). Text is then rasterized only at whole multiples of it and
        ///     never resampled, so it stays crisp, without anti-aliased edges; sizes in between round to the
        ///     nearest multiple. 0 = a regular font. <see cref="FromData"/> detects it from the outlines.
        /// </summary>
        public float PixelGrid { get; init; }

        /// <summary>Number of distinct pixel sizes requested so far (each owns glyphs in the atlas).</summary>
        public int CachedSizeCount => _sizes.Count;

        /// <summary>Creates a font from TTF/OTF data; later sources are fallbacks for missing characters.</summary>
        public static FontStashUIFont FromData(float defaultSize, params byte[][] fontData)
        {
            var system = new FontSystem();
            foreach (var data in fontData)
                system.AddFont(data);
            // The first font decides: a pixel font on a coarse outline grid is drawn at whole multiples of it.
            var grid = fontData.Length > 0 ? PixelFontDetector.DetectGrid(fontData[0]) : 0;
            var heightPerEm = fontData.Length > 0 ? PixelFontDetector.HeightPerEm(fontData[0]) : 1;
            return new FontStashUIFont(system, defaultSize, ownsFontSystem: true) { PixelGrid = grid, HeightPerEm = heightPerEm };
        }

        /// <summary>The same font system with every size scaled by <paramref name="scale"/> (shares the glyph atlas owner).</summary>
        internal FontStashUIFont WithSizeScale(float scale)
            => new FontStashUIFont(FontSystem, DefaultSize, _ownsSystem)
            {
                PixelGrid = PixelGrid,
                HeightPerEm = HeightPerEm,
                PixelsPerPoint = PixelsPerPoint * scale
            };

        /// <summary>Creates a font from streams (e.g. <c>TitleContainer.OpenStream</c>, which also reads Android assets).</summary>
        public static FontStashUIFont FromStreams(float defaultSize, params Stream[] streams)
        {
            var data = new byte[streams.Length][];
            for (var i = 0; i < streams.Length; i++)
            {
                using var buffer = new MemoryStream();
                streams[i].CopyTo(buffer);
                data[i] = buffer.ToArray();
            }
            return FromData(defaultSize, data);
        }

        public static FontStashUIFont FromFiles(float defaultSize, params string[] paths)
        {
            var data = new byte[paths.Length][];
            for (var i = 0; i < paths.Length; i++)
                data[i] = File.ReadAllBytes(paths[i]);
            return FromData(defaultSize, data);
        }

        /// <summary>The FontStashSharp font object for a (rounded) pixel size.</summary>
        public SpriteFontBase GetFont(float pixelSize)
        {
            var size = PixelGrid > 0 ? SnapToGrid(pixelSize) : Math.Clamp(MathF.Round(pixelSize / SizeStep) * SizeStep, MinSize, MaxSize);
            if (!_sizes.TryGetValue(size, out var font))
            {
                font = FontSystem.GetFont(size);
                _sizes[size] = font;
            }
            return font;
        }

        public override float GetLineHeight(float pixelSize) => GetFont(Size(pixelSize)).LineHeight * Correction(Size(pixelSize));

        public override Vector2 MeasureString(string text, float pixelSize)
        {
            if (string.IsNullOrEmpty(text))
                return new Vector2(0, GetLineHeight(pixelSize));
            return WithLineHeight(GetFont(Size(pixelSize)).MeasureString(text) * Correction(Size(pixelSize)), pixelSize);
        }

        public override Vector2 MeasureString(StringBuilder text, float pixelSize)
        {
            if (text == null || text.Length == 0)
                return new Vector2(0, GetLineHeight(pixelSize));
            return WithLineHeight(GetFont(Size(pixelSize)).MeasureString(text) * Correction(Size(pixelSize)), pixelSize);
        }

        public override void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float pixelSize, Vector2 scale)
        {
            if (PixelGrid > 0)
            {
                DrawPixelString(spriteBatch, text, null, position, color, pixelSize, scale);
                return;
            }

            var raster = RasterScale(scale);
            GetFont(Size(pixelSize) * raster).DrawText(spriteBatch, text, position, color, scale: scale / raster * Correction(Size(pixelSize) * raster));
        }

        public override void DrawString(SpriteBatch spriteBatch, StringBuilder text, Vector2 position, Color color, float pixelSize, Vector2 scale)
        {
            if (PixelGrid > 0)
            {
                DrawPixelString(spriteBatch, null, text, position, color, pixelSize, scale);
                return;
            }

            var raster = RasterScale(scale);
            GetFont(Size(pixelSize) * raster).DrawText(spriteBatch, text, position, color, scale: scale / raster * Correction(Size(pixelSize) * raster));
        }

        public override void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float pixelSize, Vector2 scale, Vector2 rasterScale)
        {
            if (PixelGrid > 0)
            {
                DrawPixelString(spriteBatch, text, null, position, color, pixelSize, scale);
                return;
            }

            var raster = RasterScale(rasterScale);
            // Scaled glyph quads take the atlas padding along, so their soft edges are not cut at fractional positions.
            _paddedRenderer.Batch = spriteBatch;
            GetFont(Size(pixelSize) * raster).DrawText(_paddedRenderer, text, position, color, scale: scale / raster * Correction(Size(pixelSize) * raster));
            _paddedRenderer.Batch = null;
        }

        public override void DrawString(SpriteBatch spriteBatch, StringBuilder text, Vector2 position, Color color, float pixelSize, Vector2 scale, Vector2 rasterScale)
        {
            if (PixelGrid > 0)
            {
                DrawPixelString(spriteBatch, null, text, position, color, pixelSize, scale);
                return;
            }

            var raster = RasterScale(rasterScale);
            GetFont(Size(pixelSize) * raster).DrawText(spriteBatch, text, position, color, scale: scale / raster * Correction(Size(pixelSize) * raster));
        }

        private readonly PaddedRenderer _paddedRenderer = new PaddedRenderer();

        /// <summary>
        ///     Draws FontStashSharp's glyph quads one atlas texel larger on every side (the atlas pads glyphs
        ///     with empty texels), so a glyph scaled or placed at a fractional position keeps its filtered edge
        ///     instead of losing it at the quad border and hopping a pixel.
        /// </summary>
        private sealed class PaddedRenderer : FontStashSharp.Interfaces.IFontStashRenderer
        {
            public SpriteBatch? Batch;

            public GraphicsDevice GraphicsDevice => Batch!.GraphicsDevice;

            public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 scale, float depth)
            {
                if (sourceRectangle is not { } source || rotation != 0)
                {
                    Batch!.Draw(texture, position, sourceRectangle, color, rotation, Vector2.Zero, scale, SpriteEffects.None, depth);
                    return;
                }

                var left = source.Left > 0 ? 1 : 0;
                var top = source.Top > 0 ? 1 : 0;
                var right = source.Right < texture.Width ? 1 : 0;
                var bottom = source.Bottom < texture.Height ? 1 : 0;
                var padded = new Rectangle(source.Left - left, source.Top - top, source.Width + left + right, source.Height + top + bottom);
                Batch!.Draw(texture, position - new Vector2(left, top) * scale, padded, color, 0, Vector2.Zero, scale, SpriteEffects.None, depth);
            }
        }

        public void Dispose()
        {
            _sizes.Clear();
            if (_ownsSystem)
                FontSystem.Dispose();
        }

        private float Size(float size)
        {
            var pixels = (size > 0 ? size : DefaultSize) * PixelsPerPoint * HeightPerEm;
            return PixelGrid > 0 ? SnapToGrid(pixels) : pixels;
        }

        // Whole multiples of the pixel grid (at least one), where the font's pixels land on screen pixels.
        private float SnapToGrid(float pixels) => Math.Clamp(Math.Max(1, MathF.Round(pixels / PixelGrid)) * PixelGrid, MinSize, MaxSize);

        // A pixel font is never resampled: the render scale picks the next multiple of the grid, drawn at 1:1
        // from whole pixels. Uneven scales (squash) keep their ratio.
        private void DrawPixelString(SpriteBatch spriteBatch, string? text, StringBuilder? builder, Vector2 position, Color color, float pixelSize, Vector2 scale)
        {
            var raster = RasterScale(scale);
            var size = SnapToGrid(Size(pixelSize) * raster);
            var font = GetFont(size);
            var drawScale = scale / raster;
            var at = new Vector2(MathF.Round(position.X), MathF.Round(position.Y));
            if (text != null)
                font.DrawText(spriteBatch, text, at, color, scale: drawScale);
            else
                font.DrawText(spriteBatch, builder!, at, color, scale: drawScale);
        }

        // Rounding the raster size changes the glyph size slightly; this restores the exact size.
        private float Correction(float pixelSize)
        {
            if (PixelGrid > 0)
                return 1;
            var size = pixelSize;
            var rounded = Math.Clamp(MathF.Round(size / SizeStep) * SizeStep, MinSize, MaxSize);
            return size / rounded;
        }

        private static float RasterScale(Vector2 scale)
        {
            var s = Math.Max(Math.Abs(scale.X), Math.Abs(scale.Y));
            return s > 0.01f ? s : 1f;
        }

        // A single line measures at least one line height, like SpriteFont.MeasureString.
        private Vector2 WithLineHeight(Vector2 size, float pixelSize) => new Vector2(size.X, Math.Max(size.Y, GetLineHeight(pixelSize)));
    }
}
