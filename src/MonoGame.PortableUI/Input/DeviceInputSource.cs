using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Input;

namespace MonoGame.PortableUI.Input
{
    public sealed class DeviceInputSource : IInputSource
    {
        private static readonly MouseButton[] EmptyButtons = new MouseButton[0];

        public static DeviceInputSource Instance { get; } = new DeviceInputSource();

        private DeviceInputSource()
        {
        }

        public PointF MousePosition => (PointF)Mouse.GetState().Position;

        public IReadOnlyCollection<MouseButton> PressedMouseButtons
        {
            get
            {
                var mouseState = Mouse.GetState();
                var buttons = new List<MouseButton>(3);
                if (mouseState.LeftButton == ButtonState.Pressed)
                    buttons.Add(MouseButton.Left);
                if (mouseState.RightButton == ButtonState.Pressed)
                    buttons.Add(MouseButton.Right);
                if (mouseState.MiddleButton == ButtonState.Pressed)
                    buttons.Add(MouseButton.Middle);
                return buttons.Count == 0 ? EmptyButtons : buttons;
            }
        }

        public int ScrollWheelValue => Mouse.GetState().ScrollWheelValue;

        public TouchCollection Touches => TouchPanel.GetState();
    }
}
