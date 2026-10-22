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
                // Feedback: the phone's own haptics for touch, rumble for a paired gamepad, theme sounds.
                _engine.Haptics.Players.Add(new AndroidHaptics(view));
                _engine.Haptics.Players.Add(new MonoGame.PortableUI.Haptics.GamePadRumbleHaptics());
                _engine.Sounds.UseThemeSounds = true;
            }
        }

        /// <summary>Whether the system back key closes an opened page (otherwise Android handles it). UI thread.</summary>
        public bool CanHandleBack => _engine?.ActiveScreen is DetailPage;

        /// <summary>The system back key, from the activity (UI thread): closes an opened page on the game thread.</summary>
        public bool HandleBack()
        {
            if (_engine == null || !CanHandleBack)
                return false;
            _engine.InvokeOnGameThread(() =>
            {
                if (_engine.ActiveScreen is DetailPage)
                    _engine.NavigateBack();
            });
            return true;
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            _screen?.SyncRenderQuality();
            // On demand the frame after a transition may never be drawn: the trace ends here.
            if (_tracing && !IsTraced)
            {
                _tracing = false;
                global::Android.Util.Log.Info("PortableUI.Nav", _transitionTrace.ToString());
            }
        }

        protected override void LoadContent()
        {
            FontManager.LoadFonts(this, "default", "Segoe");
            DemoFonts.RegisterThemeFonts();
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

        // Frame times while a screen transition runs, logged when it ends (logcat tag PortableUI.Nav): each entry is the
        // interval since the previous frame / that frame's draw time in ms; the first frame has only its draw time.
        private readonly System.Text.StringBuilder _transitionTrace = new System.Text.StringBuilder();
        private long _lastDrawTimestamp;
        private bool _tracing;

        protected override void Draw(GameTime gameTime)
        {
            var now = System.Diagnostics.Stopwatch.GetTimestamp();
            var interval = System.Diagnostics.Stopwatch.GetElapsedTime(_lastDrawTimestamp, now).TotalMilliseconds;
            _lastDrawTimestamp = now;
            GraphicsDevice.Clear(Color.White);
            base.Draw(gameTime);
            TraceTransition(interval, System.Diagnostics.Stopwatch.GetElapsedTime(now).TotalMilliseconds);
        }

        // A screen transition, a card pulse or scrolling on a detail page.
        private bool IsTraced => _engine is { IsTransitioning: true } || _engine?.ActiveScreen is DetailPage { IsPulsing: true } or DetailPage { IsScrolling: true };

        private void TraceTransition(double interval, double draw)
        {
            if (_engine == null || !IsTraced)
                return;
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            if (!_tracing)
            {
                _tracing = true;
                if (!_engine.IsTransitioning)
                    _transitionTrace.Clear().Append(_engine.ActiveScreen is DetailPage { IsPulsing: true } ? "pulse: -/" : "scroll: -/");
                else
                    _transitionTrace.Clear()
                        .Append(_engine.ActiveScreen is DetailPage ? "open" : "back")
                        .Append(_engine.WarmUpScreens ? ", warm-up on: -/" : ", warm-up off: -/");
                _transitionTrace.Append(draw.ToString("0", culture));
                return;
            }
            _transitionTrace.Append(' ').Append(interval.ToString("0", culture)).Append('/').Append(draw.ToString("0", culture));
        }
    }
}
