using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Demo
{
    public sealed class DemoGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private readonly DemoRunOptions _runOptions;
        private DemoThemePreset _activeThemePreset;
        private bool _fontsLoaded;
        private ScreenEngine? _screenEngine;
        private Texture2D? _whitePixel;

        public DemoGame()
            : this(new DemoRunOptions())
        {
        }

        public DemoGame(DemoThemePreset initialThemePreset)
            : this(new DemoRunOptions { InitialThemePreset = initialThemePreset ?? DemoThemeRegistry.Default })
        {
        }

        public DemoGame(DemoRunOptions runOptions)
        {
            _runOptions = runOptions ?? new DemoRunOptions();
            _activeThemePreset = _runOptions.InitialThemePreset ?? DemoThemeRegistry.Default;
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
            _whitePixel = new Texture2D(GraphicsDevice, 1, 1);
            _whitePixel.SetData(new[] { Color.White });
            ApplyTheme(_activeThemePreset);

            var deleteIcon = Content.Load<Texture2D>("Images/ic_delete");
            if (_runOptions.IsScreenshotMode)
            {
                SaveThemeScreenshots(_runOptions.ScreenshotDirectory!, _runOptions.ScreenshotScreen, deleteIcon, _runOptions.ScreenshotOverlay);
                Exit();
                return;
            }

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

        private void SaveThemeScreenshots(string directory, string screenName, Texture2D deleteIcon, string? overlay)
        {
            Directory.CreateDirectory(directory);
            var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1f / 60));

            var worldSpace = string.Equals(screenName, "worldspace", StringComparison.OrdinalIgnoreCase);
            foreach (var preset in DemoThemeRegistry.Presets)
            {
                // Apply the preset to the primary engine too so controls constructed by MainScreen
                // pick up the right theme defaults.
                ApplyTheme(preset);

                Screen screen = worldSpace
                    ? new WorldSpaceScreen(this, preset)
                    : new MainScreen(deleteIcon, preset, _ => { });
                using var surface = new UISurface(this, screen, 1180, 760, preset.CreateTheme())
                {
                    ShowSoftwareCursor = false,
                    InputSource = PortableUI.Input.NullInputSource.Instance,
                    LayoutScale = _runOptions.LayoutScale
                };
                (screen as MainScreen)?.TrySelectTab(screenName);
                if (overlay != null)
                {
                    // Static capture: no enter animations.
                    surface.Engine.TransitionDuration = TimeSpan.Zero;
                    surface.Engine.Toasts.AnimationDuration = TimeSpan.Zero;
                }
                surface.Update(gameTime);
                if (overlay != null)
                {
                    // After the first update the selected tab's content is attached and laid out.
                    (screen as MainScreen)?.ShowOverlayForScreenshot(overlay);
                    // Let popup open animations finish before capturing.
                    for (var step = 1; step <= 3; step++)
                        surface.Update(new GameTime(gameTime.TotalGameTime + TimeSpan.FromSeconds(step), gameTime.ElapsedGameTime));
                }
                var target = surface.Draw(gameTime);
                GraphicsDevice.SetRenderTarget(null);

                var path = Path.Combine(directory, $"{preset.Id}.png");
                using var stream = File.Create(path);
                target.SaveAsPng(stream, target.Width, target.Height);
            }
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
