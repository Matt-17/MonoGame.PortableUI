
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
            
            foreach (var child in Children)
            {
                child.UpdateLayout(childOffset + (Size)contentRect);

                if (Orientation == Orientation.Vertical)
                {
                    childOffset.Y += child.MeasuredHeight;
                    contentRect.Height -= child.MeasuredHeight;
                }
                else
                {
                    childOffset.X += child.MeasuredWidth;
                    contentRect.Width -= child.MeasuredWidth;
                }
            }

            // BoundingRect evtl neu setzen, wenn Auto gesetzt ist
        }
    }
}