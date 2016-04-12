using System;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Controls
{
    public abstract class ContentControl : UIControl
    {
        private UIControl _child;

        public UIControl Child
        {
            get { return _child; }
            set
            {
                value.Parent = this;
                _child = value;
            }
        }

        public Thickness Padding { get; set; }

        protected ContentControl(Game game) : base(game)
        {
            Padding = new Thickness(8);
        }

        protected internal override void OnUpdate(TimeSpan elapsed, Rectangle rect)
        {
            base.OnUpdate(elapsed, rect);
        }
    }
}