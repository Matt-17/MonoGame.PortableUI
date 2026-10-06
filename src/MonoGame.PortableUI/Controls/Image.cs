using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class Image : Control
    {
        public Image()
        {
            IsFocusable = false;
        }

        private Texture2D? _source;
        private Color _tintColor;
        // Uniform matches the WPF default; None would draw oversized sources clipped to a corner.
        private Stretch _stretch = Stretch.Uniform;
        private SamplerState _samplerState = SamplerState.LinearClamp;

        /// <summary>The texture; its size feeds the measured size, so a new source re-lays out.</summary>
        public Texture2D? Source
        {
            get => _source;
            set
            {
                if (ReferenceEquals(_source, value))
                    return;
                var sizeChanged = _source == null || value == null || _source.Width != value.Width || _source.Height != value.Height;
                _source = value;
                InvalidateLayout(sizeChanged);
                RequestRedraw();
            }
        }

        /// <summary>Tint multiplied over the image, straight alpha (see <see cref="Media.ColorAlpha"/>);
        /// transparent (the default) draws the image untinted.</summary>
        public Color TintColor
        {
            get => _tintColor;
            set
            {
                if (_tintColor == value)
                    return;
                _tintColor = value;
                RequestRedraw();
            }
        }

        private Rectangle? _sourceRectangle;

        /// <summary>
        ///     The part of <see cref="Source"/> to show, in texels (a sprite-sheet cell, a crop); null shows the
        ///     whole texture. Measuring, stretching and alignment use its size. Clamped to the texture.
        /// </summary>
        public Rectangle? SourceRectangle
        {
            get => _sourceRectangle;
            set
            {
                if (_sourceRectangle == value)
                    return;
                var sizeChanged = SourceRect.Size != Clamp(value).Size;
                _sourceRectangle = value;
                InvalidateLayout(sizeChanged);
                RequestRedraw();
            }
        }

        private Rectangle Clamp(Rectangle? rectangle)
        {
            if (_source == null)
                return Rectangle.Empty;
            return rectangle is { } r ? Rectangle.Intersect(r, _source.Bounds) : _source.Bounds;
        }

        /// <summary>The texels drawn: <see cref="SourceRectangle"/> within the texture, or the whole texture.</summary>
        private Rectangle SourceRect => Clamp(_sourceRectangle);

        private int SourceWidth => SourceRect.Width;

        private int SourceHeight => SourceRect.Height;

        public Stretch Stretch
        {
            get => _stretch;
            set
            {
                if (_stretch == value)
                    return;
                _stretch = value;
                InvalidateLayout(true);
            }
        }

        public SamplerState SamplerState
        {
            get => _samplerState;
            set
            {
                if (ReferenceEquals(_samplerState, value))
                    return;
                _samplerState = value;
                RequestRedraw();
            }
        }
        
        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);

            if (Source == null)
                return;

            var x = rect.Left;
            var y = rect.Top;

            var imageSize = GetImageSize((Size)rect);
            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Stretch:
                case HorizontalAlignment.Center:
                    x += (rect.Width - imageSize.Width) / 2;
                    break;
                case HorizontalAlignment.Right:
                    x += rect.Width - imageSize.Width;
                    break;
            }

            switch (VerticalAlignment)
            {
                case VerticalAlignment.Stretch:
                case VerticalAlignment.Center:
                    y += (rect.Height - imageSize.Height) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    y += rect.Height - imageSize.Height;
                    break;
            }

            var source = SourceRect;
            if (source.Width <= 0 || source.Height <= 0 || imageSize.Width <= 0 || imageSize.Height <= 0)
                return;
            var destination = new Rect(new PointF(x, y), imageSize);
            var tintColor = TintColor == Color.Transparent ? Color.White : TintColor;

            if (!CropToBox(ref source, ref destination, rect))
                return;
            spriteBatch.Draw(Source, destination, source, Brush.ApplyOpacity(tintColor, RenderOpacity));
        }

        /// <summary>
        ///     UniformToFill (or None on a small box) draws larger than the box: crops <paramref name="source"/>
        ///     to the texels inside <paramref name="box"/> and shrinks <paramref name="destination"/> to match, so
        ///     nothing is drawn past the box and the alignment is kept. False when nothing is visible.
        /// </summary>
        internal static bool CropToBox(ref Rectangle source, ref Rect destination, Rect box)
        {
            var visible = destination ^ box;
            if (visible.Width <= 0 || visible.Height <= 0)
                return false;
            if (visible.Width >= destination.Width - 0.01f && visible.Height >= destination.Height - 0.01f)
                return true;

            var texelsX = source.Width / destination.Width;
            var texelsY = source.Height / destination.Height;
            var left = source.X + (visible.Left - destination.Left) * texelsX;
            var top = source.Y + (visible.Top - destination.Top) * texelsY;
            var cropped = new Rectangle(
                (int)MathF.Round(left), (int)MathF.Round(top),
                Math.Max(1, (int)MathF.Round(visible.Width * texelsX)), Math.Max(1, (int)MathF.Round(visible.Height * texelsY)));
            source = Rectangle.Intersect(cropped, source);
            destination = visible;
            return source.Width > 0 && source.Height > 0;
        }

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            // Margin must stay out of the size the source image is fitted into, and constraints
            // apply to the content box only (same order as Control.MeasureLayout).
            var size = new Size(Width.IsFixed() ? Width : 0, Height.IsFixed() ? Height : 0);

            // One fixed side with a uniform stretch: the other side follows the aspect ratio
            // (Width=400 on a 100x100 texture measures 400x400, as in WPF).
            if (Source != null && SourceWidth > 0 && SourceHeight > 0
                && (Stretch == Stretch.Uniform || Stretch == Stretch.UniformToFill)
                && Width.IsFixed() != Height.IsFixed())
            {
                if (Width.IsFixed())
                    size.Height = Width * SourceHeight / SourceWidth;
                else
                    size.Width = Height * SourceWidth / SourceHeight;
                return ApplyConstraints(size) + Margin;
            }

            if (Source != null && (size.Width == 0 || size.Height == 0))
            {
                if (size.Height == 0)
                    size.Height = SourceHeight;
                if (size.Width == 0)
                    size.Width = SourceWidth;

                size = GetImageSize(size);

                if (Height.IsFixed())
                    size.Height = Height;
                if (Width.IsFixed())
                    size.Width = Width;
            }

            return ApplyConstraints(size) + Margin;
        }

        private Size GetImageSize(Size size)
        {
            if (Source == null)
                return Size.Empty;

            if (SourceWidth == 0 || SourceHeight == 0 || size.Width == 0 || size.Height == 0)
                return Size.Empty;

            var widthGap = size.Width / SourceWidth;
            var heightGap = size.Height / SourceHeight;

            float newWidth;
            float newHeight;

            switch (Stretch)
            {
                case Stretch.None:
                    newWidth = SourceWidth;
                    newHeight = SourceHeight;
                    break;
                case Stretch.Uniform:

                    if (widthGap < heightGap)
                    {
                        newWidth = size.Width;
                        var scalingFactor = newWidth / SourceWidth;
                        newHeight = SourceHeight * scalingFactor;
                    }
                    else
                    {
                        newHeight = size.Height;
                        var scalingFactor = newHeight / SourceHeight;
                        newWidth = SourceWidth * scalingFactor;
                    }
                    break;
                case Stretch.UniformToFill:
                    if (widthGap > heightGap)
                    {
                        newWidth = size.Width;
                        var scalingFactor = newWidth / SourceWidth;
                        newHeight = SourceHeight * scalingFactor;
                    }
                    else
                    {
                        newHeight = size.Height;
                        var scalingFactor = newHeight / SourceHeight;
                        newWidth = SourceWidth * scalingFactor;
                    }
                    return new Size(newWidth, newHeight);
                case Stretch.Fill:
                    newWidth = size.Width;
                    newHeight = size.Height;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new Size(newWidth, newHeight);
        }
    }
}
