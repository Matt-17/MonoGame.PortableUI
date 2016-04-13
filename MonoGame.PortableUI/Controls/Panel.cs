using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Controls
{
    public abstract class Panel : Control
    {
        private List<Control> _children;

        protected List<Control> Children
        {
            get { return _children; }
            set { _children = value; }
        }


        public Panel() 
        {
            _children = new List<Control>();
        }

        public void AddChild(Control child)
        {
            child.Parent = this;
            _children.Add(child);
        }

        public void RemoveChild(Control child)
        {
            child.Parent = null;
            _children.Remove(child);
        }

    }
}