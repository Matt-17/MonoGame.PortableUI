using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls.Events
{
    public class MouseButtonEventHandlerArgs : BaseEventHandlerArgs
    {
        public MouseButtonEventHandlerArgs(PointF absolutePoint)
        {
            AbsolutePoint = absolutePoint;
        }

        public PointF AbsolutePoint { get; set; }
    }
}