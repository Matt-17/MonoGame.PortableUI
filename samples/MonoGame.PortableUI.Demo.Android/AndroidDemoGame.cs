using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Themes;

namespace MonoGame.PortableUI.Demo.Android
{
    /// <summary>
    /// Minimal Android host game for MonoGame.PortableUI: initializes the <see cref="ScreenEngine"/>
    /// with the Android clipboard service and the default portable theme, loads the "default" font,
    /// wires the touch panel to the real back-buffer size, and shows <see cref="AndroidDemoScreen"/>.
    /// </summary>
    public sealed class AndroidDemoGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private ScreenEngine? _engine;
        private AndroidDemoScreen? _screen;

        private readonly System.Action<bool>? _setFullscreen;

        public AndroidDemoGame(System.Action<bool>? setFullscreen = null)
        {
            _setFullscreen = setFullscreen;
            _graphics = new GraphicsDeviceManager(this)
            {
                IsFullScreen = false, // system bars stay visible; the window draws under them (edge to edge)
                SupportedOrientations = DisplayOrientation.Portrait,
                SynchronizeWithVerticalRetrace = true
            };
            // Frames follow the display's vsync (90 Hz on a Pixel 5) instead of a fixed 60 Hz step,
            // which on a 90 Hz panel shows every third refresh twice (judder while scrolling).
            IsFixedTimeStep = false;

            // Pin the back buffer to the real display resolution. Left at its default, MonoGame's
            // Android back buffer comes back smaller than the GL surface it actually renders into
            // (a density-scaled size), so SpriteBatch draws stretched to the surface while
            // GraphicsDevice.ScissorRectangle is applied in the smaller back-buffer space. The two
            // diverge with distance from the origin, which clips content-tight scissor rects (button
            // and list-item text sized to the text) while leaving stretched ones intact. Matching the
            // back buffer to the surface keeps drawing and scissoring in the same coordinate space.
            var metrics = global::Android.App.Application.Context.Resources?.DisplayMetrics;
            if (metrics != null)
            {
                _graphics.PreferredBackBufferWidth = metrics.WidthPixels;
                _graphics.PreferredBackBufferHeight = metrics.HeightPixels;
            }

            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            _engine = ScreenEngine.Initialize(this, new ScreenEngineOptions
            {
                ClipboardService = new AndroidClipboardService(),
                // Android 12+ look: lists stretch at their ends instead of moving past them.
                OverscrollEffect = MonoGame.PortableUI.Controls.OverscrollEffect.Stretch,
                // Mobile style: scroll bars appear while scrolling and fade out shortly after.
                ScrollBarVisibility = MonoGame.PortableUI.Controls.ScrollBarVisibility.AutoHide,
                // Lay out in dp and draw at native resolution: touch targets get their Android size.
                LayoutScale = global::Android.App.Application.Context.Resources?.DisplayMetrics?.Density ?? 1f,
                // App-style: draw only when the UI changes, so the phone idles while nothing moves.
                RenderMode = RenderMode.OnDemand,
                // Touch and keys wake the loop at once (AndroidInputBridge), so idle polling can be slow.
                IdleUpdateInterval = System.TimeSpan.FromMilliseconds(250),
                Theme = PortableThemes.Default.CreateTheme()
            });
            base.Initialize();
            // Edge-to-edge window: the UI draws under the bars and keeps clear via SafeAreaPanel.
            if (Services.GetService(typeof(global::Android.Views.View)) is global::Android.Views.View view)
            {
                // Back buffer = the view's real size (edge to edge it covers the system bars).
                AndroidSurfaceSize.Follow(view, _graphics, _engine);
                AndroidWindowInsets.Attach(view, _engine);
                AndroidInputBridge.Attach(view, _engine);
                _engine.OnScreenKeyboard = new AndroidOnScreenKeyboard(view, _engine);
                _engine.AccessibilityBridge = new MonoGame.PortableUI.Accessibility.AndroidAccessibilityBridge(view, _engine);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            _screen?.SyncRenderQuality();
        }

        protected override void LoadContent()
        {
            FontManager.LoadFonts(this, "default", "Segoe");
            FontManager.DefaultFont = FontManager.GetFontOrDefault("default");
            // Text is scaled by the display density (TextScaling.DensityScale); a runtime-rasterized
            // font keeps it sharp where the 14 pt SpriteFont would be stretched.
            FontManager.DefaultDynamicFont = DemoFonts.Selawik;

            // Route real Android touch coordinates into the library. Without configuring the display
            // size the TouchPanel reports untransformed device pixels; matching the back buffer keeps
            // hit-testing aligned with what is drawn.
            var pp = GraphicsDevice.PresentationParameters;
            TouchPanel.DisplayWidth = pp.BackBufferWidth;
            TouchPanel.DisplayHeight = pp.BackBufferHeight;
            TouchPanel.EnabledGestures = GestureType.Tap | GestureType.VerticalDrag | GestureType.HorizontalDrag | GestureType.Flick;

            _screen = new AndroidDemoScreen(_setFullscreen);
            _engine?.NavigateToScreen(_screen);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);
            base.Draw(gameTime);
        }
    }
}
