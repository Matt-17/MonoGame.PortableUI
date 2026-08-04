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

        public AndroidDemoGame()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                IsFullScreen = true,
                SupportedOrientations = DisplayOrientation.Portrait
            };

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
                Theme = PortableThemes.Default.CreateTheme()
            });
            base.Initialize();
        }

        protected override void LoadContent()
        {
            FontManager.LoadFonts(this, "default", "Segoe");
            FontManager.DefaultFont = FontManager.GetFontOrDefault("default");

            // Route real Android touch coordinates into the library. Without configuring the display
            // size the TouchPanel reports untransformed device pixels; matching the back buffer keeps
            // hit-testing aligned with what is drawn.
            var pp = GraphicsDevice.PresentationParameters;
            TouchPanel.DisplayWidth = pp.BackBufferWidth;
            TouchPanel.DisplayHeight = pp.BackBufferHeight;
            TouchPanel.EnabledGestures = GestureType.Tap | GestureType.VerticalDrag | GestureType.HorizontalDrag | GestureType.Flick;

            _engine?.NavigateToScreen(new AndroidDemoScreen());
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);
            base.Draw(gameTime);
        }
    }
}
