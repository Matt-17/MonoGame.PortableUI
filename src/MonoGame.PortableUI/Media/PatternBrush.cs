using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    ///     Tiles a tiny procedural pixel pattern (pinstripes, checkerboard dithers, LCD grids).
    ///     The texture is generated lazily per device via <see cref="BrushTextureCache"/>, so the
    ///     brush itself is device-free and safe to build inside theme definitions.
    /// </summary>
    public sealed class PatternBrush : Brush
    {
        private readonly Color[] _pixels;
        private readonly int _patternWidth;
        private readonly int _patternHeight;
        private readonly int _hash;

        public PatternBrush(int patternWidth, int patternHeight, Color[] pixels, float scale = 1)
        {
            if (pixels == null || pixels.Length != patternWidth * patternHeight)
                throw new ArgumentException("Pixel data must match the pattern dimensions.", nameof(pixels));

            _patternWidth = Math.Max(1, patternWidth);
            _patternHeight = Math.Max(1, patternHeight);
            _pixels = pixels;
            Scale = Math.Max(0.25f, scale);

            var hash = new HashCode();
            hash.Add(_patternWidth);
            hash.Add(_patternHeight);
            foreach (var pixel in pixels)
                hash.Add(pixel.PackedValue);
            _hash = hash.ToHashCode();
        }

        public float Scale { get; }

        /// <summary>Horizontal 1px pinstripes: alternating base/stripe rows.</summary>
        public static PatternBrush Pinstripes(Color baseColor, Color stripeColor, int spacing = 3)
        {
            spacing = Math.Max(2, spacing);
            var pixels = new Color[spacing];
            for (var i = 0; i < spacing; i++)
                pixels[i] = i == spacing - 1 ? stripeColor : baseColor;
            return new PatternBrush(1, spacing, pixels);
        }

        /// <summary>2×2 50% checkerboard dither (classic 1-bit Mac look).</summary>
        public static PatternBrush Dither(Color a, Color b)
        {
            return new PatternBrush(2, 2, new[] { a, b, b, a });
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect)
        {
            Draw(spriteBatch, rect, 1);
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            // Pixel patterns stay crisp only on whole pixels (linear filtering would smear them).
            Rectangle snapped = rect;
            rect = new Rect(snapped.X, snapped.Y, snapped.Width, snapped.Height);
            if (rect.Width <= 0 || rect.Height <= 0)
                return;
            var texture = GetTexture(spriteBatch.GraphicsDevice);
            var tint = ApplyOpacity(Color.White, opacity);
            foreach (var tile in TileBrush.GetTileSegments(rect, texture.Width, texture.Height, Scale))
                spriteBatch.Draw(texture, tile.DestinationRect, tile.SourceRectangle, tint);
        }

        // The texture repeats the pattern up to about this size: tiling a 1×2 pinstripe directly
        // meant one sprite per two pixels (~450k sprites for a full screen).
        private const int TargetTileSize = 128;

        private Texture2D GetTexture(GraphicsDevice device)
        {
            var repeatX = Math.Max(1, TargetTileSize / _patternWidth);
            var repeatY = Math.Max(1, TargetTileSize / _patternHeight);
            var width = _patternWidth * repeatX;
            var height = _patternHeight * repeatY;
            var key = new BrushTextureCacheKey("pattern-v2", _hash, width, height);
            if (BrushTextureCache.TryGet(device, key, out var cached))
                return cached;
            return BrushTextureCache.GetOrCreate(device, key, graphicsDevice =>
            {
                var data = new Color[width * height];
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                        data[y * width + x] = Premultiply(_pixels[(y % _patternHeight) * _patternWidth + x % _patternWidth]);
                }

                var texture = new Texture2D(graphicsDevice, width, height);
                texture.SetData(data);
                return texture;
            });
        }
    }
}
