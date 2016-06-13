using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI
{
    public class ScreenEngine
    {
        private static ScreenManager _manager;

        public static float ScaleFactor { get; set; }

        public static Control FocusedControl { get; set; }

        public static Rect? _Rect { get; set; }

        internal static ScreenManager Manager
        {
            get
            {
                if (_manager == null)
                    throw new TypeInitializationException("ScreenManager", new ArgumentNullException());
                return _manager;
            }
        }

        public static void Initialize(Game game)
        {
            _manager = new ScreenManager(game);
            if (_Rect != null)
            {
                _manager.Width = (int) _Rect.Value.Width;
                _manager.Height =(int) _Rect.Value.Height ;
                _manager.ActiveScreen?.InvalidateLayout(true);
            }
            game.Components.Add(Manager);


            ScaleFactor = 1;

            Manager.Initialize();
        }


        public static void NavigateToScreen<T>(T screen) where T : Screen
        {
            Manager.NavigateToScreen(screen);
        }

        public static void SetScreenSize(int width, int height)
        {
            if (_manager != null)
            {
                _manager.Width = width;
                _manager.Height = height;
                _manager.ActiveScreen?.InvalidateLayout(true);
            }
            else
            {
                _Rect = new Rect(width, height);
            }
        }

        public static void NavigateBack()
        {
            Manager.NavigateBack();
        }

        public static void SetScreenSize()
        {
        }
    }
}