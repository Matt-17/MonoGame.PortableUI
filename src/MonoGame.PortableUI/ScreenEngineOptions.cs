using System;
using System.Collections.Generic;
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

        /// <summary>The platform software keyboard text fields raise (default: none, for desktop).</summary>
        public Input.IOnScreenKeyboard OnScreenKeyboard { get; set; } = Input.NullOnScreenKeyboard.Instance;
        public bool AddComponentToGame { get; set; } = true;

        /// <summary>
        ///     Show hover visuals for touch input too. Off by default: on phones the pointer that a
        ///     finger leaves behind would keep the last tapped control looking hovered. A real mouse
        ///     (also on Android/ChromeOS) always hovers.
        /// </summary>
        public bool HoverOnTouch { get; set; }

        /// <summary>
        ///     How long a finger must rest before it counts as a press (pressed look, list hold-to-select).
        ///     A drag that starts earlier is a scroll and lights nothing up. Default 200 ms.
        /// </summary>
        public TimeSpan TouchPressedDelay { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>Default scroll bar behaviour of every <c>ScrollViewer</c> (one can override it).</summary>
        public Controls.ScrollBarVisibility ScrollBarVisibility { get; set; } = Controls.ScrollBarVisibility.Visible;

        /// <summary>How long an <see cref="Controls.ScrollBarVisibility.AutoHide"/> bar stays after the last scroll movement.</summary>
        public TimeSpan ScrollBarAutoHideDelay { get; set; } = TimeSpan.FromSeconds(0.75);

        /// <summary>Default look of a drag past the end of a scroll viewer (Shift = rubber band,
        /// Stretch = Android 12+ stretch). A <c>ScrollViewer.OverscrollEffect</c> overrides it.</summary>
        public Controls.OverscrollEffect OverscrollEffect { get; set; } = Controls.OverscrollEffect.Shift;
        public ScreenSizeMode ScreenSizeMode { get; set; } = ScreenSizeMode.Viewport;

        /// <summary>
        ///     Layout units per pixel divisor, e.g. the Android display density (2.75 on a Pixel 5):
        ///     the UI is laid out in density-independent units (window / LayoutScale) and drawn at
        ///     native resolution through the render transform, so it stays sharp. 0 or 1 (default):
        ///     layout units are pixels. Ignored when <see cref="ReferenceSize"/> is set.
        /// </summary>
        public float LayoutScale { get; set; }
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
        ///     <see cref="PortableUI.RenderMode.Continuous"/> (default) draws every frame;
        ///     <see cref="PortableUI.RenderMode.OnDemand"/> draws only when the UI changed and idles the
        ///     game loop otherwise (battery-friendly apps). Can be switched at runtime.
        /// </summary>
        public RenderMode RenderMode { get; set; } = RenderMode.Continuous;

        /// <summary>
        ///     Update period while <see cref="RenderMode"/> is OnDemand and nothing needs drawing: input
        ///     and timers are still polled at this rate (default 33 ms, ~30 Hz). Longer saves more power
        ///     but delays the first frame after a touch by up to this much.
        /// </summary>
        public TimeSpan IdleUpdateInterval { get; set; } = TimeSpan.FromMilliseconds(33);

        /// <summary>
        /// The engine these options belong to, set once by <see cref="ScreenEngine"/>'s constructor.
        /// Lets the <see cref="Theme"/> setter invalidate the screen this instance actually drives
        /// instead of always the process-wide primary engine (relevant for secondary engines such as
        /// the one behind each <see cref="UISurface"/>).
        /// </summary>
        internal ScreenEngine? Owner { get; set; }

        /// <summary>
        ///     Display effects that belong to the screen this UI is shown on, not to its look — e.g.
        ///     the curvature (<see cref="CrtBarrelPostEffect"/>) and scanlines of an in-world CRT
        ///     monitor. They run after the theme's <see cref="PortableTheme.PostEffects"/> and stay when
        ///     the theme changes. Pointer input is mapped through a barrel here as well.
        /// </summary>
        public IReadOnlyList<PostEffect> PostEffects { get; set; } = Array.Empty<PostEffect>();

        /// <summary>
        ///     Draws the theme's <see cref="PortableTheme.Cursor"/> at the pointer as part of the UI
        ///     (so display effects such as CRT curvature bend it too). Off by default: the system
        ///     pointer is used. Turn it on for in-world screens or games that hide the OS cursor.
        /// </summary>
        public bool ShowSoftwareCursor { get; set; }

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
