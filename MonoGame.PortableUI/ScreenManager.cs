using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI
{
    internal class ScreenManager : DrawableGameComponent
    {
        private SpriteBatch _spriteBatch;
        public static TimeSpan Time { get; private set; }

        public Control FocusedControl { get; set; }

       internal ScreenManager(Game game) : base(game)
        {
            ScreenHistory = new Stack<Screen>();
            UpdateOrder = int.MaxValue;
            DrawOrder = int.MaxValue;
        }

        public Screen ActiveScreen
        {
            get { return ScreenHistory.Count > 0 ? ScreenHistory.Peek() : null; }
        }

        public Stack<Screen> ScreenHistory { get; }
        public int Width { get; set; }
        public int Height { get; set; }

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

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            Time = gameTime.TotalGameTime;
            var time = gameTime.ElapsedGameTime;
            ActiveScreen.Update(time);
        }

        public void NavigateToScreen<TScreen>(TScreen screen) where TScreen : Screen
        {
            screen.ScreenManager = this;

            ScreenHistory.Push(screen);
        }

        public void NavigateBack()
        {
            ScreenHistory.Pop();
        }
    }
}