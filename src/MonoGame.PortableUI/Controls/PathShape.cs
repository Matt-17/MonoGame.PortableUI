using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     Draws a <see cref="PathGeometry"/> filled and/or stroked, scaled to the control's arranged
    ///     size. The shape is rasterized once per pixel size (and on geometry or colour changes) and
    ///     then drawn as a sprite, so it is crisp at any size and costs one draw per frame.
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
            set { _fill = value; _dirty = true; }
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
            if (IsGone)
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

            var width = (int)System.Math.Ceiling(rect.Width);
            var height = (int)System.Math.Ceiling(rect.Height);
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
    }
}
