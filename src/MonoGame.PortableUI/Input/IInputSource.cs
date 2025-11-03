using System.Collections.Generic;
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
    }
}
