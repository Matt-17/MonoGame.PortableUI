using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls.Events
{
    public class TouchEventArgs : BaseEventArgs
    {
        public TouchEventArgs(PointF position)
        {
            Position = position;
        }

        public PointF Position { get; set; }

        /// <summary>A touch-down that caught a moving scroll viewer belongs to it alone (no press below).</summary>
        internal Control? CaughtBy { get; set; }
    }
}