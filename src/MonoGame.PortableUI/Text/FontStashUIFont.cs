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

        /// <summary>Number of distinct pixel sizes requested so far (each owns glyphs in the atlas).</summary>
        public int CachedSizeCount => _sizes.Count;

        /// <summary>Creates a font from TTF/OTF data; later sources are fallbacks for missing characters.</summary>
        public static FontStashUIFont FromData(float defaultSize, params byte[][] fontData)
        {
            var system = new FontSystem();
            foreach (var data in fontData)
                system.AddFont(data);
            return new FontStashUIFont(system, defaultSize, ownsFontSystem: true);
        }

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
            var size = Math.Clamp(MathF.Round(pixelSize / SizeStep) * SizeStep, MinSize, MaxSize);
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
            var raster = RasterScale(scale);
            GetFont(Size(pixelSize) * raster).DrawText(spriteBatch, text, position, color, scale: scale / raster * Correction(Size(pixelSize) * raster));
        }

        public override void DrawString(SpriteBatch spriteBatch, StringBuilder text, Vector2 position, Color color, float pixelSize, Vector2 scale)
        {
            var raster = RasterScale(scale);
            GetFont(Size(pixelSize) * raster).DrawText(spriteBatch, text, position, color, scale: scale / raster * Correction(Size(pixelSize) * raster));
        }

        public void Dispose()
        {
            _sizes.Clear();
            if (_ownsSystem)
                FontSystem.Dispose();
        }

        private float Size(float size) => (size > 0 ? size : DefaultSize) * PixelsPerPoint;

        // Rounding the raster size changes the glyph size slightly; this restores the exact size.
        private float Correction(float pixelSize)
        {
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
