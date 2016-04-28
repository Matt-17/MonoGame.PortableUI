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
        }
    }
}