using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Audio;

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
            if (_runOptions.SurfaceBenchmarkFile != null)
            {
                SurfaceBenchmark.Run(this, _runOptions.SurfaceBenchmarkFile, _runOptions.SurfaceCount);
                Exit();
                return;
            }

            if (_runOptions.BenchmarkFile != null)
            {
                BenchmarkThemes(_runOptions.BenchmarkFile, deleteIcon);
                Exit();
                return;
            }

            if (_runOptions.GarbageBenchmarkFile != null)
            {
                BenchmarkGarbage(_runOptions.GarbageBenchmarkFile, deleteIcon);
                Exit();
                return;
            }

            if (_runOptions.MotionRecordDirectory != null)
            {
                RecordMotion(_runOptions.MotionRecordDirectory, deleteIcon);
                Exit();
                return;
            }

            if (_runOptions.IsScreenshotMode)
            {
                SaveThemeScreenshots(_runOptions.ScreenshotDirectory!, _runOptions.ScreenshotScreen, deleteIcon, _runOptions.ScreenshotOverlay);
                Exit();
                return;
            }

            if (_runOptions.SoundTest)
            {
                RunSoundTest();
                Exit();
                return;
            }

            // UI sounds in each theme's own set (soft clicks, glass pings, terminal clacks, 8-bit bleeps).
            if (_screenEngine != null)
            {
                _screenEngine.Sounds.UseThemeSounds = !_runOptions.Mute;
                // A gamepad rumbles on clicks, toggles and focus moves made with it.
                _screenEngine.Haptics.Players.Add(new MonoGame.PortableUI.Haptics.GamePadRumbleHaptics());
            }

            _screenEngine?.NavigateToScreen(new MainScreen(deleteIcon, _activeThemePreset, ApplyTheme));
        }

        private void RunSoundTest()
        {
            foreach (var style in new[] { UiSynthStyle.Soft, UiSynthStyle.Retro, UiSynthStyle.Glass, UiSynthStyle.Mechanical })
            {
                using var player = SoundEffectUiSoundPlayer.CreateDefault(style);
                if (player == null)
                {
                    Console.WriteLine($"sound-test {style}: no audio device");
                    continue;
                }

                var count = 0;
                for (var i = 0; i <= (int)UiSound.Type; i++)
                {
                    if (player[(UiSound)i] == null)
                        continue;
                    player.Play((UiSound)i, 0.5f, 0, 0);
                    count++;
                    System.Threading.Thread.Sleep(150);
                }

                Console.WriteLine($"sound-test {style}: played {count} sounds");
            }

            var pad = Microsoft.Xna.Framework.Input.GamePad.GetState(PlayerIndex.One);
            Console.WriteLine($"gamepad: {(pad.IsConnected ? "connected, rumbling a click and an error" : "none connected")}");
            if (pad.IsConnected)
            {
                var rumble = new MonoGame.PortableUI.Haptics.GamePadRumbleHaptics();
                foreach (var haptic in new[] { MonoGame.PortableUI.Haptics.UiHaptic.Click, MonoGame.PortableUI.Haptics.UiHaptic.Double })
                {
                    var start = DateTime.UtcNow;
                    var origin = MonoGame.PortableUI.Common.ScreenSystem.TotalTime;
                    rumble.Play(haptic, 1);
                    while (rumble.IsActive)
                    {
                        System.Threading.Thread.Sleep(5);
                        rumble.Update(origin + (DateTime.UtcNow - start));
                    }
                    System.Threading.Thread.Sleep(400);
                }
            }
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
            {
                _screenEngine.Options.Theme = _activeThemePreset.CreateTheme();
            }
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

        private static readonly string[] DemoTabs = ["Gallery", "Inspector", "Controls", "Visual FX", "Motion", "Layout", "Drag & drop", "Scroll", "Data grid", "Stress"];

        /// <summary>
        ///     --benchmark-garbage out.csv: managed bytes allocated per frame (Update + Draw) on the game thread, for every tab
        ///     of the first theme and the controls tab of every theme (--screenshot-themes limits them; --garbage-all-tabs
        ///     measures every tab of every theme). Two phases after a warm-up that lets the JIT optimise: the pointer resting
        ///     on the surface, then circling over it. A game runs for hours, so anything above zero adds up to collections.
        /// </summary>
        private void BenchmarkGarbage(string file, Texture2D deleteIcon)
        {
            const int frames = 600;
            var allTabs = Array.IndexOf(Environment.GetCommandLineArgs(), "--garbage-all-tabs") >= 0;
            // --garbage-frames: also print each measured frame that allocated, to tell a one-off event from a per-frame cost.
            var logFrames = Array.IndexOf(Environment.GetCommandLineArgs(), "--garbage-frames") >= 0;
            var lines = new List<string> { "theme,tab,bytes_per_frame_resting,bytes_per_frame_moving,layout_passes_per_100_frames" };
            var first = true;
            foreach (var preset in DemoThemeRegistry.Presets)
            {
                if (_runOptions.ScreenshotThemes is { Length: > 0 } only && Array.IndexOf(only, preset.Id) < 0)
                    continue;
                foreach (var tab in DemoTabs)
                {
                    if (!first && !allTabs && tab != "Controls")
                        continue;
                    ApplyTheme(preset);
                    var pointer = new PortableUI.Input.VirtualInputSource();
                    var screen = new MainScreen(deleteIcon, preset, _ => { });
                    using var surface = new UISurface(this, screen, 1180, 760, preset.CreateTheme())
                    {
                        ShowSoftwareCursor = true,
                        InputSource = pointer
                    };
                    screen.TrySelectTab(tab);
                    // --garbage-no-tooltips: tooltips never open, to tell their show/hide events from the per-frame cost.
                    if (Array.IndexOf(Environment.GetCommandLineArgs(), "--garbage-no-tooltips") >= 0)
                        surface.Engine.Options.ToolTipHoverDelay = TimeSpan.FromDays(1);
                    var time = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(16));
                    var angle = 0f;
                    var layoutPasses = 0L;
                    void Frame(bool moving)
                    {
                        time.TotalGameTime += time.ElapsedGameTime;
                        if (moving)
                        {
                            angle += .05f;
                            pointer.SetPointer(new PortableUI.Common.PointF(590 + MathF.Cos(angle) * 450, 380 + MathF.Sin(angle * 1.3f) * 300));
                        }
                        surface.Update(time);
                        layoutPasses += surface.Engine.LayoutPassesThisFrame;
                        surface.Draw(time);
                    }
                    pointer.SetPointer(new PortableUI.Common.PointF(590, 380));
                    // Warm up both phases; the JIT optimises hot methods in the background after their first calls.
                    for (var round = 0; round < 3; round++)
                    {
                        for (var i = 0; i < 150; i++) Frame(i % 2 == 0);
                        System.Threading.Thread.Sleep(150);
                    }
                    pointer.SetPointer(new PortableUI.Common.PointF(590, 380));
                    for (var i = 0; i < 30; i++) Frame(false);
                    layoutPasses = 0;
                    long Measure(bool moving)
                    {
                        var first = GC.GetAllocatedBytesForCurrentThread();
                        for (var i = 0; i < frames; i++)
                        {
                            var before = GC.GetAllocatedBytesForCurrentThread();
                            Frame(moving);
                            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                            if (logFrames && bytes > 0)
                                Console.WriteLine($"  {preset.Id} {tab} {(moving ? "moving" : "resting")} frame {i}: {bytes} bytes");
                        }
                        return GC.GetAllocatedBytesForCurrentThread() - first;
                    }
                    var resting = Measure(false) / (double)frames;
                    var moving = Measure(true) / (double)frames;
                    GraphicsDevice.SetRenderTarget(null);
                    var line = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2:0},{3:0},{4:0}", preset.Id, tab, resting, moving, layoutPasses * 100.0 / (2 * frames));
                    lines.Add(line);
                    Console.WriteLine(line);
                }
                first = false;
            }
            File.WriteAllLines(file, lines);
        }

        /// <summary>
        ///     --record-motion dir: plays the Motion tab's demos on a fixed timeline and saves every frame
        ///     (30 fps, deterministic clock) as frame_0000.png... - turn them into a GIF or video afterwards.
        /// </summary>
        private void RecordMotion(string directory, Texture2D deleteIcon)
        {
            Directory.CreateDirectory(directory);
            var preset = _activeThemePreset;
            ApplyTheme(preset);
            var screen = new MainScreen(deleteIcon, preset, _ => { });
            using var surface = new UISurface(this, screen, 1180, 760, preset.CreateTheme())
            {
                ShowSoftwareCursor = false,
                InputSource = PortableUI.Input.NullInputSource.Instance
            };
            screen.TrySelectTab("motion");
            if (string.Equals(_runOptions.MotionRecordTimeline, "transitions", StringComparison.OrdinalIgnoreCase))
            {
                RecordTransitions(directory, screen, surface);
                return;
            }

            (double At, string Motion)[] timeline =
            [
                (0.3, "throw"), (0.5, "pop"), (1.2, "jelly"), (2.0, "shake"), (2.6, "hop"), (2.8, "spring"),
                (3.7, "throw"), (4.3, "jelly"), (4.5, "pop"), (5.0, "spring"), (5.2, "hop"),
                (6.2, "loop"), (6.6, "stagger"), (8.4, "loop")
            ];
            const int fps = 30;
            const double seconds = 9.6;
            var step = TimeSpan.FromSeconds(1.0 / fps);
            var time = TimeSpan.FromSeconds(1);
            for (var i = 0; i < 3; i++)
            {
                time += step;
                surface.Update(new GameTime(time, step));
            }

            var start = time;
            var next = 0;
            for (var frame = 0; frame < seconds * fps; frame++)
            {
                var elapsed = (time - start).TotalSeconds;
                while (next < timeline.Length && timeline[next].At <= elapsed)
                    screen.PlayMotion(timeline[next++].Motion);
                time += step;
                surface.Update(new GameTime(time, step));
                var target = surface.Draw(new GameTime(time, step));
                GraphicsDevice.SetRenderTarget(null);
                using var stream = File.Create(Path.Combine(directory, $"frame_{frame:D4}.png"));
                target.SaveAsPng(stream, target.Width, target.Height);
            }
        }

        /// <summary>
        ///     --record-timeline transitions: a scripted pointer hovers and clicks the transition demo, so the
        ///     theme's hover fades and the retargeting springs show in the frames.
        /// </summary>
        private void RecordTransitions(string directory, MainScreen screen, UISurface surface)
        {
            var input = new PortableUI.Input.VirtualInputSource();
            surface.InputSource = input;
            surface.ShowSoftwareCursor = true;

            // (time, target, offset from its centre, left button down)
            (double At, string Target, Vector2 Offset, bool Down)[] path =
            [
                (0.0, "set", new Vector2(-40, -150), false),
                (0.7, "set", Vector2.Zero, false),
                (1.0, "set", Vector2.Zero, true), (1.1, "set", Vector2.Zero, false),
                (2.0, "set", Vector2.Zero, true), (2.1, "set", Vector2.Zero, false),
                (2.4, "set", Vector2.Zero, true), (2.5, "set", Vector2.Zero, false),
                (3.4, "set", Vector2.Zero, true), (3.5, "set", Vector2.Zero, false),
                (4.4, "hoverA", Vector2.Zero, false),
                (4.9, "hoverA", Vector2.Zero, false),
                (5.3, "hoverB", Vector2.Zero, false),
                (5.8, "hoverB", Vector2.Zero, false),
                (6.3, "set", Vector2.Zero, false),
                (6.5, "set", Vector2.Zero, true), (6.6, "set", Vector2.Zero, false),
                (7.4, "set", new Vector2(-40, -150), false),
                (8.2, "set", new Vector2(-40, -150), false)
            ];
            const int fps = 30;
            var step = TimeSpan.FromSeconds(1.0 / fps);
            var time = TimeSpan.FromSeconds(1);
            for (var i = 0; i < 3; i++)
            {
                time += step;
                surface.Update(new GameTime(time, step));
            }

            Vector2 PointAt(int index)
            {
                var rect = screen.MotionTargetRect(path[index].Target) ?? default;
                return new Vector2(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2) + path[index].Offset;
            }

            var start = time;
            var seconds = path[^1].At;
            for (var frame = 0; frame < seconds * fps; frame++)
            {
                var elapsed = (time - start).TotalSeconds;
                var next = 1;
                while (next < path.Length - 1 && path[next].At <= elapsed)
                    next++;
                var previous = next - 1;
                var span = path[next].At - path[previous].At;
                var t = span <= 0 ? 1 : Math.Clamp((elapsed - path[previous].At) / span, 0, 1);
                // Ease the pointer like a hand: slow start, slow stop.
                t = t * t * (3 - 2 * t);
                var point = Vector2.Lerp(PointAt(previous), PointAt(next), (float)t);
                input.SetPointer(new PortableUI.Common.PointF(point.X, point.Y), leftDown: path[previous].Down);

                time += step;
                surface.Update(new GameTime(time, step));
                var target = surface.Draw(new GameTime(time, step));
                GraphicsDevice.SetRenderTarget(null);
                using var stream = File.Create(Path.Combine(directory, $"frame_{frame:D4}.png"));
                target.SaveAsPng(stream, target.Width, target.Height);
            }
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
