using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public enum ImageBrushTileMode
    {
        None,
        Tile,
        FlipXY
    }

    public class ImageBrush : Brush
    {
        // Stable per-texture id so cached rounded variants key off identity (Texture2D has no id of
        // its own and reference hashes could collide). Weak so it never keeps a texture alive.
        private static readonly ConditionalWeakTable<Texture2D, object> SourceIds = new();
        private static int _nextSourceId;

        private Texture2D? _source;
        public Texture2D? Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }

        private Stretch _stretch = Stretch.Fill;
        public Stretch Stretch
        {
            get => _stretch;
            set => SetProperty(ref _stretch, value);
        }

        private ImageBrushTileMode _tileMode;
        public ImageBrushTileMode TileMode
        {
            get => _tileMode;
            set => SetProperty(ref _tileMode, value);
        }

        private Rectangle? _sourceRect;
        public Rectangle? SourceRect
        {
            get => _sourceRect;
            set => SetProperty(ref _sourceRect, value);
        }

        private Color _tintColor = Color.White;
        public Color TintColor
        {
            get => _tintColor;
            set => SetProperty(ref _tintColor, value);
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect)
        {
            MarkDrawn();
            Draw(spriteBatch, rect, 1);
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            MarkDrawn();
            Draw(spriteBatch, new BrushContext(rect, 0, opacity, spriteBatch.GraphicsDevice));
        }

        public override void Draw(SpriteBatch spriteBatch, in BrushContext context)
        {
            MarkDrawn();
            if (Source == null || context.Rect.Width <= 0 || context.Rect.Height <= 0)
                return;

            if (TileMode == ImageBrushTileMode.Tile || TileMode == ImageBrushTileMode.FlipXY)
            {
                DrawTiled(spriteBatch, context.Rect, context.Opacity);
                return;
            }

            var sourceRect = GetSourceRect();
            var tint = ApplyOpacity(TintColor, context.Opacity);

            // Source.Format guard: the rounded path reads pixels with GetData<Color>, which only
            // works on an uncompressed Color texture; anything else falls through to a plain draw.
            if (!context.Radius.IsEmpty && Source.Format == SurfaceFormat.Color)
            {
                // Rounded target: the image must follow the corners, but spriteBatch.Draw only fills
                // a rectangle. Build (and cache) a target-sized copy that has the image sampled into
                // it and the corners masked out — the same CPU-mask trick the gradient brushes use —
                // then draw that once. Tint/opacity stay on the draw call so the cache is stable.
                var width = Math.Max(1, (int)Math.Ceiling(context.Rect.Width));
                var height = Math.Max(1, (int)Math.Ceiling(context.Rect.Height));
                var radius = context.Radius;
                var key = CreateRoundedCacheKey(width, height, sourceRect, radius);
                if (!BrushTextureCache.TryGet(spriteBatch.GraphicsDevice, key, out var rounded))
                    rounded = CreateCachedRoundedTexture(spriteBatch.GraphicsDevice, key, width, height, sourceRect, radius);
                spriteBatch.Draw(rounded, context.Rect, tint);
                return;
            }

            if (Stretch == Stretch.UniformToFill)
            {
                // Fill the target while preserving aspect by cropping the *source* to the target's
                // aspect and drawing into the exact target rect. Scaling an oversized destination
                // instead would spill outside the control (its scissor is widened for drop shadows),
                // so the overflow must be clipped here, in the brush, not left to the scissor.
                var croppedSource = GetUniformToFillSource(context.Rect, sourceRect);
                spriteBatch.Draw(Source, context.Rect, croppedSource, tint);
                return;
            }

            var destination = GetStretchedRect(context.Rect, sourceRect.Width, sourceRect.Height);
            spriteBatch.Draw(Source, destination, sourceRect, tint);
        }

        /// <summary>
        /// The centred sub-rectangle of <paramref name="source"/> whose aspect matches
        /// <paramref name="targetRect"/>, so drawing it into the target fills it without distortion
        /// and clips the overflow (the UniformToFill crop).
        /// </summary>
        internal static Rectangle GetUniformToFillSource(Rect targetRect, Rectangle source)
        {
            if (source.Width <= 0 || source.Height <= 0 || targetRect.Width <= 0 || targetRect.Height <= 0)
                return source;

            var widthScale = targetRect.Width / source.Width;
            var heightScale = targetRect.Height / source.Height;
            var fillScale = Math.Max(widthScale, heightScale);

            var visibleWidth = (int)Math.Round(targetRect.Width / fillScale);
            var visibleHeight = (int)Math.Round(targetRect.Height / fillScale);
            visibleWidth = Math.Clamp(visibleWidth, 1, source.Width);
            visibleHeight = Math.Clamp(visibleHeight, 1, source.Height);

            var left = source.Left + (source.Width - visibleWidth) / 2;
            var top = source.Top + (source.Height - visibleHeight) / 2;
            return new Rectangle(left, top, visibleWidth, visibleHeight);
        }

        internal Rect GetStretchedRect(Rect targetRect, int sourceWidth, int sourceHeight)
        {
            if (sourceWidth <= 0 || sourceHeight <= 0)
                return Rect.Empty;

            var widthScale = targetRect.Width / sourceWidth;
            var heightScale = targetRect.Height / sourceHeight;
            float width;
            float height;

            switch (Stretch)
            {
                case Stretch.None:
                    width = sourceWidth;
                    height = sourceHeight;
                    break;
                case Stretch.Uniform:
                    var uniformScale = Math.Min(widthScale, heightScale);
                    width = sourceWidth * uniformScale;
                    height = sourceHeight * uniformScale;
                    break;
                case Stretch.UniformToFill:
                    var fillScale = Math.Max(widthScale, heightScale);
                    width = sourceWidth * fillScale;
                    height = sourceHeight * fillScale;
                    break;
                case Stretch.Fill:
                    width = targetRect.Width;
                    height = targetRect.Height;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new Rect(
                targetRect.Left + (targetRect.Width - width) / 2,
                targetRect.Top + (targetRect.Height - height) / 2,
                width,
                height);
        }

        private BrushTextureCacheKey CreateRoundedCacheKey(int width, int height, Rectangle source, CornerRadius radius)
        {
            return new BrushTextureCacheKey(
                "image-rounded",
                width,
                height,
                Source != null ? GetSourceId(Source) : 0,
                HashCode.Combine((int)Stretch, radius, source, TintColor));
        }

        // Miss path only, so the closure over the size/source/radius is not allocated per draw.
        private Texture2D CreateCachedRoundedTexture(GraphicsDevice device, BrushTextureCacheKey key, int width, int height, Rectangle source, CornerRadius radius)
        {
            return BrushTextureCache.GetOrCreate(device, key, graphicsDevice => CreateRoundedTexture(graphicsDevice, width, height, source, radius));
        }

        /// <summary>
        /// Builds a <paramref name="width"/>×<paramref name="height"/> texture with the image sampled
        /// into it per the current <see cref="Stretch"/> and its corners alpha-masked to
        /// <paramref name="radius"/>, so it can be drawn as a rounded fill with a single call.
        /// </summary>
        private Texture2D CreateRoundedTexture(GraphicsDevice graphicsDevice, int width, int height, Rectangle source, CornerRadius radius)
        {
            var image = Source!;
            var imagePixels = new Color[image.Width * image.Height];
            image.GetData(imagePixels);

            // Where the source lands inside the target (oversized/centred for UniformToFill, inset for
            // Uniform, exact for Fill); we invert this per pixel to find the source texel to sample.
            var placement = GetStretchedRect(new Rect(0, 0, width, height), source.Width, source.Height);
            var data = new Color[width * height];

            if (placement.Width > 0 && placement.Height > 0)
            {
                for (var y = 0; y < height; y++)
                {
                    var v = (y + 0.5f - placement.Top) / placement.Height;
                    if (v < 0 || v >= 1)
                        continue;

                    var sampleY = Math.Clamp(source.Top + (int)(v * source.Height), 0, image.Height - 1);
                    var rowOffset = sampleY * image.Width;
                    var targetRow = y * width;

                    for (var x = 0; x < width; x++)
                    {
                        var u = (x + 0.5f - placement.Left) / placement.Width;
                        if (u < 0 || u >= 1)
                            continue;

                        var sampleX = Math.Clamp(source.Left + (int)(u * source.Width), 0, image.Width - 1);
                        data[targetRow + x] = imagePixels[rowOffset + sampleX];
                    }
                }
            }

            RoundedRectRenderer.ApplyCornerMask(data, width, height, radius);
            var texture = new Texture2D(graphicsDevice, width, height);
            texture.SetData(data);
            return texture;
        }

        private static int GetSourceId(Texture2D texture)
        {
            if (SourceIds.TryGetValue(texture, out var boxed))
                return (int)boxed;

            var id = System.Threading.Interlocked.Increment(ref _nextSourceId);
            SourceIds.AddOrUpdate(texture, id);
            return id;
        }

        private void DrawTiled(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            if (Source == null)
                return;

            var sourceRect = GetSourceRect();
            for (var top = rect.Top; top < rect.Bottom; top += sourceRect.Height)
            {
                var height = Math.Min(sourceRect.Height, rect.Bottom - top);
                for (var left = rect.Left; left < rect.Right; left += sourceRect.Width)
                {
                    var width = Math.Min(sourceRect.Width, rect.Right - left);
                    var segmentSource = new Rectangle(sourceRect.Left, sourceRect.Top, (int)Math.Ceiling(width), (int)Math.Ceiling(height));
                    var effects = TileMode == ImageBrushTileMode.FlipXY && IsOddTile(rect, left, top, sourceRect)
                        ? SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically
                        : SpriteEffects.None;
                    spriteBatch.Draw(Source, new Rect(left, top, width, height), segmentSource, ApplyOpacity(TintColor, opacity), 0, Vector2.Zero, effects, 0);
                }
            }
        }

        private Rectangle GetSourceRect()
        {
            if (Source == null)
                return Rectangle.Empty;

            var sourceRect = SourceRect ?? new Rectangle(0, 0, Source.Width, Source.Height);
            var width = Math.Max(0, Math.Min(sourceRect.Width, Source.Width - sourceRect.Left));
            var height = Math.Max(0, Math.Min(sourceRect.Height, Source.Height - sourceRect.Top));
            return new Rectangle(
                Math.Max(0, sourceRect.Left),
                Math.Max(0, sourceRect.Top),
                width,
                height);
        }

        private static bool IsOddTile(Rect targetRect, float left, float top, Rectangle sourceRect)
        {
            var column = (int)((left - targetRect.Left) / Math.Max(1, sourceRect.Width));
            var row = (int)((top - targetRect.Top) / Math.Max(1, sourceRect.Height));
            return (column + row) % 2 != 0;
        }
    }
}
