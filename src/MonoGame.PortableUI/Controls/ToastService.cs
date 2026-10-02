using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public enum ToastDuration
    {
        Short,
        Long
    }

    public enum ToastEdge
    {
        Top,
        Bottom
    }

    /// <summary>What happens when <see cref="ToastService.Show"/> is called with a full queue.</summary>
    public enum ToastOverflowPolicy
    {
        /// <summary>Drop the oldest waiting message (newest information wins). Default.</summary>
        DropOldest,
        /// <summary>Ignore the new message.</summary>
        DropNewest
    }

    /// <summary>
    ///     Short self-dismissing messages ("toasts") above every screen. They take no focus and no
    ///     input, are shown one at a time in order, and survive screen navigation (they belong to
    ///     the engine, not a screen); <see cref="Clear"/> removes them explicitly.
    /// </summary>
    public sealed class ToastService
    {
        private readonly ScreenEngine _engine;
        private readonly Queue<(string Message, ToastDuration Duration)> _queue = new Queue<(string, ToastDuration)>();
        private readonly ToastLayer _layer = new ToastLayer();
        private Border? _current;
        private TimeSpan _shownAt;
        private TimeSpan _visibleFor;
        private bool _leaving;

        internal ToastService(ScreenEngine engine)
        {
            _engine = engine;
            _layer.IgnoresInput = true;
            _layer.ScreenEngine = engine;
        }

        public ToastEdge Edge { get; set; } = ToastEdge.Bottom;

        /// <summary>Distance from the anchored screen edge.</summary>
        public float EdgeOffset { get; set; } = 48;

        /// <summary>Messages waiting behind the visible one; further ones follow <see cref="Overflow"/>.</summary>
        public int MaxQueued { get; set; } = 5;

        public ToastOverflowPolicy Overflow { get; set; } = ToastOverflowPolicy.DropOldest;

        public TimeSpan ShortDuration { get; set; } = TimeSpan.FromSeconds(2);

        public TimeSpan LongDuration { get; set; } = TimeSpan.FromSeconds(3.5);

        /// <summary>Fade/slide time for entering and leaving.</summary>
        public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(180);

        /// <summary>Text of the message on screen (also while it animates out), or null.</summary>
        public string? CurrentMessage => _current?.Content is TextBlock text ? text.Text : null;

        public int QueuedCount => _queue.Count;

        public event EventHandler<string>? Shown;

        public event EventHandler<string>? Dismissed;

        public void Show(string message, ToastDuration duration = ToastDuration.Short)
        {
            if (string.IsNullOrEmpty(message))
                return;

            if (_queue.Count >= Math.Max(0, MaxQueued))
            {
                if (Overflow == ToastOverflowPolicy.DropNewest || MaxQueued <= 0)
                    return;
                _queue.Dequeue();
            }

            _queue.Enqueue((message, duration));
        }

        /// <summary>Removes the visible message and everything queued, without animation.</summary>
        public void Clear()
        {
            _queue.Clear();
            if (_current != null)
                FinishCurrent();
        }

        internal Screen Layer => _layer;

        internal bool HasContent => _current != null;

        internal void Update()
        {
            var now = ScreenSystem.TotalTime;
            if (_current == null && _queue.Count > 0)
                ShowNext(now);

            if (_current != null && !_leaving && now - _shownAt >= _visibleFor)
                StartLeaving();

            if (_current != null)
                _layer.Update();
        }

        private void ShowNext(TimeSpan now)
        {
            var (message, duration) = _queue.Dequeue();
            var theme = PortableTheme.ResolveCurrent();
            _current = new Border
            {
                Content = new TextBlock { Text = message, TextColor = theme.ToolTipTextColor, TextWrapping = TextWrapping.Wrap },
                BackgroundBrush = theme.ToolTipBackgroundBrush,
                BorderBrush = theme.ToolTipBorderBrush,
                BorderThickness = theme.ToolTipBorderWidth,
                Padding = new Thickness(16, 10),
                CornerRadius = new CornerRadius(10),
                MaxWidth = 480,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = Edge == ToastEdge.Top ? VerticalAlignment.Top : VerticalAlignment.Bottom,
                Margin = Edge == ToastEdge.Top ? new Thickness(16, EdgeOffset, 16, 0) : new Thickness(16, 0, 16, EdgeOffset),
                IsHitTestVisible = false,
                IsFocusable = false
            };
            _layer.Content = _current;
            _shownAt = now;
            _visibleFor = duration == ToastDuration.Long ? LongDuration : ShortDuration;
            _leaving = false;
            // Wake an idle on-demand loop when the toast is due to leave.
            _layer.ScreenEngine?.RequestRedrawAt(now + _visibleFor);

            var slide = Edge == ToastEdge.Top ? -12 : 12;
            _current.Opacity = 0;
            _current.Translation = new Vector2(0, slide);
            _current.Animate()
                .FadeTo(1)
                .TranslateTo(Vector2.Zero)
                .Duration(AnimationDuration)
                .Ease(Easings.CubicOut)
                .Start();
            Shown?.Invoke(this, message);
        }

        private void StartLeaving()
        {
            _leaving = true;
            var toast = _current!;
            if (AnimationDuration <= TimeSpan.Zero)
            {
                FinishCurrent();
                return;
            }

            toast.Animate()
                .FadeTo(0)
                .Duration(AnimationDuration)
                .Ease(Easings.Linear)
                .OnCompleted(() =>
                {
                    if (ReferenceEquals(_current, toast))
                        FinishCurrent();
                })
                .Start();
        }

        private void FinishCurrent()
        {
            var message = CurrentMessage;
            _current = null;
            _leaving = false;
            _layer.Content = null;
            if (message != null)
                Dismissed?.Invoke(this, message);
        }

        /// <summary>Screen hosting the visible toast: laid out and animated, never given input.</summary>
        private sealed class ToastLayer : Screen
        {
        }
    }
}
