using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    public sealed class SwipedEventArgs : EventArgs
    {
        public SwipedEventArgs(SwipeDirection direction) => Direction = direction;

        /// <summary><see cref="SwipeDirection.Left"/>: the finger moved left (a "next" step, or the row's right action).</summary>
        public SwipeDirection Direction { get; }
    }

    /// <summary>
    ///     Horizontal swipes over its content: switching pages or tabs with a flick, or swipe actions on a list row
    ///     (the content follows the finger and reveals <see cref="LeftAction"/> / <see cref="RightAction"/> behind it).
    ///     A swipe takes over the touch once the finger clearly moves sideways, so a button under it does not click
    ///     and a vertical scroll viewer around or inside keeps vertical drags.
    /// </summary>
    public class SwipeArea : Grid
    {
        // Android's touch slop: closer than this it may still be a tap.
        private const float Slop = 8;

        private Control? _content;
        private Control? _leftAction;
        private Control? _rightAction;
        private bool _tracking;
        private bool _claimed;
        private PointF _start;
        private float _offset;
        private float _velocity;
        private float _lastX;
        private TimeSpan _lastTime;

        public SwipeArea()
        {
            TouchMove += (_, args) => Move(args);
            TouchUp += (_, args) => Release(args);
            TouchCancel += (_, _) => Cancel();
        }

        /// <summary>What is swiped.</summary>
        public Control? Content
        {
            get => _content;
            set => Replace(ref _content, value, on: true);
        }

        /// <summary>Shown behind the content while it is dragged to the right (its left side opens up).</summary>
        public Control? LeftAction
        {
            get => _leftAction;
            set => Replace(ref _leftAction, value, on: false);
        }

        /// <summary>Shown behind the content while it is dragged to the left.</summary>
        public Control? RightAction
        {
            get => _rightAction;
            set => Replace(ref _rightAction, value, on: false);
        }

        /// <summary>The content moves with the finger and springs back (row actions); off: it stays (page flicks).</summary>
        public bool FollowsFinger { get; set; }

        /// <summary>After a committed swipe the content slides out of view instead of springing back (delete a row).</summary>
        public bool SlidesOutOnCommit { get; set; }

        /// <summary>How far a swipe must go to count (layout units; at most 40 % of the width).</summary>
        public float CommitDistance { get; set; } = 96;

        /// <summary>A flick this fast counts however short it is (layout units per second).</summary>
        public float CommitSpeed { get; set; } = 700;

        public bool CanSwipeLeft { get; set; } = true;

        public bool CanSwipeRight { get; set; } = true;

        /// <summary>The horizontal offset while a swipe is dragged (0 when it ends).</summary>
        public event EventHandler<float>? Dragging;

        /// <summary>A swipe went far or fast enough.</summary>
        public event EventHandler<SwipedEventArgs>? Swiped;

        private void Replace(ref Control? slot, Control? value, bool on)
        {
            if (slot != null)
                Children.Remove(slot);
            slot = value;
            if (value == null)
                return;
            if (on)
                AddChild(value);
            else
            {
                // Behind the content (drawn first), hidden until the content moves away from it.
                Children.Insert(0, value);
                value.IsVisible = false;
            }
        }

        internal override void OnPreviewTouchDown(TouchEventArgs args)
        {
            _tracking = true;
            _claimed = false;
            _start = args.Position;
            _offset = 0;
            _velocity = 0;
            _lastX = args.Position.X;
            _lastTime = ScreenSystem.TotalTime;
        }

        private void Move(TouchEventArgs args)
        {
            if (!_tracking)
                return;
            var dx = args.Position.X - _start.X;
            var dy = args.Position.Y - _start.Y;
            if (!_claimed)
            {
                if (Math.Abs(dy) > Slop && Math.Abs(dy) >= Math.Abs(dx))
                {
                    // Vertical: not ours (a scroll viewer takes it).
                    _tracking = false;
                    return;
                }
                if (Math.Abs(dx) <= Slop || Math.Abs(dx) < Math.Abs(dy) * 1.2f)
                    return;
                _claimed = true;
                // The swipe owns the finger from here: no click below, no scroll viewer taking over.
                VisualTreeHelper.CancelDescendantTouches(this, args);
                if (Screen is { } screen)
                    screen.TouchScrollOwner = this;
            }

            var now = ScreenSystem.TotalTime;
            var seconds = (float)(now - _lastTime).TotalSeconds;
            if (seconds > 0.001f)
            {
                // Smoothed: one late touch sample must not decide a flick.
                _velocity = _velocity * 0.4f + (args.Position.X - _lastX) / seconds * 0.6f;
                _lastX = args.Position.X;
                _lastTime = now;
            }

            // A direction that is not allowed gives way only a little (rubber band).
            _offset = dx < 0 && !CanSwipeLeft || dx > 0 && !CanSwipeRight ? dx * 0.15f : dx;
            if (FollowsFinger && _content != null)
                _content.Translation = new Vector2(_offset, 0);
            if (_leftAction != null)
                _leftAction.IsVisible = _offset > 0;
            if (_rightAction != null)
                _rightAction.IsVisible = _offset < 0;
            Dragging?.Invoke(this, _offset);
            args.Handled = true;
        }

        private void Release(TouchEventArgs args)
        {
            var claimed = _claimed;
            var offset = _offset;
            var velocity = _velocity;
            End();
            if (!claimed)
                return;
            args.Handled = true;

            var width = ClippingRect.Width;
            var distance = width > 0 ? Math.Min(CommitDistance, width * 0.4f) : CommitDistance;
            var direction = offset < 0 ? SwipeDirection.Left : SwipeDirection.Right;
            var allowed = direction == SwipeDirection.Left ? CanSwipeLeft : CanSwipeRight;
            var flick = Math.Abs(velocity) >= CommitSpeed && Math.Sign(velocity) == Math.Sign(offset);
            var commit = allowed && offset != 0 && (Math.Abs(offset) >= distance || flick);

            if (commit && SlidesOutOnCommit && FollowsFinger && _content != null)
            {
                var content = _content;
                content.Animate().TranslateTo(new Vector2(Math.Sign(offset) * (width + 24), 0))
                    .Duration(TimeSpan.FromMilliseconds(180)).Ease(Easings.CubicOut)
                    .OnCompleted(() => Swiped?.Invoke(this, new SwipedEventArgs(direction))).Start();
                return;
            }
            SpringBack();
            if (commit)
                Swiped?.Invoke(this, new SwipedEventArgs(direction));
        }

        private void Cancel()
        {
            var claimed = _claimed;
            End();
            if (claimed)
                SpringBack();
        }

        private void End()
        {
            _tracking = false;
            _claimed = false;
            if (Screen is { } screen && ReferenceEquals(screen.TouchScrollOwner, this))
                screen.TouchScrollOwner = null;
            Dragging?.Invoke(this, 0);
        }

        private void SpringBack()
        {
            _offset = 0;
            if (FollowsFinger && _content != null && _content.Translation != Vector2.Zero)
            {
                var actions = (_leftAction, _rightAction);
                _content.Animate().TranslateTo(Vector2.Zero).Spring(SpringSpec.Snappy).OnCompleted(() =>
                {
                    if (actions._leftAction != null)
                        actions._leftAction.IsVisible = false;
                    if (actions._rightAction != null)
                        actions._rightAction.IsVisible = false;
                }).Start();
            }
        }
    }
}
