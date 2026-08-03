using System;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    [Flags]
    public enum DragDropEffects
    {
        None = 0,
        Copy = 1,
        Move = 2,
        Link = 4,
        All = Copy | Move | Link
    }

    /// <summary>Event args for DragEnter/DragOver/DragLeave/Drop. Targets signal acceptance by setting <see cref="Effect"/> (None = reject).</summary>
    public sealed class DragEventArgs : BaseEventArgs
    {
        internal DragEventArgs(DragOperation operation)
        {
            Operation = operation;
        }

        public DragOperation Operation { get; }
        public object? Payload => Operation.Payload;
        public Control Source => Operation.Source;
        public DragDropEffects AllowedEffects => Operation.AllowedEffects;
        public PointF Position { get; internal set; }
        public DragDropEffects Effect { get; set; }
    }

    public delegate void DragEventHandler(object? sender, DragEventArgs args);

    public sealed class DragCompletedEventArgs : EventArgs
    {
        internal DragCompletedEventArgs(Control? target, DragDropEffects effect, PointF position)
        {
            Target = target;
            Effect = effect;
            Position = position;
        }

        public Control? Target { get; }
        public DragDropEffects Effect { get; }
        public PointF Position { get; }
    }

    /// <summary>
    ///     A running drag &amp; drop operation (non-blocking): carries source, payload and allowed
    ///     effects; raises <see cref="DragMoved"/> per frame and <see cref="Completed"/>/<see cref="Canceled"/>
    ///     when the pointer is released or the drag is aborted (Esc / right-click / navigation).
    /// </summary>
    public sealed class DragOperation
    {
        internal DragOperation(Control source, object? payload, DragDropEffects allowedEffects, Control? dragVisual)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Payload = payload;
            AllowedEffects = allowedEffects;
            DragVisual = dragVisual;
        }

        public Control Source { get; }
        public object? Payload { get; }
        public DragDropEffects AllowedEffects { get; }

        /// <summary>Optional ghost control rendered above everything, following the pointer. Must not have a parent.</summary>
        public Control? DragVisual { get; }

        /// <summary>Offset between the pointer and the ghost's top-left corner.</summary>
        public PointF GrabOffset { get; set; } = new PointF(12, 12);

        public bool IsActive { get; internal set; }
        public Control? CurrentTarget { get; internal set; }
        internal DragDropEffects LastEffect { get; set; }
        internal bool IsTouchDrag { get; set; }

        public event DragEventHandler? DragMoved;
        public event EventHandler<DragCompletedEventArgs>? Completed;
        public event EventHandler? Canceled;

        internal void RaiseMoved(DragEventArgs args)
        {
            DragMoved?.Invoke(Source, args);
        }

        internal void RaiseCompleted(Control? target, DragDropEffects effect, PointF position)
        {
            Completed?.Invoke(Source, new DragCompletedEventArgs(target, effect, position));
        }

        internal void RaiseCanceled()
        {
            Canceled?.Invoke(Source, EventArgs.Empty);
        }
    }

    /// <summary>WPF-style entry point: start a drag from a MouseDown/TouchDown/LongTouch handler.</summary>
    public static class DragDrop
    {
        public static DragOperation? DoDragDrop(Control source, object? payload, DragDropEffects allowedEffects, Control? dragVisual = null)
        {
            return source?.Screen?.BeginDrag(source, payload, allowedEffects, dragVisual);
        }
    }
}
