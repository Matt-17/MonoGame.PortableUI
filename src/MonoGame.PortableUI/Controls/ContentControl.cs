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
                InvalidateLayout(true);
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
            if (IsGone || (Height.IsFixed() && Width.IsFixed()))
                return base.MeasureLayout();

            // Min/Max bound the content box (content + padding); margin is added afterwards.
            var content = VisualChild?.Measure() ?? Size.Empty;
            var size = new Size(
                Width.IsFixed() ? Width : content.Width + Padding.Horizontal,
                Height.IsFixed() ? Height : content.Height + Padding.Vertical);

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
