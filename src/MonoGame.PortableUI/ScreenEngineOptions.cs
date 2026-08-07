using System;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI
{
    public sealed class ScreenEngineOptions
    {
        private PortableTheme _theme = PortableTheme.CreateDefault();

        public TimeSpan DoubleClickThreshold { get; set; } = TimeSpan.FromMilliseconds(400);
        public TimeSpan ToolTipHoverDelay { get; set; } = TimeSpan.FromMilliseconds(500);
        public TimeSpan ToolTipLongPressDelay { get; set; } = TimeSpan.FromMilliseconds(650);
        public PointF ToolTipOffset { get; set; } = new PointF(12, 18);
        public float ToolTipScreenPadding { get; set; } = 8;
        public IClipboardService ClipboardService { get; set; } = NullClipboardService.Instance;
        public bool AddComponentToGame { get; set; } = true;
        public ScreenSizeMode ScreenSizeMode { get; set; } = ScreenSizeMode.Viewport;
        public Effect? Effect { get; set; }

        /// <summary>
        /// Design ("logical") resolution the UI is authored for. When set (both components &gt; 0),
        /// the whole screen is laid out in this virtual space and the finished frame is uniformly
        /// scaled to fill the real window, so controls keep their proportions on larger displays
        /// instead of merely gaining empty space. The larger window axis receives extra logical
        /// space (no letter-boxing) so the reference area is always fully visible. Zero (the
        /// default) disables scaling: layout uses the raw viewport, exactly as before.
        /// </summary>
        public PointF ReferenceSize { get; set; }

        /// <summary>
        /// The engine these options belong to, set once by <see cref="ScreenEngine"/>'s constructor.
        /// Lets the <see cref="Theme"/> setter invalidate the screen this instance actually drives
        /// instead of always the process-wide primary engine (relevant for secondary engines such as
        /// the one behind each <see cref="UISurface"/>).
        /// </summary>
        internal ScreenEngine? Owner { get; set; }

        public PortableTheme Theme
        {
            get { return _theme; }
            set
            {
                var nextTheme = value ?? PortableTheme.CreateDefault();
                if (ReferenceEquals(_theme, nextTheme))
                    return;

                _theme = nextTheme;
                ThemeVersion.Next();
                Owner?.ActiveScreen?.InvalidateLayout(true);
            }
        }
    }
}
