using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public abstract class Panel : Control
    {
        private ControlCollection _children;

        public ControlCollection Children
        {
            get { return _children; }
        }


        protected Panel()
        {
            _children = new ControlCollection(this);
            _children.CollectionChanged += _children_CollectionChanged;
        }

        internal Rect GetRectangleForChild(Control child)
        {
            return child.BoundingRect;
        }

        private void _children_CollectionChanged(object sender, CollectionChangedEventArgs args)
        {                                        
            InvalidateLayout(true);
        }

        public override IEnumerable<Control> GetDescendants()
        {
            return Children;
        }

        protected internal override void OnAfterDraw(SpriteBatch spriteBatch, Rect renderedBoundingRect)
        {
            base.OnAfterDraw(spriteBatch, renderedBoundingRect);
            foreach (var child in Children.Reverse())
            {
                child.Draw(spriteBatch, GetRectangleForChild(child));
            }
        }
    }
}