using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls.Events
{
    public class ScrollWheelChangedEventArgs : BaseEventArgs
    {
        public PointF Position { get; set; }
        public int Delta { get; set; }

        public ScrollWheelChangedEventArgs(PointF position, int delta)
        {
            Position = position;
            Delta = delta;
        }
    }
}