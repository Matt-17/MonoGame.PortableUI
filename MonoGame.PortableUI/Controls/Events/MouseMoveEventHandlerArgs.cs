using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Controls.Events
{
    public class MouseMoveEventHandlerArgs : BaseEventHandlerArgs
    {
        public MouseMoveEventHandlerArgs(Point oldPoint, Point newPoint)
        {
            OldPoint = oldPoint;
            AbsolutePoint = newPoint;
        }

        public Point OldPoint { get; set; }
        public Point AbsolutePoint { get; set; }
    }
}