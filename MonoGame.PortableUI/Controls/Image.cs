using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class Image : Control
    {
        public Texture2D ImageSource { get; set; }

        public Stretch Stretch { get; set; }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            Rect destRect;
            var widthGap = rect.Width - ImageSource.Width;
            var heightGap = rect.Height - ImageSource.Height;
            float newWidth;
            float newHeight;
            switch (Stretch)
            {
                case Stretch.None:
                    destRect = new Rect(rect.Left, rect.Top, ImageSource.Width, ImageSource.Height);
                    break;
                case Stretch.Uniform:
                    
                    if (widthGap < heightGap)
                    {
                        newWidth = rect.Width;
                        var scalingFactor = newWidth/ImageSource.Width;
                        newHeight = ImageSource.Height*scalingFactor;
                    }
                    else
                    {
                        newHeight = rect.Height;
                        var scalingFactor = newHeight / ImageSource.Height;
                        newWidth = ImageSource.Width * scalingFactor;
                    }
                    destRect = new Rect(rect.Left, rect.Top, newWidth, newHeight);
                    break;
                case Stretch.UniformToFill:
                    if (widthGap > heightGap)
                    {
                        newWidth = rect.Width;
                        var scalingFactor = newWidth / ImageSource.Width;
                        newHeight = ImageSource.Height * scalingFactor;
                    }
                    else
                    {
                        newHeight = rect.Height;
                        var scalingFactor = newHeight / ImageSource.Height;
                        newWidth = ImageSource.Width * scalingFactor;
                    }
                    //so far no clipping visibile cause scissor mask is not implemented yet
                    destRect = new Rect(rect.Left, rect.Top, newWidth, newHeight);
                    break;
                case Stretch.Fill:
                    destRect = rect;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            spriteBatch.Draw(ImageSource, destinationRectangle: destRect);
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