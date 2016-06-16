using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI
{
    internal class ScreenComponent : DrawableGameComponent
    {
        private readonly ScreenEngine _screenEngine;
        private SpriteBatch _spriteBatch;

        public Control FocusedControl { get; set; }

        internal ScreenComponent(ScreenEngine screenEngine, Game game) : base(game)
        {
            _screenEngine = screenEngine;
            UpdateOrder = int.MaxValue;
            DrawOrder = int.MaxValue;
        }

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
            _screenEngine.ActiveScreen.Draw(_spriteBatch);
        }

        public override void Update(GameTime gameTime)
        {
            _screenEngine.Update(gameTime);
        }
    }
}