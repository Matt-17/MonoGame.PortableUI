using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls.Events
{
    public class TouchEventHandlerArgs : BaseEventHandlerArgs
    {
        public TouchEventHandlerArgs(PointF position)
        {
            Position = position;
        }

        public PointF Position { get; set; }
    }
}