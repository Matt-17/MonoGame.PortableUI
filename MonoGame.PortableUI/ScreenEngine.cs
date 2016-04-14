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
            Fonts = LoadFonts(game, "Segoe-light-14");

            Manager.Initialize();
        }

        private static Dictionary<string, SpriteFont> LoadFonts(Game game, params string[] fontList)
        {
            var fonts = new Dictionary<string, SpriteFont>();
            foreach (var font in fontList)
            {
                fonts[$"{font}"] = game.Content.Load<SpriteFont>($@"Fonts/{font}");
            }
            return fonts;
        }

        public static Texture2D Pixel { get; private set; }
        public static Dictionary<string, SpriteFont> Fonts { get; set; }

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