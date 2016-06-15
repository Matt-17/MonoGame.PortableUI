using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Input;

namespace MonoGame.PortableUI.Controls.Events
{
    public class MouseEventHandlerArgs : BaseEventHandlerArgs
    {
        public MouseEventHandlerArgs(PointF position, List<MouseButton> buttons)
        {
            Position = position;
            Buttons = buttons;
        }
        public MouseEventHandlerArgs(PointF position, MouseButton mouseButton)
        {
            Position = position;
            Buttons = new List<MouseButton> { mouseButton};
        }

        public List<MouseButton> Buttons { get; set; }         
        public PointF Position { get; set; }
    }
}