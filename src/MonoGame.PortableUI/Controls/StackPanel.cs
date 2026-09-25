using System;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class StackPanel : Panel
    {
        private float _spacing;

        public Orientation Orientation { get; set; }

        /// <summary>Gap inserted between neighbouring (non-collapsed) children.</summary>
        public float Spacing
        {
            get => _spacing;
            set
            {
                if (_spacing.Equals(value))
                    return;
                _spacing = Math.Max(0, value);
                InvalidateLayout(true);
            }
        }

        public override Size MeasureLayout()
        {
            if (IsGone || (Width.IsFixed() && Height.IsFixed()))
                return base.MeasureLayout();

            // Single pass: measuring per child is the expensive part, so accumulate the
            // main-axis sum and cross-axis max from one MeasureLayout call each.
            float mainSum = 0;
            float crossMax = 0;
            var visibleChildren = 0;
            foreach (var child in Children)
            {
                if (!child.IsGone)
                    visibleChildren++;
                var childSize = child.Measure();
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

            if (visibleChildren > 1)
                mainSum += Spacing * (visibleChildren - 1);

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

            var placed = false;
            foreach (var child in Children)
            {
                if (placed && !child.IsGone && Spacing > 0)
                {
                    if (Orientation == Orientation.Vertical)
                        contentRect.Top += Spacing;
                    else
                        contentRect.Left += Spacing;
                }

                child.UpdateLayout(contentRect);
                if (!child.IsGone)
                    placed = true;

                if (Orientation == Orientation.Vertical)
                    contentRect.Top += child.BoundingRect.Height;
                else
                    contentRect.Left += child.BoundingRect.Width;
            }
        }
    }
}
