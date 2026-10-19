using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    public abstract class Panel : Control
    {
        private ControlCollection _children;

        public ControlCollection Children
        {
            get { return _children; }
        }

        /// <summary>Inner spacing between the panel bounds and its children.</summary>
        public Thickness Padding { get; set; }

        /// <summary>
        ///     Layout transition (FLIP) for every child without its own <see cref="Animation.TransitionProperty.Layout"/>
        ///     entry: a child that a layout pass (or, in a <see cref="Canvas"/>, a position change) moves is drawn where it
        ///     was and glides to its new place - siblings make room for an inserted child, close the gap of a removed
        ///     one and slide on reorder. Combine with <see cref="Control.VisibilityTransition"/> for the child that enters
        ///     or leaves. Null (default) = children jump.
        /// </summary>
        public Animation.Transition? LayoutTransition
        {
            get => _layoutTransition;
            set
            {
                _layoutTransition = value;
                if (value != null)
                    LayoutTransitionsInUse = true;
            }
        }

        private Animation.Transition? _layoutTransition;


        public void AddChild(Control control)
        {
            Children.Add(control);
        }

        protected Panel()
        {
            IsFocusable = false;
            _children = new ControlCollection(this);
            _children.CollectionChanged += _children_CollectionChanged;
        }

        private void _children_CollectionChanged(object? sender, CollectionChangedEventArgs args)
        {                                        
            InvalidateLayout(true);
        }

        public override IEnumerable<Control> GetDescendants()
        {
            return Children;
        }

        protected internal override int VisualChildCount => Children.Count;

        protected internal override Control GetVisualChild(int index) => Children[index];

    }
}
