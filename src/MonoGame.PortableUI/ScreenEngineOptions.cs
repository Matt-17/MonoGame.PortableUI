using System;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI
{
    public sealed class ScreenEngineOptions
    {
        public TimeSpan DoubleClickThreshold { get; set; } = TimeSpan.FromMilliseconds(400);
        public TimeSpan ToolTipHoverDelay { get; set; } = TimeSpan.FromMilliseconds(500);
        public TimeSpan ToolTipLongPressDelay { get; set; } = TimeSpan.FromMilliseconds(650);
        public PointF ToolTipOffset { get; set; } = new PointF(12, 18);
        public float ToolTipScreenPadding { get; set; } = 8;
        public bool AddComponentToGame { get; set; } = true;
        public ScreenSizeMode ScreenSizeMode { get; set; } = ScreenSizeMode.Viewport;
        public Effect? Effect { get; set; }
    }
}
