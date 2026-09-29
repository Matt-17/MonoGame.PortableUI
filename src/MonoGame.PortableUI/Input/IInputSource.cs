using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Input;

namespace MonoGame.PortableUI.Input
{
    public interface IInputSource
    {
        PointF MousePosition { get; }
        IReadOnlyCollection<MouseButton> PressedMouseButtons { get; }
        int ScrollWheelValue { get; }
        TouchCollection Touches { get; }
        KeyboardState KeyboardState { get; }

        /// <summary>First gamepad (D-pad/left stick navigate focus, A activates, B goes back,
        /// shoulders tab). Defaults to "no gamepad" so existing sources keep compiling.</summary>
        GamePadState GamePad => default;

        /// <summary>Accumulated horizontal wheel/tilt value (0 where the device has none).</summary>
        int HorizontalScrollWheelValue => 0;
    }
}
