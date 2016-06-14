using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls.Events
{
    public class ScrollWheelChangedEventHandlerArgs : BaseEventHandlerArgs
    {
        public PointF Position { get; set; }
        public int Delta { get; set; }

        public ScrollWheelChangedEventHandlerArgs(PointF position, int delta)
        {
            Position = position;
            Delta = delta;
        }
    }
}