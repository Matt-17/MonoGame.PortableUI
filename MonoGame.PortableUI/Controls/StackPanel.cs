using System;
using System.Linq;
using Microsoft.Xna.Framework;
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

        public override void InvalidateLayout(bool boundsChanged)
        {
            base.InvalidateLayout(boundsChanged);
        }

        public override Size MeasureLayout(Size availableSize)
        {
            //return base.MeasureLayout(availableSize);
            var result = new Size();
            foreach (var child in Children)
            {
                // TODO Orientation
                result += child.MeasureLayout(new Size(availableSize.Width, -1));
            }
            // todo orientation
            return result;
        }

        public override void UpdateLayout(Rect boundingRect)
        {
            // Bounding rect to default - is necessary
            base.UpdateLayout(boundingRect);


            //CreateRect();

            var childX = Position.X;
            var childY = 0f;//Position.Y;

            foreach (var child in Children)
            {
                Rect rect = child.MeasureLayout(new Size(boundingRect.Width, -1));
                rect.Top = childY;
                childY += rect.Height;
                ChildrenRects[child] = rect;
                child.UpdateLayout(rect.AtOrigin());
            }

            // BoundingRect evtl neu setzen, wenn WrapContent gesetzt ist




            //child.Position = new Point(childX, childY);
            //switch (Orientation)
            //{
            //    case Orientation.Horizontal:
            //        childX += (int) child.MeasuredWidth;
            //        break;
            //    case Orientation.Vertical:
            //        childY += (int) child.MeasuredHeight;
            //        break;
            //    default:
            //        throw new ArgumentOutOfRangeException();
            //}
            //child.UpdateLayout();
            //}
        }

        //private void CreateRect()
        //{
        //    float height = Height;
        //    float width = Width;

        //    if (Height == -1)
        //        switch (Orientation)
        //        {
        //            case Orientation.Horizontal:
        //                height = Children.Select(child => child.MeasuredHeight).Max();
        //                break;
        //            case Orientation.Vertical:
        //                height = Children.Select(child => child.MeasuredHeight).Sum();
        //                break;
        //        }

        //    if (Width == -1)
        //        switch (Orientation)
        //        {
        //            case Orientation.Horizontal:
        //                width = Children.Select(child => child.MeasuredWidth).Sum();
        //                break;
        //            case Orientation.Vertical:
        //                width = Children.Select(child => child.MeasuredWidth).Max();
        //                break;
        //        }
        //    BoundingRect = new Size(width, height);
        //}
    }
}