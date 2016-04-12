using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Controls
{
    public abstract class Panel : UIControl
    {
        private List<UIControl> _children;

        protected List<UIControl> Children
        {
            get { return _children; }
            set { _children = value; }
        }


        public Panel(Game game) : base(game)
        {
            _children = new List<UIControl>();
        }

        public void AddChild(UIControl child)
        {
            child.Parent = this;
            _children.Add(child);
        }

        public void RemoveChild(UIControl child)
        {
            child.Parent = null;
            _children.Remove(child);
        }

    }
}