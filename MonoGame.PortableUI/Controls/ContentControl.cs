using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public abstract class ContentControl : Control
    {
        private Control _child;

        public Control Child
        {
            get { return _child; }
            set
            {
                value.Parent = this;
                _child = value;
            }
        }

        public Thickness Padding { get; set; }

        protected ContentControl()
        {
        }

        protected internal override void OnUpdate(TimeSpan elapsed, Rectangle rect)
        {
            base.OnUpdate(elapsed, rect);
        }
    }
}