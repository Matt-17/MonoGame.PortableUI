using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI
{
    internal class ScreenManager : DrawableGameComponent
    {
        private SpriteBatch _spriteBatch;

        internal ScreenManager(Game game) : base(game)
        {
            ScreenHistory = new Stack<Screen>();
            UpdateOrder = int.MaxValue;
            DrawOrder = int.MaxValue;
        }

        public Screen ActiveScreen
        {
            get { return ScreenHistory.Peek(); }
        }

        public Stack<Screen> ScreenHistory { get; }

        public override void Initialize()
        {
            base.Initialize();
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

            ScreenHistory.Push(screen);
        }

        public void NavigateBack()
        {
            ScreenHistory.Pop();
        }
    }
}