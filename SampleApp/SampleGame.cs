using Microsoft.Xna.Framework;
using MonoGame.PortableUI;
using SampleApp.Screens;

namespace SampleApp
{
    public class SampleGame : Game
    {
        public static Game GameInstance;
        private GraphicsDeviceManager _graphics;
        private bool _invalid;
        private int _width;
        private int _height;
        private ScreenEngine _screenEngine;

        public SampleGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferMultiSampling = true;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            //Window.AllowUserResizing = true;
            //Window.ClientSizeChanged += (sender, eventArgs) => _invalid = true;

            GameInstance = this;
        }

        public new void Initialize()
        {

        }

        protected override void LoadContent()
        {
            FontManager.LoadFonts(this, "Segoe");
            _screenEngine = ScreenEngine.Initialize(this);
            Components.Add(ScreenEngine.ScreenComponent);
            _screenEngine.NavigateToScreen(new StartScreen());
            _screenEngine.SetScreenSize(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        }

        protected override void Update(GameTime gameTime)
        {
            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            //    Exit();
            if (_invalid)
            {
                _graphics.PreferredBackBufferWidth = _width;
                _graphics.PreferredBackBufferHeight = _height;
                _graphics.ApplyChanges();
                _screenEngine.SetScreenSize(_width, _height);
                _invalid = false;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }

        public void SetClientSize(int width, int height)
        {
            _width = width;
            _height = height;
            _invalid = true;
        }
    }
}
