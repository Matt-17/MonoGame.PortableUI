using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     Draws a <see cref="PathGeometry"/> filled and/or stroked, scaled to the control's arranged
    ///     size. The shape is rasterized once per pixel size (and on geometry changes) and then drawn as a
    ///     sprite, so it is crisp at any size and costs one draw per frame. A fill-only shape (an icon) is
    ///     rasterized in white and tinted when drawn, so changing its <see cref="Fill"/> costs nothing; that white
    ///     mask is shared by every fill-only shape of the same geometry and pixel size, so a list of a hundred
    ///     identical icons rasterizes once (<see cref="Prepare"/> can do it ahead of time).
    /// </summary>
    public class PathShape : Control
    {
        private Texture2D? _texture;
        private int _textureWidth;
        private int _textureHeight;
        private int _textureVersion = -1;
        private PathGeometry? _geometry;
        private Color? _fill = Color.White;
        private Color? _stroke;
        private float _strokeWidth = 1;
        private bool _dirty = true;
        private int _resetGeneration;

        // Fill-only masks, shared: per geometry, by pixel size. A geometry that is gone takes its masks with it.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<PathGeometry, System.Collections.Generic.Dictionary<(int Width, int Height), Mask>> Masks = new();

        private sealed class Mask
        {
            public Texture2D Texture = null!;
            public int Version;
            public int ResetGeneration;
        }

        /// <summary>
        ///     Rasterizes the shared white mask of <paramref name="geometry"/> at <paramref name="width"/>×
        ///     <paramref name="height"/> device pixels now, so the first fill-only shape drawn at that size does not
        ///     pay for it (a page's icons before the page opens, say).
        /// </summary>
        public static void Prepare(GraphicsDevice device, PathGeometry geometry, int width, int height)
        {
            if (device == null || geometry == null || width < 1 || height < 1)
                return;
            SharedMask(device, geometry, width, height);
        }

        private static Texture2D SharedMask(GraphicsDevice device, PathGeometry geometry, int width, int height)
        {
            var masks = Masks.GetValue(geometry, static _ => new System.Collections.Generic.Dictionary<(int, int), Mask>());
            var generation = PathRasterizer.GetResetGeneration(device);
            lock (masks)
            {
                if (masks.TryGetValue((width, height), out var mask) && !mask.Texture.IsDisposed && mask.Version == geometry.Version
                    && mask.ResetGeneration == generation && ReferenceEquals(mask.Texture.GraphicsDevice, device))
                    return mask.Texture;
                // A changed geometry makes all its masks stale, not just this size.
                if (mask != null && mask.Version != geometry.Version)
                {
                    foreach (var stale in masks.Values)
                        stale.Texture.Dispose();
                    masks.Clear();
                }
                else
                    mask?.Texture.Dispose();
                mask = new Mask
                {
                    Texture = PathRasterizer.CreateTexture(device, geometry, width, height, Color.White),
                    Version = geometry.Version,
                    ResetGeneration = generation
                };
                masks[(width, height)] = mask;
                return mask.Texture;
            }
        }

        public PathShape()
        {
            IsFocusable = false;
        }

        public PathGeometry? Geometry
        {
            get => _geometry;
            set { _geometry = value; _dirty = true; }
        }

        public Color? Fill
        {
            get => _fill;
            set
            {
                // Tinted at draw time while there is no stroke; only a change between some fill and none re-rasterizes.
                if (_stroke != null || value.HasValue != _fill.HasValue)
                    _dirty = true;
                _fill = value;
                RequestRedraw();
            }
        }

        public Color? Stroke
        {
            get => _stroke;
            set { _stroke = value; _dirty = true; }
        }

        /// <summary>Stroke width in the geometry's view-box units.</summary>
        public float StrokeWidth
        {
            get => _strokeWidth;
            set { _strokeWidth = value; _dirty = true; }
        }

        public override Size MeasureLayout()
        {
            if (IsGoneShown)
                return Size.Empty;
            // Without explicit size the shape takes its view box size.
            var view = Geometry?.ViewBox;
            var width = Width.IsFixed() ? Width : view?.Width ?? 0;
            var height = Height.IsFixed() ? Height : view?.Height ?? 0;
            return ApplyConstraints(new Size(width, height)) + Margin;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            if (Geometry == null || rect.Width < 1 || rect.Height < 1)
                return;

            var width = Media.BrushTextureCache.PixelSize(rect.Width);
            var height = Media.BrushTextureCache.PixelSize(rect.Height);
            if (Tinted)
            {
                // A shape that had its own texture (stroked before) no longer needs it.
                if (_texture != null)
                {
                    _texture.Dispose();
                    _texture = null;
                }
                var mask = SharedMask(spriteBatch.GraphicsDevice, Geometry, width, height);
                spriteBatch.Draw(mask, new Rectangle((int)rect.Left, (int)rect.Top, width, height), Brush.ApplyOpacity(Fill!.Value, RenderOpacity));
                return;
            }

            if (_texture == null || _texture.IsDisposed || _dirty || width != _textureWidth || height != _textureHeight
                || _textureVersion != Geometry.Version || !ReferenceEquals(_texture.GraphicsDevice, spriteBatch.GraphicsDevice)
                || _resetGeneration != PathRasterizer.GetResetGeneration(spriteBatch.GraphicsDevice))
            {
                _resetGeneration = PathRasterizer.GetResetGeneration(spriteBatch.GraphicsDevice);
                _texture?.Dispose();
                _texture = PathRasterizer.CreateTexture(spriteBatch.GraphicsDevice, Geometry, width, height, Fill, Stroke, StrokeWidth);
                _textureWidth = width;
                _textureHeight = height;
                _textureVersion = Geometry.Version;
                _dirty = false;
            }

            spriteBatch.Draw(_texture, new Rectangle((int)rect.Left, (int)rect.Top, width, height), Brush.ApplyOpacity(Color.White, RenderOpacity));
        }

        // Fill only: rasterized as a white mask, the fill applied as the sprite's tint.
        private bool Tinted => _stroke == null && _fill.HasValue;
    }
}
