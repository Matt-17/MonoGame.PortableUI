using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Demo
{
    public sealed class DemoGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private ScreenEngine _screenEngine;

        public DemoGame()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1180,
                PreferredBackBufferHeight = 760
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            Window.Title = "MonoGame.PortableUI Demo";
        }

        protected override void Initialize()
        {
            _screenEngine = ScreenEngine.Initialize(this);
            ApplyScreenSize();
            Window.ClientSizeChanged += (sender, args) => ApplyScreenSize();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            FontManager.LoadFonts(this, "Segoe", "default", "arial");
            var deleteIcon = Content.Load<Texture2D>("Images/ic_delete");
            _screenEngine.NavigateToScreen(new MainScreen(deleteIcon));
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(34, 36, 40));
            base.Draw(gameTime);
        }

        private void ApplyScreenSize()
        {
            _screenEngine?.SetScreenSize(Window.ClientBounds.Width, Window.ClientBounds.Height);
        }
    }
}
