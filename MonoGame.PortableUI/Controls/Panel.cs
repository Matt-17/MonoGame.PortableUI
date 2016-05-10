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

        protected Dictionary<Control, Rect> ChildrenRects;

        protected Panel()
        {
            ChildrenRects = new Dictionary<Control, Rect>();
            _children = new ControlCollection(this);
            _children.CollectionChanged += _children_CollectionChanged;
        }

        internal Rect GetRectangleForChild(Control child)
        {
            return ChildrenRects[child];
        }

        private void _children_CollectionChanged(object sender, CollectionChangedEventArgs args)
        {
            foreach (var newElement in args.NewElements)
                ChildrenRects.Add(newElement, Rect.Empty);

            foreach (var removedElement in args.RemovedElements)
                ChildrenRects.Remove(removedElement);

            InvalidateLayout(true);
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