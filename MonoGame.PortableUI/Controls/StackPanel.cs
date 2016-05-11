
using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class StackPanel : Panel
    {
        public Orientation Orientation { get; set; }

        public Thickness Padding { get; set; }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            spriteBatch.Draw(ScreenEngine.Pixel, rect - Margin, BackgroundColor);
        }

        public override Size MeasureLayout(Size availableSize)
        {   
            availableSize -= Margin;
            availableSize -= Padding;
            var width = Width;
            var height = Height;
            if (!width.IsAuto() && !height.IsAuto())
                return new Size(width, height);

            var result = new Size();

            if (width.IsAuto())
                result.Width = availableSize.Width;

            if (height.IsAuto())
                result.Height = availableSize.Height;

            if (HorizontalAlignment != HorizontalAlignment.Stretch)
                result.Width = Orientation == Orientation.Vertical ? Children.Max(child => child.MeasuredWidth) : Children.Sum(child => child.MeasuredWidth);
            
            if (VerticalAlignment != VerticalAlignment.Stretch)
                result.Height = Orientation == Orientation.Horizontal ? Children.Max(child => child.MeasuredHeight) : Children.Sum(child => child.MeasuredHeight);
      
            return result + Margin + Padding;
        }

        public override void UpdateLayout(Rect availableBoundingRect)
        {
            // Bounding rect to default - is necessary
            base.UpdateLayout(availableBoundingRect);

            var contentRect = BoundingRect - Margin;

            contentRect -= Padding;

            var childOffset = new PointF(contentRect.Left, contentRect.Top);

            if (Orientation == Orientation.Vertical)
                contentRect.Height = 0;
            else
                contentRect.Width = 0;
            foreach (var child in Children)
            {
                var measureLayout = child.MeasureLayout((Size) contentRect);
                Rect rect = childOffset + measureLayout;
                child.ClientRect = rect;
                child.UpdateLayout(rect.AtOrigin());
                if (Orientation == Orientation.Vertical)
                {
                    childOffset.Y += rect.Height;
                }
                else
                {
                    childOffset.X += rect.Width;
                }
            }

            // BoundingRect evtl neu setzen, wenn Auto gesetzt ist
        }
    }
}