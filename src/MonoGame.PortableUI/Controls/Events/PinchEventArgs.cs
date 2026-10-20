using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls.Events
{
    /// <summary>
    ///     Two fingers on a control (<see cref="Control.Pinch"/>): how far apart they moved and how far they turned
    ///     since the last event, and the point between them (layout units). Multiply <see cref="Scale"/> into a zoom
    ///     and add <see cref="Rotation"/> to an angle frame by frame.
    /// </summary>
    public sealed class PinchEventArgs : BaseEventArgs
    {
        public PinchEventArgs(PointF center, float scale, float rotation)
        {
            Center = center;
            Scale = scale;
            Rotation = rotation;
        }

        /// <summary>The point midway between the fingers.</summary>
        public PointF Center { get; }

        /// <summary>The fingers' distance now over their distance at the last event (1 at the start).</summary>
        public float Scale { get; }

        /// <summary>How far the line between the fingers turned since the last event, in radians (clockwise positive).</summary>
        public float Rotation { get; }
    }
}
