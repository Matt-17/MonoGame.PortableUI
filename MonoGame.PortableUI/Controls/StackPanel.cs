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
            base.OnUpdate(elapsed, rect);
            foreach (var child in Children)
                child.Update(elapsed);
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rectangle rect)
        {
            spriteBatch.Draw(ScreenEngine.Pixel, rect, BackgroundColor);
            foreach (var child in Children)
                child.Draw(spriteBatch);
        }

        public override void UpdateLayout()
        {
            base.UpdateLayout();

            CreateRect();

            var childX = Position.X;
            var childY = Position.Y;

            foreach (var child in Children)
            {
                child.Position = new Point(childX, childY);
                switch (Orientation)
                {
                    case Orientation.Horizontal:
                        childX += (int) child.MeasuredWidth;
                        break;
                    case Orientation.Vertical:
                        childY += (int) child.MeasuredHeight;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                child.UpdateLayout();
            }
        }
        
        private void CreateRect()
        {
            float height = Height;
            float width = Width;

            if (Height == -1)
                switch (Orientation)
                {
                    case Orientation.Horizontal:
                        height = Children.Select(child => child.MeasuredHeight).Max();
                        break;
                    case Orientation.Vertical:
                        height = Children.Select(child => child.MeasuredHeight).Sum();
                        break;
                }

            if (Width == -1)
                switch (Orientation)
                {
                    case Orientation.Horizontal:
                        width = Children.Select(child => child.MeasuredWidth).Sum();
                        break;
                    case Orientation.Vertical:
                        width = Children.Select(child => child.MeasuredWidth).Max();
                        break;
                }
            BoundingRect = new Size(width, height);
        }
    }
}