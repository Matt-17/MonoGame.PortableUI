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
