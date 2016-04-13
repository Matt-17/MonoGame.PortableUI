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
        public ContentAlignment ContentAlignment { get; set; }

        protected internal override void OnUpdate(TimeSpan elapsed, Rectangle rect)
        {
            rect = CreateRect(rect);
            
            base.OnUpdate(elapsed, rect);

            float x = rect.X;
            float y = rect.Y;

            foreach (var child in Children)
            {
                Rectangle childRect = CreateChildRect(x, y, child);

                child.OnUpdate(elapsed, childRect);

                x += child.MeasuredWidth;
                y += child.MeasuredHeight;
            }
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rectangle rect)
        {
            rect = CreateRect(rect-Padding);

            base.OnDraw(spriteBatch, rect);
            spriteBatch.Draw(ScreenEngine.Pixel, rect, BackgroundColor);

            float x = rect.X;
            float y = rect.Y;

            foreach (var child in Children)
            {
                Rectangle childRect = CreateChildRect(x, y, child);

                child.OnDraw(spriteBatch, childRect);

                x += child.MeasuredWidth;
                y += child.MeasuredHeight;
            }
        }

        private Rectangle CreateRect(Rectangle rect)
        {
            if (Height == -1)
                switch (Orientation)
                {
                    case Orientation.Horizontal:
                        rect.Height = (int)Children.Select(child => child.MeasuredHeight).Max();
                        break;
                    case Orientation.Vertical:
                        rect.Height = (int)Children.Select(child => child.MeasuredHeight).Sum();
                        break;
                }

            if (Width == -1)
                switch (Orientation)
                {
                    case Orientation.Horizontal:
                        rect.Width = (int)Children.Select(child => child.MeasuredWidth).Sum();
                        break;
                    case Orientation.Vertical:
                        rect.Width = (int)Children.Select(child => child.MeasuredWidth).Max();
                        break;
                }

            return rect;
        }

        private Rectangle CreateChildRect(float x, float y, Control child)
        {
            Rectangle childRect;

            int childX;
            int childY;

            var contentAlignmentValues = Enum.GetValues(typeof(ContentAlignment));


            switch (Orientation)
            {
                case Orientation.Horizontal:

                    //initial position is top left
                    childY = (int)(Top + child.Margin.Top);
                    childX = (int)(child.Margin.Left + x);

                    foreach (ContentAlignment alignment in contentAlignmentValues)
                    {
                        if ((ContentAlignment & alignment) == alignment)
                        {
                            switch (alignment)
                            {
                                case ContentAlignment.Bottom:
                                    childY = (int) (Top + Height - child.MeasuredHeight);
                                    break;
                                case ContentAlignment.Top:
                                    childY = (int) (Top + child.Margin.Top);
                                    break;
                                case ContentAlignment.CenterHorizontal:
                                    childX = (int) (Left + Width/2) -
                                             (int) Children.Select(c => c.MeasuredWidth).Sum()/2 + (int) (x - Left) +
                                             (int) child.Margin.Left;
                                    break;
                                case ContentAlignment.CenterVertical:
                                    childY = (int) (Top + Height/2 - child.MeasuredHeight/2);
                                    break;
                                case ContentAlignment.Left:
                                    childX = (int) (child.Margin.Left + x);
                                    break;
                                case ContentAlignment.Right:
                                    childX = (int) ((Left + Width) - (int) Children.Select(c => c.MeasuredWidth).Sum() +
                                             (int) (x - Left) + (int) child.Margin.Left);
                                    break;
                            }
                        }
                    }
                    childRect = new Rectangle(childX, childY, (int) child.Width, (int) child.Height);
                    break;
                case Orientation.Vertical:

                    // initital position is top left
                    childX = (int)(Left + child.Margin.Left);
                    childY = (int)(child.Top + child.Margin.Top + y);

                    foreach (ContentAlignment alignment in contentAlignmentValues)
                    {
                        if ((ContentAlignment & alignment) == alignment)
                        {
                            switch (alignment)
                            {
                                case ContentAlignment.Right:
                                    childX = (int) (Left + (Width - child.MeasuredWidth));
                                    break;
                                case ContentAlignment.Left:
                                    childX = (int) (Left + child.Margin.Left);
                                    break;
                                case ContentAlignment.Top:
                                    childY = (int)(child.Top + child.Margin.Top + y);
                                    break;
                                case ContentAlignment.Bottom:
                                    childY = (int)(Top + Height) - (int)Children.Select(c => c.MeasuredHeight).Sum() + (int)(y - Top) + (int)child.Margin.Top;
                                    break;
                                case ContentAlignment.CenterHorizontal:
                                    childX = (int)(Left + (Width / 2 - child.MeasuredWidth / 2));
                                    break;
                                case ContentAlignment.CenterVertical:
                                    childY = (int)(Top + Height / 2) - (int)Children.Select(c => c.MeasuredHeight).Sum() / 2 + (int)(y - Top) + (int)child.Margin.Top;
                                    break;
                            }
                        }
                    }
                    childRect = new Rectangle(childX, childY, (int) child.Width, (int) child.Height);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return childRect;
        }

        public enum Layout
        {
            WrapContent = -1
        }
    }
}