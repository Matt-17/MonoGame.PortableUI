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

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            Content?.UpdateLayout(BoundingRect - Padding);
        }

        public override Size MeasureLayout()
        {
            var size = base.MeasureLayout();

            if (Height.IsFixed() && Width.IsFixed())
                return size;
            
            size += Padding;
            size += Content?.MeasureLayout() ?? Size.Empty;
            
            if (Height.IsFixed())
                size.Height = Height;
            if (Width.IsFixed())
                size.Width = Width;
            
            return size;
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