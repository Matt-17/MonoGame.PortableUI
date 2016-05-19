using System.Linq;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class StackPanel : Panel
    {
        public Orientation Orientation { get; set; }

        public override Size MeasureLayout(Size availableSize)
        {
            var size = base.MeasureLayout(availableSize);
            if (Width.IsFixed() && Height.IsFixed())
                return size;

            if (Orientation == Orientation.Vertical)
            {
                size.Width += Children.Max(child => child.MeasureLayout(availableSize).Width);
                size.Height += Children.Sum(child => child.MeasureLayout(availableSize).Height);
            }
            else
            {
                size.Width += Children.Sum(child => child.MeasureLayout(availableSize).Width);
                size.Height += Children.Max(child => child.MeasureLayout(availableSize).Height);
            }

            return size;
        }

        public override void UpdateLayout(Rect rect)
        {
            // Bounding rect to default - is necessary
            base.UpdateLayout(rect);
            var contentRect = BoundingRect - Margin;

            if (Orientation == Orientation.Vertical)
                contentRect.Height = Size.Infinity;
            else
                contentRect.Width = Size.Infinity;

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