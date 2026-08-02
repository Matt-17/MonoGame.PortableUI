using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    public delegate Control? ControlTemplate(ContentControl owner);

    public abstract class ContentControl : Control
    {
        private Control? _content;
        private Control? _templateRoot;
        private ControlTemplate? _template;

        public event ContentChangedEventHandler? ContentChanged;

        protected virtual void OnContentChanged(Control? newControl)
        {
            ContentChanged?.Invoke(this, new ContentChangedEventArgs(newControl));
        }

        public Control? Content
        {
            get { return _content; }
            set
            {
                if (_content != null)
                    _content.Parent = null;

                if (value != null && Template == null)
                    value.Parent = this;
                _content = value;
                RebuildTemplateRoot();
                OnContentChanged(value);
            }
        }

        public ControlTemplate? Template
        {
            get { return _template; }
            set
            {
                if (_template == value)
                    return;

                _template = value;
                RebuildTemplateRoot();
                InvalidateLayout(true);
            }
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            VisualChild?.UpdateLayout(BoundingRect - Margin - Padding);
        }

        public override Size MeasureLayout()
        {
            var size = base.MeasureLayout();

            if (Height.IsFixed() && Width.IsFixed())
                return size;

            size -= Margin;
            size += Padding;
            size += VisualChild?.MeasureLayout() ?? Size.Empty;

            if (Height.IsFixed())
                size.Height = Height;
            if (Width.IsFixed())
                size.Width = Width;

            return ApplyConstraints(size) + Margin;
        }

        public Thickness Padding { get; set; }
        public override IEnumerable<Control> GetDescendants()
        {
            if (VisualChild != null)
                yield return VisualChild;
        }

        protected Control? VisualChild => _templateRoot ?? _content;

        private void RebuildTemplateRoot()
        {
            if (_templateRoot != null)
            {
                _templateRoot.Parent = null;
                _templateRoot = null;
            }

            if (_template == null)
            {
                if (_content != null)
                    _content.Parent = this;
                return;
            }

            if (_content != null)
                _content.Parent = null;

            _templateRoot = _template(this);
            if (_templateRoot != null)
                _templateRoot.Parent = this;
        }
    }
}
