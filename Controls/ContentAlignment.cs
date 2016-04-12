using System;

namespace MonoGame.PortableUI.Controls
{
    [Flags]
    public enum ContentAlignment
    {
        Left = 0,
        Right = 1,
        Top = 2,
        Bottom = 4,
        CenterHorizontal = 8,
        CenterVertical = 16
    }
}