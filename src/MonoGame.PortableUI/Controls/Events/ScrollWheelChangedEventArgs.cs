using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls.Events
{
    public class ScrollWheelChangedEventArgs : BaseEventArgs
    {
        public PointF Position { get; set; }
        public int Delta { get; set; }

        /// <summary>True for the horizontal wheel/tilt, or the vertical wheel with Shift held.</summary>
        public bool IsHorizontal { get; set; }

        public ScrollWheelChangedEventArgs(PointF position, int delta)
        {
            Position = position;
            Delta = delta;
        }
    }
}