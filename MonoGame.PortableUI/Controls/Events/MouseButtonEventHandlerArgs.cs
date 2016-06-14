using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls.Events
{
    public class MouseButtonEventHandlerArgs : BaseEventHandlerArgs
    {
        public MouseButtonEventHandlerArgs(PointF absolutePoint, MouseButton button)
        {
            AbsolutePoint = absolutePoint;
            Button = button;
        }

        public MouseButton Button { get; set; }         
        public PointF AbsolutePoint { get; set; }
    }

    public enum MouseButton
    {
        Left,
        Middle,
        Right
    }
}