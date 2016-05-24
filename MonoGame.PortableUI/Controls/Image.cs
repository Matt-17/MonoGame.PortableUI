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
            if (TintColor != Color.Transparent)
                spriteBatch.Draw(Source, destinationRectangle: rect, color: TintColor);
            else
                spriteBatch.Draw(Source, destinationRectangle: rect);
        }

        public override Size MeasureLayout()
        {
            if (float.IsNaN(Width))
                Width = Source.Width;

            if (float.IsNaN(Height))
                Height = Source.Height;

            Size destSize;
            var widthGap = Width - Source.Width;
            var heightGap = Height - Source.Height;
            float newWidth;
            float newHeight;
            switch (Stretch)
            {
                case Stretch.None:
                    destSize = new Size(Source.Width, Source.Height);
                    break;
                case Stretch.Uniform:

                    if (widthGap < heightGap)
                    {
                        newWidth = Width;
                        var scalingFactor = newWidth / Source.Width;
                        newHeight = Source.Height * scalingFactor;
                    }
                    else
                    {
                        newHeight = Height;
                        var scalingFactor = newHeight / Source.Height;
                        newWidth = Source.Width * scalingFactor;
                    }
                    destSize = new Size(newWidth, newHeight);
                    break;
                case Stretch.UniformToFill:
                    if (widthGap > heightGap)
                    {
                        newWidth = Width;
                        var scalingFactor = newWidth / Source.Width;
                        newHeight = Source.Height * scalingFactor;
                    }
                    else
                    {
                        newHeight = Height;
                        var scalingFactor = newHeight / Source.Height;
                        newWidth = Source.Width * scalingFactor;
                    }
                    //so far no clipping visibile cause scissor mask is not implemented yet
                    destSize = new Size(newWidth, newHeight);
                    break;
                case Stretch.Fill:
                    destSize = new Size(Width, Height);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return destSize;
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
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