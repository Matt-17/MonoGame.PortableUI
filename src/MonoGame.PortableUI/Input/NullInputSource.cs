using System.Collections.Generic;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Input;

namespace MonoGame.PortableUI.Input
{
    public sealed class NullInputSource : IInputSource
    {
        private static readonly MouseButton[] EmptyButtons = new MouseButton[0];

        public static NullInputSource Instance { get; } = new NullInputSource();

        private NullInputSource()
        {
        }

        public PointF MousePosition => new PointF();

        public IReadOnlyCollection<MouseButton> PressedMouseButtons => EmptyButtons;

        public int ScrollWheelValue => 0;

        public TouchCollection Touches => default(TouchCollection);

        public Microsoft.Xna.Framework.Input.KeyboardState KeyboardState => default(Microsoft.Xna.Framework.Input.KeyboardState);
    }
}
