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
            if (_runOptions.BenchmarkFile != null)
            {
                BenchmarkThemes(_runOptions.BenchmarkFile, deleteIcon);
                Exit();
                return;
            }

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

        /// <summary>
        ///     --benchmark-themes out.csv: renders the controls tab of each theme offscreen (1180×760)
        ///     and reports the mean frame time (CPU + GPU: each frame waits for the GPU) and the
        ///     real draw calls per frame. Optional --screenshot-themes limits the themes,
        ///     --screenshot-overlay modal measures with a dialog open.
        /// </summary>
        private void BenchmarkThemes(string file, Texture2D deleteIcon)
        {
            const int warmup = 20;
            const int frames = 120;
            var lines = new List<string> { "theme,ms_per_frame,draw_calls,sprites,batch_flushes" };
            var probe = new Color[1];
            foreach (var preset in DemoThemeRegistry.Presets)
            {
                if (_runOptions.ScreenshotThemes is { Length: > 0 } only && Array.IndexOf(only, preset.Id) < 0)
                    continue;
                ApplyTheme(preset);
                var screen = new MainScreen(deleteIcon, preset, _ => { });
                using var surface = new UISurface(this, screen, 1180, 760, preset.CreateTheme())
                {
                    ShowSoftwareCursor = false,
                    InputSource = PortableUI.Input.NullInputSource.Instance
                };
                screen.TrySelectTab("controls");
                var time = TimeSpan.FromSeconds(1);
                var step = TimeSpan.FromMilliseconds(16);
                RenderTarget2D? target = null;
                for (var i = 0; i < warmup; i++)
                {
                    time += step;
                    surface.Update(new GameTime(time, step));
                    if (i == 1 && _runOptions.ScreenshotOverlay != null)
                        screen.ShowOverlayForScreenshot(_runOptions.ScreenshotOverlay);
                    target = surface.Draw(new GameTime(time, step));
                }
                target!.GetData(0, new Rectangle(0, 0, 1, 1), probe, 0, 1);

                var before = GraphicsDevice.Metrics;
                var flushes = 0L;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                for (var i = 0; i < frames; i++)
                {
                    time += step;
                    surface.Update(new GameTime(time, step));
                    target = surface.Draw(new GameTime(time, step));
                    flushes += surface.Engine.BatchFlushesThisFrame;
                    // Wait for the GPU so the time covers the actual rendering.
                    target.GetData(0, new Rectangle(0, 0, 1, 1), probe, 0, 1);
                }
                watch.Stop();
                var after = GraphicsDevice.Metrics;
                GraphicsDevice.SetRenderTarget(null);
                var line = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1:0.00},{2},{3},{4}",
                    preset.Id, watch.Elapsed.TotalMilliseconds / frames, (after.DrawCount - before.DrawCount) / frames,
                    (after.SpriteCount - before.SpriteCount) / frames, flushes / frames);
                lines.Add(line);
                Console.WriteLine(line);
            }
            File.WriteAllLines(file, lines);
        }

        private void SaveThemeScreenshots(string directory, string screenName, Texture2D deleteIcon, string? overlay)
        {
            Directory.CreateDirectory(directory);
            var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1f / 60));

            var worldSpace = string.Equals(screenName, "worldspace", StringComparison.OrdinalIgnoreCase);
            foreach (var preset in DemoThemeRegistry.Presets)
            {
                if (_runOptions.ScreenshotThemes is { Length: > 0 } only && Array.IndexOf(only, preset.Id) < 0)
                    continue;
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
                if (_runOptions.ScreenshotCursor is { } cursorAt)
                {
                    // Capture the theme's software pointer at a fixed spot.
                    var pointer = new PortableUI.Input.VirtualInputSource();
                    pointer.SetPointer(cursorAt);
                    surface.InputSource = pointer;
                    surface.ShowSoftwareCursor = true;
                }
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
                if (_runOptions.ScreenshotTabs > 0)
                {
                    // Keyboard navigation: Tab n times so a focus ring shows in the capture.
                    var keys = surface.InputSource as PortableUI.Input.VirtualInputSource ?? new PortableUI.Input.VirtualInputSource();
                    surface.InputSource = keys;
                    for (var i = 0; i < _runOptions.ScreenshotTabs; i++)
                    {
                        keys.SetKeyboardState(new Microsoft.Xna.Framework.Input.KeyboardState(Microsoft.Xna.Framework.Input.Keys.Tab));
                        surface.Update(gameTime);
                        keys.SetKeyboardState(new Microsoft.Xna.Framework.Input.KeyboardState());
                        surface.Update(gameTime);
                    }
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
