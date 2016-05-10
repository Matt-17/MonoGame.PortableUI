
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
            spriteBatch.Draw(ScreenEngine.Pixel, rect, BackgroundColor);
        }

        public override Size MeasureLayout(Size availableSize)
        {
            //return base.MeasureLayout(availableSize);
            var result = new Size();
            if (Orientation == Orientation.Vertical)
                availableSize.Height = 0;
            else
                availableSize.Width = 0;

            foreach (var child in Children)
            {
                result += child.MeasureLayout(availableSize - Padding);
            }

            if (Orientation == Orientation.Vertical)
                result.Width = availableSize.Width;
            else
                result.Height = availableSize.Height;

            return result;
        }

        public override void UpdateLayout(Rect boundingRect)
        {
            // Bounding rect to default - is necessary
            base.UpdateLayout(boundingRect);
            boundingRect -= Padding;

            var childOffset = new PointF(boundingRect.Left, boundingRect.Top);

            if (Orientation == Orientation.Vertical)
                boundingRect.Height = 0;
            else
                boundingRect.Width = 0;
            foreach (var child in Children)
            {
                var measureLayout = child.MeasureLayout((Size)boundingRect);
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