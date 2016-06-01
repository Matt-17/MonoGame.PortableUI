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

        public Image()
        {
            VerticalAlignment = VerticalAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Center;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            if (TintColor != Color.Transparent)
                spriteBatch.Draw(Source, destinationRectangle: rect, color: TintColor);
            else
                spriteBatch.Draw(Source, destinationRectangle: rect);
        }

        public override void UpdateLayout(Rect rect)
        {
            if (IsGone)
                BoundingRect = Rect.Empty;

            var measuredSize = GetImageSize(rect);
            var offset = rect.Offset;
            
            BoundingRect = GetRectForAlignment(rect, measuredSize, offset);
        }

        private Size GetImageSize(Rect rect)
        {
            var widthGap = rect.Width / Source.Width;
            var heightGap = rect.Height / Source.Height;
            
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
                        newWidth = rect.Width;
                        var scalingFactor = newWidth/Source.Width;
                        newHeight = Source.Height*scalingFactor;
                    }
                    else
                    {
                        newHeight = rect.Height;
                        var scalingFactor = newHeight/Source.Height;
                        newWidth = Source.Width*scalingFactor;
                    }
                    break;
                case Stretch.UniformToFill:
                    if (widthGap > heightGap)
                    {
                        newWidth = rect.Width;
                        var scalingFactor = newWidth/Source.Width;
                        newHeight = Source.Height*scalingFactor;
                    }
                    else
                    {
                        newHeight = rect.Height;
                        var scalingFactor = newHeight/Source.Height;
                        newWidth = Source.Width*scalingFactor;
                    }
                    break;
                case Stretch.Fill:
                    newWidth = rect.Width;
                    newHeight = rect.Height;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            return new Size(Width.IsFixed() ? Width : newWidth, Height.IsFixed() ? Height : newHeight);
        }
    }

    public enum Stretch
    {
        None,
        Uniform,
        UniformToFill,
        Fill
    }
}