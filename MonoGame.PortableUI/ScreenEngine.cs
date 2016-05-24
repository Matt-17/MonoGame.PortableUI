using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI
{
    public class ScreenEngine
    {
        private static ScreenManager _manager;

        public static float ScaleFactor { get; set; }

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

                           
            ScaleFactor = 1;

            Manager.Initialize();
        }
        

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