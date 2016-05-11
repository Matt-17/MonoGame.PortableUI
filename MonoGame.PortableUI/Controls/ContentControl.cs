using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public abstract class ContentControl : Control
    {
        private Control _content;

        public event ContentChangedEvent ContentChanged;

        protected virtual void OnContentChanged(Control newControl)
        {
            ContentChanged?.Invoke(this, new ContentChangedEventArgs(newControl));
        }

        public Control Content
        {
            get { return _content; }
            set
            {
                if (_content != null)
                    _content.Parent = null;
                value.Parent = this;
                _content = value;
                OnContentChanged(value);
            }
        }

        public Thickness Padding { get; set; }
        public override IEnumerable<Control> GetDescendants()
        {
            if (Content != null)
                yield return Content;
        }

        protected ContentControl()
        {
        }
    }
}