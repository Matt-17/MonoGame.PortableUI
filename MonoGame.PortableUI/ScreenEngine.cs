using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI
{
    public class ScreenEngine
    {
        private static ScreenManager _manager;  

        internal static ScreenManager Manager
        {
            get
            {
                if (_manager == null)
                    throw new TypeInitializationException("ScreenEngine", new ArgumentNullException());
                return _manager;
            }
        }

        public static void Initialize(Game game)
        {                       
            _manager = new ScreenManager(game);
            GraphicsDevice = game.GraphicsDevice;
            game.Components.Add(Manager);

            Manager.Initialize();
        }

        internal static GraphicsDevice GraphicsDevice { get; private set; }


        public static void NavigateToScreen<T>(T screen) where T : Screen
        {
            Manager.NavigateToScreen(screen);
        }

        public static void NavigateBack()
        {
            Manager.NavigateBack();
        }

    }


    internal class ScreenManager : DrawableGameComponent
    {
        public Screen ActiveScreen
        {
            get { return ScreenHistory.Peek(); }
        }

        public Stack<Screen> ScreenHistory { get; }
        private SpriteBatch _spriteBatch;

        internal ScreenManager(Game game) : base(game)
        {
            ScreenHistory = new Stack<Screen>();
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