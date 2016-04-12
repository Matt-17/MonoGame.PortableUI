using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI
{
    public class ScreenEngine : DrawableGameComponent
    {
        public Screen ActiveScreen
        {
            get { return _screenHistory.Peek(); }
        }
        private readonly Stack<Screen> _screenHistory;
        private SpriteBatch _spriteBatch;

        public static ScreenEngine Initialize(Game game)
        {
            var engine = new ScreenEngine(game);
            game.Components.Add(engine);
            engine.LoadContent();
            return engine;
        }

        private ScreenEngine(Game game) : base(game)
        {
            _screenHistory = new Stack<Screen>();
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            ActiveScreen.Draw(_spriteBatch);
        }

        protected override void OnVisibleChanged(object sender, EventArgs args)
        {
            base.OnVisibleChanged(sender, args);
        }

        protected override void OnDrawOrderChanged(object sender, EventArgs args)
        {
            base.OnDrawOrderChanged(sender, args);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            var elapsed = gameTime.ElapsedGameTime;
            ActiveScreen.Update(elapsed);
        }

        public void NavigateToScreen<TScreen>(TScreen screen) where TScreen : Screen
        {
            screen.ScreenEngine = this;

            _screenHistory.Push(screen);
        }
    }
}