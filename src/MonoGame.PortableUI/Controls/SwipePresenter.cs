using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using ControlAnimation = MonoGame.PortableUI.Animation.Animation;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>Direction the visible content slides when swapped.</summary>
    public enum SwipeDirection
    {
        /// <summary>Old content exits left, new content enters from the right (a "next" step).</summary>
        Left,

        /// <summary>Old content exits right, new content enters from the left (a "previous" step).</summary>
        Right,
    }

    /// <summary>
    /// Shows a single piece of content and swaps it with a horizontal slide: the outgoing control
    /// travels off one edge while the incoming control glides in from the other, both clipped to the
    /// presenter's bounds. Useful for paging carousels (e.g. a ship/catalogue browser).
    ///
    /// The slide distance is the presenter's own measured width, so it is resolution-independent; the
    /// animation is kicked off in <see cref="UpdateLayout"/> once that width is known.
    /// </summary>
    public class SwipePresenter : Panel
    {
        private Control? _incoming;   // the current / topmost content
        private Control? _outgoing;   // content sliding out (removed on completion)
        private SwipeDirection _pendingDirection;
        private bool _pendingSwipe;
        private ControlAnimation? _incomingAnimation;
        private ControlAnimation? _outgoingAnimation;

        /// <summary>How long a swap takes.</summary>
        public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(260);

        /// <summary>Easing for the slide; defaults to a decelerating cubic.</summary>
        public Easing Easing { get; set; } = Easings.CubicOut;

        // Overflowing content (the part still off-screen) must be clipped to the presenter.
        protected internal override bool ClipsDescendants => true;

        /// <summary>The content currently shown (settled or sliding in).</summary>
        public Control? Content => _incoming;

        /// <summary>Replace the content immediately, with no animation (first show / rebuilds).</summary>
        public void SetContent(Control? content)
        {
            FinishSwipe();
            Children.Clear();
            _incoming = content;
            _outgoing = null;
            if (content != null)
            {
                content.Translation = Vector2.Zero;
                Children.Add(content);
            }
        }

        /// <summary>Swap to <paramref name="content"/> with a slide in the given direction.</summary>
        public void Swipe(Control content, SwipeDirection direction)
        {
            if (_incoming == null)
            {
                SetContent(content);
                return;
            }

            FinishSwipe();               // collapse any swap still in flight
            _outgoing = _incoming;
            _incoming = content;
            _pendingDirection = direction;
            _pendingSwipe = true;
            Children.Add(content);       // laid out (and animated) on the next layout pass
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);

            var content = BoundingRect - Margin - Padding;
            foreach (var child in Children)
                child.UpdateLayout(content);

            if (_pendingSwipe && content.Width > 0)
                StartSwipe(content.Width);
        }

        private void StartSwipe(float width)
        {
            _pendingSwipe = false;

            // Left = old exits left / new enters from the right; Right is the mirror.
            var sign = _pendingDirection == SwipeDirection.Left ? 1f : -1f;

            if (_incoming != null)
            {
                _incoming.Translation = new Vector2(sign * width, 0);
                _incomingAnimation = _incoming.Animate()
                    .TranslateTo(Vector2.Zero)
                    .Duration(Duration)
                    .Ease(Easing)
                    .Start();
            }

            if (_outgoing != null)
            {
                var outgoing = _outgoing;
                outgoing.Translation = Vector2.Zero;
                _outgoingAnimation = outgoing.Animate()
                    .TranslateTo(new Vector2(-sign * width, 0))
                    .Duration(Duration)
                    .Ease(Easing)
                    .OnCompleted(() =>
                    {
                        Children.Remove(outgoing);
                        if (ReferenceEquals(_outgoing, outgoing))
                            _outgoing = null;
                    })
                    .Start();
            }
        }

        // Snap any in-flight swap to its end state so a new swap starts clean.
        private void FinishSwipe()
        {
            _pendingSwipe = false;
            _incomingAnimation?.Cancel();
            _outgoingAnimation?.Cancel();
            _incomingAnimation = null;
            _outgoingAnimation = null;

            if (_outgoing != null)
            {
                Children.Remove(_outgoing);
                _outgoing = null;
            }
            if (_incoming != null)
                _incoming.Translation = Vector2.Zero;
        }

        public override Size MeasureLayout()
        {
            var size = base.MeasureLayout();
            if (Width.IsFixed() && Height.IsFixed())
                return size;

            var content = Size.Empty;
            foreach (var child in Children)
            {
                var childSize = child.MeasureLayout();
                content.Width = Math.Max(content.Width, childSize.Width);
                content.Height = Math.Max(content.Height, childSize.Height);
            }

            if (Width.IsFixed())
                content.Width = Width;
            if (Height.IsFixed())
                content.Height = Height;

            return ApplyConstraints(content) + Margin;
        }
    }
}
