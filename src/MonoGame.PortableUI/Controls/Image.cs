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

        public Texture2D? Source { get; set; }

        public Color TintColor { get; set; }

        // Uniform matches the WPF default; None would draw oversized sources clipped to a corner.
        public Stretch Stretch { get; set; } = Stretch.Uniform;

        public SamplerState SamplerState { get; set; } = SamplerState.LinearClamp;
        
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

            var destinationRectangle = new Rect(new PointF(x,y), imageSize);
            var tintColor = TintColor == Color.Transparent ? Color.White : TintColor;

            spriteBatch.Draw(Source, destinationRectangle, Brush.ApplyOpacity(tintColor, RenderOpacity));
        }

        public override Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            // Margin must stay out of the size the source image is fitted into, and constraints
            // apply to the content box only (same order as Control.MeasureLayout).
            var size = new Size(Width.IsFixed() ? Width : 0, Height.IsFixed() ? Height : 0);

            if (Source != null && (size.Width == 0 || size.Height == 0))
            {
                if (size.Height == 0)
                    size.Height = Source.Height;
                if (size.Width == 0)
                    size.Width = Source.Width;

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

            if (Source.Width == 0 || Source.Height == 0 || size.Width == 0 || size.Height == 0)
                return Size.Empty;

            var widthGap = size.Width / Source.Width;
            var heightGap = size.Height / Source.Height;

            float newWidth;
            float newHeight;

            switch (Stretch)
            {
                case Stretch.None:
                    newWidth = Source.Width;
                    newHeight = Source.Height;
                    break;
                case Stretch.Uniform:

                    if (widthGap < heightGap)
                    {
                        newWidth = size.Width;
                        var scalingFactor = newWidth / Source.Width;
                        newHeight = Source.Height * scalingFactor;
                    }
                    else
                    {
                        newHeight = size.Height;
                        var scalingFactor = newHeight / Source.Height;
                        newWidth = Source.Width * scalingFactor;
                    }
                    break;
                case Stretch.UniformToFill:
                    if (widthGap > heightGap)
                    {
                        newWidth = size.Width;
                        var scalingFactor = newWidth / Source.Width;
                        newHeight = Source.Height * scalingFactor;
                    }
                    else
                    {
                        newHeight = size.Height;
                        var scalingFactor = newHeight / Source.Height;
                        newWidth = Source.Width * scalingFactor;
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
