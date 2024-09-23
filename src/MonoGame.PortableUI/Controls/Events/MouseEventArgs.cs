using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Input;

namespace MonoGame.PortableUI.Controls.Events
{
    public class MouseEventArgs : BaseEventArgs
    {
        public MouseEventArgs(PointF position, List<MouseButton> buttons)
        {
            Position = position;
            Buttons = buttons;
        }
        public MouseEventArgs(PointF position, MouseButton mouseButton)
        {
            Position = position;
            Buttons = new List<MouseButton> { mouseButton};
        }

        public List<MouseButton> Buttons { get; set; }         
        public PointF Position { get; set; }
    }
}