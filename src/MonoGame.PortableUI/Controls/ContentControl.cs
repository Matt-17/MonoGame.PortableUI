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
            var slot = BoundingRect - Margin - Padding;
            if (CentersFixedSizeContent && VisualChild is { } child)
                slot = CenterFixedSize(child, slot);
            VisualChild?.UpdateLayout(slot);
        }

        /// <summary>
        ///     Content with a fixed width or height that is left on Stretch sits in the middle (an icon in a button)
        ///     instead of the top left corner.
        /// </summary>
        protected virtual bool CentersFixedSizeContent => false;

        private static Rect CenterFixedSize(Control child, Rect slot)
        {
            var size = child.ApplyConstraints(new Size(child.Width, child.Height));
            var width = size.Width + child.Margin.Horizontal;
            var height = size.Height + child.Margin.Vertical;
            if (child.HorizontalAlignment == HorizontalAlignment.Stretch && child.Width.IsFixed() && width < slot.Width)
                slot = new Rect(slot.Left + (slot.Width - width) / 2, slot.Top, width, slot.Height);
            if (child.VerticalAlignment == VerticalAlignment.Stretch && child.Height.IsFixed() && height < slot.Height)
                slot = new Rect(slot.Left, slot.Top + (slot.Height - height) / 2, slot.Width, height);
            return slot;
        }

        public override Size MeasureLayout()
        {
            if (IsGoneShown || (Height.IsFixed() && Width.IsFixed()))
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

        protected internal override int VisualChildCount => VisualChild != null ? 1 : 0;

        protected internal override Control GetVisualChild(int index) => VisualChild!;

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
