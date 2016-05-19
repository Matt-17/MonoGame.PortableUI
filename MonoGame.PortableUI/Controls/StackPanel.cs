using System.Linq;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class StackPanel : Panel
    {
        public Orientation Orientation { get; set; }

        public override Size MeasureLayout()
        {
            var size = base.MeasureLayout();
            if (Width.IsFixed() && Height.IsFixed())
                return size;

            if (Orientation == Orientation.Vertical)
            {
                size.Width += Children.Max(child => child.MeasureLayout().Width);
                size.Height += Children.Sum(child => child.MeasureLayout().Height);
            }
            else
            {
                size.Width += Children.Sum(child => child.MeasureLayout().Width);
                size.Height += Children.Max(child => child.MeasureLayout().Height);
            }

            return size;
        }

        public override void UpdateLayout(Rect rect)
        {                                                   
            base.UpdateLayout(rect);
            var contentRect = BoundingRect - Margin;

            if (Orientation == Orientation.Vertical)
                contentRect.Height = Size.Infinity;
            else
                contentRect.Width = Size.Infinity;      

            foreach (var child in Children)
            {
                child.UpdateLayout(contentRect);

                if (Orientation == Orientation.Vertical)
                    contentRect.Top += child.BoundingRect.Height;
                else
                    contentRect.Left += child.BoundingRect.Width;
            }                                                       
        }
    }
}