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
        ///     sizes as FontStashSharp heights. <see cref="FromData(float, byte[][])"/> reads it from the font.
        /// </summary>
        public float HeightPerEm { get; init; } = 1;

        /// <summary>
        ///     For pixel-art fonts: the pixel height at which one font pixel is one screen pixel (8 for
        ///     Press Start 2P, 16 for Px437 VGA). Text is then rasterized only at whole multiples of it and
        ///     never resampled, so it stays crisp, without anti-aliased edges; sizes in between round to the
        ///     nearest multiple. 0 = a regular font. <see cref="FromData(float, byte[][])"/> detects it from the outlines.
        /// </summary>
        public float PixelGrid { get; init; }

        /// <summary>
        ///     How lines are sized and where the baseline sits: <see cref="TextLineMetrics.Font"/> (default,
        ///     FontStashSharp's metrics) or <see cref="TextLineMetrics.Android"/> (whole device pixels like an
        ///     Android TextView). Android metrics need the font tables, read by <see cref="FromData(float, byte[][])"/>.
        /// </summary>
        public TextLineMetrics LineMetrics { get; init; }

        internal FontVerticalMetrics? VerticalMetrics { get; init; }

        private bool UsesAndroidMetrics => LineMetrics == TextLineMetrics.Android && VerticalMetrics != null && PixelGrid <= 0;

        /// <summary>Number of distinct pixel sizes requested so far (each owns glyphs in the atlas).</summary>
        public int CachedSizeCount => _sizes.Count;

        /// <summary>Creates a font from TTF/OTF data; later sources are fallbacks for missing characters.</summary>
        public static FontStashUIFont FromData(float defaultSize, params byte[][] fontData)
            => FromData(defaultSize, TextLineMetrics.Font, fontData);

        /// <summary>Creates a font from TTF/OTF data with the given <see cref="TextLineMetrics"/>.</summary>
        public static FontStashUIFont FromData(float defaultSize, TextLineMetrics lineMetrics, params byte[][] fontData)
        {
            var system = new FontSystem();
            foreach (var data in fontData)
                system.AddFont(data);
            // The first font decides: a pixel font on a coarse outline grid is drawn at whole multiples of it.
            var grid = fontData.Length > 0 ? PixelFontDetector.DetectGrid(fontData[0]) : 0;
            var heightPerEm = fontData.Length > 0 ? PixelFontDetector.HeightPerEm(fontData[0]) : 1;
            FontVerticalMetrics? metrics = fontData.Length > 0 && PixelFontDetector.TryReadVerticalMetrics(fontData[0], out var read) ? read : null;
            return new FontStashUIFont(system, defaultSize, ownsFontSystem: true)
            {
                PixelGrid = grid,
                HeightPerEm = heightPerEm,
                LineMetrics = lineMetrics,
                VerticalMetrics = metrics
            };
        }

        /// <summary>The same font system with every size scaled by <paramref name="scale"/> (shares the glyph atlas owner).</summary>
        internal FontStashUIFont WithSizeScale(float scale)
            => new FontStashUIFont(FontSystem, DefaultSize, _ownsSystem)
            {
                PixelGrid = PixelGrid,
                HeightPerEm = HeightPerEm,
                PixelsPerPoint = PixelsPerPoint * scale,
                LineMetrics = LineMetrics,
                VerticalMetrics = VerticalMetrics
            };

        /// <summary>Creates a font from streams (e.g. <c>TitleContainer.OpenStream</c>, which also reads Android assets).</summary>
        public static FontStashUIFont FromStreams(float defaultSize, params Stream[] streams)
            => FromStreams(defaultSize, TextLineMetrics.Font, streams);

        /// <summary>Creates a font from streams with the given <see cref="TextLineMetrics"/>.</summary>
        public static FontStashUIFont FromStreams(float defaultSize, TextLineMetrics lineMetrics, params Stream[] streams)
        {
            var data = new byte[streams.Length][];
            for (var i = 0; i < streams.Length; i++)
            {
                using var buffer = new MemoryStream();
                streams[i].CopyTo(buffer);
                data[i] = buffer.ToArray();
            }
            return FromData(defaultSize, lineMetrics, data);
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

        public override float GetLineHeight(float pixelSize)
        {
            if (UsesAndroidMetrics)
            {
                var devicePixels = DevicePixelsPerUnit();
                var (top, bottom) = AndroidLine(Em(pixelSize) * devicePixels);
                return (top + bottom) / devicePixels;
            }
            return GetFont(Size(pixelSize)).LineHeight * Correction(Size(pixelSize));
        }

        public override float GetLineSpacing(float pixelSize)
        {
            if (!UsesAndroidMetrics)
                return GetLineHeight(pixelSize);
            // Between lines Android uses FontMetricsInt ascent + descent: the hhea values, each rounded.
            var devicePixels = DevicePixelsPerUnit();
            var em = Em(pixelSize) * devicePixels;
            var m = VerticalMetrics!.Value;
            return (MathF.Round(m.Ascent * em) + MathF.Round(m.Descent * em)) / devicePixels;
        }

        // Text is measured at the resolution it is drawn at: FontStashSharp rounds every glyph advance to whole
        // pixels of the size it rasterizes, which at the small layout-unit size (14 sp is about 19 units)
        // added up to several percent of a line's width (labels wrapped early, buttons grew). Pixel fonts
        // keep their grid.
        private float MeasureRaster() => PixelGrid > 0 ? 1f : DevicePixelsPerUnit();

        // Device pixels per layout unit: the drawing engine's scale, else the main engine's.
        private static float DevicePixelsPerUnit()
        {
            var scale = ScreenEngine.DrawingEngine?.DrawScale ?? ScreenEngine.Instance?.RenderScale ?? 1f;
            return scale > 0.01f ? scale : 1f;
        }

        // The em size in layout units (sizes are points scaled by PixelsPerPoint).
        private float Em(float pixelSize) => (pixelSize > 0 ? pixelSize : DefaultSize) * PixelsPerPoint;

        // A single line's box with Android's includeFontPadding: the larger of hhea and glyph box above and
        // below the baseline, each rounded up to whole device pixels (matches TextView within a pixel).
        private (float Top, float Bottom) AndroidLine(float emDevicePixels)
        {
            var m = VerticalMetrics!.Value;
            return (Ceiling(Math.Max(m.Ascent, m.Top) * emDevicePixels), Ceiling(Math.Max(m.Descent, m.Bottom) * emDevicePixels));
        }

        // Android (FreeType with hinting) renders glyphs at a whole-pixel em: 14 sp at density 2.75 (38.5 px) is
        // set at 38 px, about 1.3 % narrower; line metrics keep the exact size. Measured against MAUI labels.
        private float GlyphPixelSize(float pixelSize, float devicePixels)
        {
            if (!UsesAndroidMetrics || devicePixels <= 0)
                return pixelSize;
            var em = Em(pixelSize) * devicePixels;
            var whole = MathF.Floor(em + 0.001f);
            return whole > 0 ? (pixelSize > 0 ? pixelSize : DefaultSize) * (whole / em) : pixelSize;
        }

        // Rounds up, ignoring float noise just above a whole pixel.
        private static float Ceiling(float value) => MathF.Ceiling(value - 0.001f);

        // Android metrics: moves FontStashSharp's baseline (hhea ascent below the top) to the rounded Android one.
        private Vector2 AndroidBaseline(Vector2 position, float pixelSize, Vector2 scale)
        {
            if (!UsesAndroidMetrics)
                return position;
            var devicePixels = Math.Abs(scale.Y) > 0.01f ? Math.Abs(scale.Y) : 1f;
            var em = Em(pixelSize);
            var android = AndroidLine(em * devicePixels).Top / devicePixels;
            var fontStash = Em(GlyphPixelSize(pixelSize, devicePixels)) * VerticalMetrics!.Value.Ascent;
            return new Vector2(position.X, position.Y + (android - fontStash) * scale.Y);
        }

        public override Vector2 MeasureString(string text, float pixelSize)
        {
            if (string.IsNullOrEmpty(text))
                return new Vector2(0, GetLineHeight(pixelSize));
            var raster = MeasureRaster();
            var size = Size(GlyphPixelSize(pixelSize, raster)) * raster;
            return WithLineHeight(GetFont(size).MeasureString(text) * (Correction(size) / raster), pixelSize);
        }

        public override Vector2 MeasureString(StringBuilder text, float pixelSize)
        {
            if (text == null || text.Length == 0)
                return new Vector2(0, GetLineHeight(pixelSize));
            var raster = MeasureRaster();
            var size = Size(GlyphPixelSize(pixelSize, raster)) * raster;
            return WithLineHeight(GetFont(size).MeasureString(text) * (Correction(size) / raster), pixelSize);
        }

        public override void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float pixelSize, Vector2 scale)
        {
            if (PixelGrid > 0)
            {
                DrawPixelString(spriteBatch, text, null, position, color, pixelSize, scale);
                return;
            }

            var raster = RasterScale(scale);
            position = AndroidBaseline(position, pixelSize, scale);
            pixelSize = GlyphPixelSize(pixelSize, raster);
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
            position = AndroidBaseline(position, pixelSize, scale);
            pixelSize = GlyphPixelSize(pixelSize, raster);
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
            position = AndroidBaseline(position, pixelSize, scale);
            pixelSize = GlyphPixelSize(pixelSize, raster);
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
            position = AndroidBaseline(position, pixelSize, scale);
            pixelSize = GlyphPixelSize(pixelSize, raster);
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
