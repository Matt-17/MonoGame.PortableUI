using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Input;

namespace MonoGame.PortableUI.Input
{
    /// <summary>
    ///     What a screen reads instead of <see cref="DeviceInputSource"/> while the game window is not the
    ///     active window: the pointer has left (hover and tooltips end), nothing is pressed, and the wheel
    ///     stays where it was so neither leaving nor returning reads as a scroll.
    /// </summary>
    internal sealed class InactiveWindowInputSource : IInputSource
    {
        private static readonly MouseButton[] EmptyButtons = new MouseButton[0];

        public PointF MousePosition => new PointF(-100000, -100000);

        public IReadOnlyCollection<MouseButton> PressedMouseButtons => EmptyButtons;

        public int ScrollWheelValue { get; set; }

        public int HorizontalScrollWheelValue { get; set; }

        public TouchCollection Touches => default(TouchCollection);

        public KeyboardState KeyboardState => default(KeyboardState);
    }
}
