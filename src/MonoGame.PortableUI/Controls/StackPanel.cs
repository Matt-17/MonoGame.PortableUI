using System;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class StackPanel : Panel
    {
        public Orientation Orientation { get; set; }

        public override Size MeasureLayout()
        {
            if (IsGone || (Width.IsFixed() && Height.IsFixed()))
                return base.MeasureLayout();

            // Single pass: measuring per child is the expensive part, so accumulate the
            // main-axis sum and cross-axis max from one MeasureLayout call each.
            float mainSum = 0;
            float crossMax = 0;
            foreach (var child in Children)
            {
                var childSize = child.MeasureLayout();
                if (Orientation == Orientation.Vertical)
                {
                    mainSum += childSize.Height;
                    crossMax = Math.Max(crossMax, childSize.Width);
                }
                else
                {
                    mainSum += childSize.Width;
                    crossMax = Math.Max(crossMax, childSize.Height);
                }
            }

            var contentWidth = Orientation == Orientation.Vertical ? crossMax : mainSum;
            var contentHeight = Orientation == Orientation.Vertical ? mainSum : crossMax;
            var size = new Size(
                Width.IsFixed() ? Width : contentWidth + Padding.Horizontal,
                Height.IsFixed() ? Height : contentHeight + Padding.Vertical);

            // Min/Max bound the content box; margin is added afterwards (same as Control).
            return ApplyConstraints(size) + Margin;
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            var contentRect = BoundingRect - Margin - Padding;

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
