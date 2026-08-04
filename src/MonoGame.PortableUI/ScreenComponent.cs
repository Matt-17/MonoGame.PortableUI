using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI
{
    internal class ScreenComponent : DrawableGameComponent
    {
        private readonly ScreenEngine _screenEngine;
        private SpriteBatch? _spriteBatch;

        internal ScreenComponent(ScreenEngine screenEngine, Game game) : base(game)
        {
            _screenEngine = screenEngine;
            UpdateOrder = int.MaxValue;
            DrawOrder = int.MaxValue;
        }

        public override void Initialize()
        {
            base.Initialize();
            ApplyViewportSize();
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        public override void Draw(GameTime gameTime)
        {
            if (_spriteBatch != null)
                _screenEngine.ActiveScreen?.Draw(_spriteBatch);
        }

        public override void Update(GameTime gameTime)
        {
            ApplyViewportSize();
            _screenEngine.Update(gameTime);
        }

        private void ApplyViewportSize()
        {
            var viewport = GraphicsDevice.Viewport;
            _screenEngine.ApplyViewportSize(viewport.Width, viewport.Height);
        }
    }
}
