using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI
{
    public class ScreenEngine
    {
        public Game Game { get; set; }
        private readonly ScreenComponent _component;

        private ScreenEngine(Game game)
        {
            Game = game;
            ScreenHistory = new Stack<Screen>();
            _component = new ScreenComponent(this, game);
            ScaleFactor = 1;
            Component.Initialize();

        }

        public static float ScaleFactor { get; set; }

        public static Control FocusedControl { get; set; }

        public Rect ScreenRect { get; set; }

        internal ScreenComponent Component => _component;

        public static DrawableGameComponent ScreenComponent
        {
            get
            {
                if (Instance == null)
                    throw new TypeInitializationException("ScreenEngine", new ArgumentNullException());
                return Instance.Component;
            }
        }

        public static ScreenEngine Instance { get; private set; }

        public static ScreenEngine Initialize(Game game)
        {
            Instance = new ScreenEngine(game);
            return Instance;
        }


        public void SetScreenSize(int width, int height)
        {
            ScreenRect = new Rect(width, height);
            ActiveScreen?.InvalidateLayout(true);
        }

        public Stack<Screen> ScreenHistory { get; }
        public Screen ActiveScreen => ScreenHistory.Count > 0 ? ScreenHistory.Peek() : null;

        public void NavigateToScreen<T>(T screen) where T : Screen
        {
            screen.ScreenEngine = this;
            ScreenHistory.Push(screen);
        }

        public void NavigateBack()
        {
            var screen = Instance.ScreenHistory.Pop();
            screen.ScreenEngine = null;
        }

        public void Update(GameTime gameTime)
        {
            ScreenSystem.TotalTime = gameTime.TotalGameTime;
            ActiveScreen.Update();
        }
    }
}