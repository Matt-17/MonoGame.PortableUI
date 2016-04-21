using System;
using System.Collections.Generic;
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
            game.Components.Add(Manager);

            Pixel = new Texture2D(game.GraphicsDevice, 1, 1);
            Pixel.SetData(new[] { Color.White });

            Manager.Initialize();
        }
        
        public static Texture2D Pixel { get; private set; }

        public static void NavigateToScreen<T>(T screen) where T : Screen
        {
            Manager.NavigateToScreen(screen);
        }

        public static void NavigateBack()
        {
            Manager.NavigateBack();
        }
    }
}