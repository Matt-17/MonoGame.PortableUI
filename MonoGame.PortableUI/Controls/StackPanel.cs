using System.Linq;
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
            if (width.IsFixed() && height.IsFixed())
                return new Size(width, height);

            var result = new Size();

            if (!width.IsFixed())
                result.Width = availableSize.Width;

            if (!height.IsFixed())
                result.Height = availableSize.Height;

            if (HorizontalAlignment != HorizontalAlignment.Stretch)
                result.Width = Orientation == Orientation.Vertical ? Children.Max(child => child.MeasureLayout(availableSize).Width) : Children.Sum(child => child.MeasureLayout(availableSize).Width);
            
            if (VerticalAlignment != VerticalAlignment.Stretch)
                result.Height = Orientation == Orientation.Horizontal ? Children.Max(child => child.MeasureLayout(availableSize).Height) : Children.Sum(child => child.MeasureLayout(availableSize).Height);
      
            return result + Margin + Padding;
        }

        public override void UpdateLayout(Rect availableBoundingRect)
        {
            // Bounding rect to default - is necessary
            base.UpdateLayout(availableBoundingRect);  
            var contentRect = BoundingRect - Margin - Padding;

            if (Orientation == Orientation.Vertical)
                contentRect.Height = Size.Auto;
            else
                contentRect.Width = Size.Auto;
            var childOffset = new PointF(contentRect.Left, contentRect.Top);
            
            foreach (var child in Children)
            {
                child.UpdateLayout(childOffset + (Size)contentRect);

                if (Orientation == Orientation.Vertical)
                {
                    childOffset.Y += child.BoundingRect.Height;
                    contentRect.Height -= child.BoundingRect.Height;
                }
                else
                {
                    childOffset.X += child.BoundingRect.Width;
                    contentRect.Width -= child.BoundingRect.Width;
                }
            }

            // BoundingRect evtl neu setzen, wenn Auto gesetzt ist
        }
    }
}