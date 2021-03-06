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
                if (!Width.IsFixed())
                    size.Width += Children.Count > 0 ? Children.Max(child => child.MeasureLayout().Width) : 0;
                if (!Height.IsFixed())
                    size.Height += Children.Sum(child => child.MeasureLayout().Height);
            }
            else
            {
                if (!Width.IsFixed())
                    size.Width += Children.Sum(child => child.MeasureLayout().Width);
                if (!Height.IsFixed())
                    size.Height += Children.Count > 0 ? Children.Max(child => child.MeasureLayout().Height) : 0;
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