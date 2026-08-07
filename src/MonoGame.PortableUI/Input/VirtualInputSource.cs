using System.Collections.Generic;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Input;

namespace MonoGame.PortableUI.Input
{
    public sealed class VirtualInputSource : IInputSource
    {
        private readonly List<MouseButton> _pressedButtons = new List<MouseButton>(3);

        public PointF MousePosition { get; private set; }

        public IReadOnlyCollection<MouseButton> PressedMouseButtons => _pressedButtons;

        public int ScrollWheelValue { get; private set; }

        public TouchCollection Touches { get; private set; }

        public Microsoft.Xna.Framework.Input.KeyboardState KeyboardState { get; private set; }

        public void SetKeyboardState(Microsoft.Xna.Framework.Input.KeyboardState keyboardState)
        {
            KeyboardState = keyboardState;
        }

        public void SetPointer(PointF position, bool leftDown = false, bool rightDown = false, bool middleDown = false)
        {
            MousePosition = position;
            _pressedButtons.Clear();
            if (leftDown)
                _pressedButtons.Add(MouseButton.Left);
            if (rightDown)
                _pressedButtons.Add(MouseButton.Right);
            if (middleDown)
                _pressedButtons.Add(MouseButton.Middle);
        }

        public void SetScrollWheelValue(int value)
        {
            ScrollWheelValue = value;
        }

        public void SetTouches(TouchCollection touches)
        {
            Touches = touches;
        }
    }
}
