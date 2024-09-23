using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class Image : Control
    {
        public Texture2D Source { get; set; }

        public Color TintColor { get; set; }

        public Stretch Stretch { get; set; }
        
        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);

            if (Source == null)
                return;

            var x = rect.Left;
            var y = rect.Top;

            var imageSize = GetImageSize((Size)rect);
            if (HorizontalAlignment == HorizontalAlignment.Stretch)
                x += (rect.Width - imageSize.Width) / 2;
            
            if (VerticalAlignment == VerticalAlignment.Center)
                y += (rect.Height - imageSize.Height) / 2;

            var destinationRectangle = new Rect(new PointF(x,y), imageSize);

            if (TintColor != Color.Transparent)
                spriteBatch.Draw(Source, destinationRectangle, TintColor);
            else
                spriteBatch.Draw(Source, destinationRectangle: destinationRectangle);
        }

        public override Size MeasureLayout()
        {
            var size = base.MeasureLayout();

            if (size.Height != 0 && size.Width != 0)
                return size;

            if (size.Height == 0)
                size.Height = Source.Height;

            if (size.Width == 0)
                size.Width = Source.Width;

            size = GetImageSize(size);

            if (Height.IsFixed())
                size.Height = Height;

            if (Width.IsFixed())
                size.Width = Width;

            return size;
        }

        private Size GetImageSize(Size size)
        {
            if (Source == null)
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