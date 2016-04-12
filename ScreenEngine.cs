using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI
{
    public class ScreenEngine : DrawableGameComponent
    {
        public Screen ActiveScreen
        {
            get { return _screenHistory.Peek(); }
        }
        private readonly Stack<Screen> _screenHistory;

        public static ScreenEngine InitializeScreenEngine(Game game)
        {
            var engine = new ScreenEngine(game);
            game.Components.Add(engine);
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
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
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
        }

        public void NavigateToScreen<TScreen>(TScreen screen) where TScreen : Screen
        {
            screen.ScreenEngine = this;

            _screenHistory.Push(screen);
        }
    }
}