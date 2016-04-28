using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Controls
{
    public abstract class Panel : Control
    {
        private ControlCollection _children;

        public ControlCollection Children
        {
            get { return _children; }
        }

        public Panel()
        {
            _children = new ControlCollection(this);
            _children.CollectionChanged += _children_CollectionChanged;
        }

        private void _children_CollectionChanged(object sender, CollectionChangedEventArgs args)
        {
            UpdateLayout();
        }
    }
}