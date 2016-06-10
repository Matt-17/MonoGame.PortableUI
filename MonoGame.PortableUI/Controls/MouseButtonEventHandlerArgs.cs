using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
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