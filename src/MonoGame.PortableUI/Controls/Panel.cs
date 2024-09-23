using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    public abstract class Panel : Control
    {
        private ControlCollection _children;

        public ControlCollection Children
        {
            get { return _children; }
        }


        public void AddChild(Control control)
        {
            Children.Add(control);
        }

        protected Panel()
        {
            _children = new ControlCollection(this);
            _children.CollectionChanged += _children_CollectionChanged;
        }     

        private void _children_CollectionChanged(object sender, CollectionChangedEventArgs args)
        {                                        
            InvalidateLayout(true);
        }

        public override IEnumerable<Control> GetDescendants()
        {
            return Children;
        }

    }
}