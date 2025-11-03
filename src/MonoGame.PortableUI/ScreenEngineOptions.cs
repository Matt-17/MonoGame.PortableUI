using System;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI
{
    public sealed class ScreenEngineOptions
    {
        public TimeSpan DoubleClickThreshold { get; set; } = TimeSpan.FromMilliseconds(400);
        public bool AddComponentToGame { get; set; } = true;
        public Effect? Effect { get; set; }
    }
}
