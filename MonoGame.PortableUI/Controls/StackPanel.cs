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

        protected internal override void OnUpdate(TimeSpan elapsed, Rectangle rect)
        {
            rect = CreateRect(rect);

            base.OnUpdate(elapsed, rect);

            float childX = rect.X;
            float childY = rect.Y;

            foreach (var child in Children)
                child.OnUpdate(elapsed, ChildRect(child, ref childX, ref childY));
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rectangle rect)
        {
            rect = CreateRect(rect - Padding);

            base.OnDraw(spriteBatch, rect);
            spriteBatch.Draw(ScreenEngine.Pixel, rect, BackgroundColor);

            float childX = rect.X;
            float childY = rect.Y;

            foreach (var child in Children)
                child.OnDraw(spriteBatch, ChildRect(child, ref childX, ref childY));
        }

        private Rectangle ChildRect(Control child, ref float childX, ref float childY)
        {
            Rectangle childRect;


            switch (Orientation)
            {
                case Orientation.Horizontal:
                    childRect = new Rectangle((int)childX, (int)childY, (int)child.BoundingRect.Width, (int)child.BoundingRect.Height);
                    childX += child.BoundingRect.Width;
                    break;
                case Orientation.Vertical:
                    childRect = new Rectangle((int)childX, (int)childY, (int)child.BoundingRect.Width, (int)child.BoundingRect.Height);
                    childY += child.BoundingRect.Height;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return childRect;
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
    }
}