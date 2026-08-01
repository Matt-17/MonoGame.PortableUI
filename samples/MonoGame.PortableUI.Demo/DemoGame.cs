using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Demo
{
    public sealed class DemoGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private DemoThemePreset _activeThemePreset;
        private bool _fontsLoaded;
        private ScreenEngine? _screenEngine;

        public DemoGame()
            : this(DemoThemeRegistry.Default)
        {
        }

        public DemoGame(DemoThemePreset initialThemePreset)
        {
            _activeThemePreset = initialThemePreset ?? DemoThemeRegistry.Default;
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1180,
                PreferredBackBufferHeight = 760
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            UpdateWindowTitle();
        }

        protected override void Initialize()
        {
            _screenEngine = ScreenEngine.Initialize(this, new ScreenEngineOptions
            {
                ClipboardService = OperatingSystem.IsWindows() ? new WindowsClipboardService() : NullClipboardService.Instance,
                Theme = _activeThemePreset.CreateTheme()
            });
            base.Initialize();
        }

        protected override void LoadContent()
        {
            FontManager.LoadFonts(this, GetFontNamesToLoad());
            _fontsLoaded = true;
            ApplyTheme(_activeThemePreset);

            var deleteIcon = Content.Load<Texture2D>("Images/ic_delete");
            _screenEngine?.NavigateToScreen(new MainScreen(deleteIcon, _activeThemePreset, ApplyTheme));
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_activeThemePreset.ClearColor);
            base.Draw(gameTime);
        }

        private void ApplyTheme(DemoThemePreset themePreset)
        {
            _activeThemePreset = themePreset ?? DemoThemeRegistry.Default;
            if (_screenEngine != null)
                _screenEngine.Options.Theme = _activeThemePreset.CreateTheme();
            if (_fontsLoaded)
                FontManager.DefaultFont = FontManager.GetFont(_activeThemePreset.FontName);
            UpdateWindowTitle();
        }

        private void UpdateWindowTitle()
        {
            Window.Title = $"MonoGame.PortableUI Demo - {_activeThemePreset.DisplayName}";
        }

        private static string[] GetFontNamesToLoad()
        {
            var fontNames = new List<string>(DemoThemeRegistry.FontNames)
            {
                "Segoe",
                "default",
                "arial"
            };
            return fontNames.ToArray();
        }
    }
}
