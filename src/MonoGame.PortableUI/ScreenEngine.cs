using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Effects;

namespace MonoGame.PortableUI
{
    public class ScreenEngine : IDisposable
    {
        public Game Game { get; set; }
        private static Control? _focusedControl;
        private readonly Dictionary<string, IKeyboard> _keyboards;

        //probably better if it's internal. making it public for a small hack
        public IKeyboard? CurrentKeyboard;

        private ScreenEngine(Game game, ScreenEngineOptions options)
        {
            Game = game;
            Options = options;
            Options.Owner = this;
            ScreenHistory = new Stack<Screen>();
            Component = new ScreenComponent(this, game);
            _keyboards = new Dictionary<string, IKeyboard>();
            ScaleFactor = 1;
#if !ANDROID
            // GameWindow.TextInput is provided by the DesktopGL/WindowsDX backends only. On Android
            // text arrives via the soft keyboard/IME; consumers route it through HandleTextInput.
            game.Window.TextInput += GameWindowTextInput;
#endif
            if (!game.Components.Contains(Component) && options.AddComponentToGame)
                game.Components.Add(Component);
        }

        public static float ScaleFactor { get; set; }
        public ScreenEngineOptions Options { get; }

        /// <summary>
        /// Factor the logical (layout) space is magnified by to fill the real window when
        /// <see cref="ScreenEngineOptions.ReferenceSize"/> is set. 1 means no scaling — layout and
        /// pixels coincide. The <see cref="ScreenComponent"/> uses it to scale the drawn frame and
        /// the active <see cref="Screen"/> uses it to map pointer input back into logical space.
        /// </summary>
        public float RenderScale { get; private set; } = 1;

        /// <summary>
        /// Top-left of the scaled UI within the window, in window pixels. Non-zero when the window's
        /// aspect ratio differs from the reference and the UI is letter-boxed (centred with bars).
        /// The <see cref="ScreenComponent"/> blits the frame here and the active <see cref="Screen"/>
        /// subtracts it before un-scaling pointer input.
        /// </summary>
        public PointF RenderOffset { get; private set; }

        public static Control? FocusedControl
        {
            get { return _focusedControl; }
            set
            {
                if (_focusedControl == value)
                    return;
                var oldElement = _focusedControl;
                _focusedControl = value;
                oldElement?.OnLostFocus(new LostFocusEventArgs(_focusedControl));
                _focusedControl?.OnGotFocus(new GotFocusEventArgs(oldElement));
            }
        }

        public Rect ScreenRect { get; set; }

        internal ScreenComponent Component { get; }

        private BackdropManager? _backdrop;
        private PostProcessManager? _postProcess;

        /// <summary>
        /// Backdrop-blur pipeline shared by all screens of this engine (created on first use).
        /// Recreated automatically if the game's GraphicsDevice is replaced
        /// (e.g. an Android activity restart / device reset), so its render targets never
        /// reference a disposed device.
        /// </summary>
        public BackdropManager Backdrop
        {
            get
            {
                var device = Game.GraphicsDevice;
                if (_backdrop == null || !ReferenceEquals(_backdrop.GraphicsDevice, device))
                {
                    _backdrop?.Dispose();
                    _backdrop = new BackdropManager(device);
                }

                return _backdrop;
            }
        }

        /// <summary>
        /// Post-process chain runner shared by all screens of this engine (created on first use).
        /// Recreated automatically when the game's GraphicsDevice is replaced.
        /// </summary>
        public PostProcessManager PostProcess
        {
            get
            {
                var device = Game.GraphicsDevice;
                if (_postProcess == null || !ReferenceEquals(_postProcess.GraphicsDevice, device))
                {
                    _postProcess?.Dispose();
                    _postProcess = new PostProcessManager(device);
                }

                return _postProcess;
            }
        }

        public bool DebugOverlayEnabled { get; private set; }

        public double FramesPerSecond { get; private set; }

        public int BatchFlushesThisFrame { get; private set; }

        public int LayoutPassesThisFrame { get; private set; }

        public static DrawableGameComponent ScreenComponent
        {
            get
            {
                if (Instance == null)
                    throw new TypeInitializationException("ScreenEngine", new ArgumentNullException());
                return Instance.Component;
            }
        }

        public static ScreenEngine? Instance { get; private set; }

        public static ScreenEngine Initialize(Game game)
        {
            return Initialize(game, new ScreenEngineOptions());
        }

        public static ScreenEngine Initialize(Game game, bool addComponent)
        {
            return Initialize(game, new ScreenEngineOptions { AddComponentToGame = addComponent });
        }

        public static ScreenEngine Initialize(Game game, ScreenEngineOptions options)
        {
            // A fresh Initialize means a new Game/activity: drop any focus captured by a previous
            // engine instance so a disposed control from the old activity is never left focused.
            _focusedControl = null;
            Instance = new ScreenEngine(game, options ?? new ScreenEngineOptions());
            return Instance;
        }

        public static ScreenEngine CreateSurfaceEngine(Game game, ScreenEngineOptions options)
        {
            return new ScreenEngine(game, options ?? new ScreenEngineOptions());
        }

        public void RegisterKeyboard(IKeyboard keyboard, string? inputScope = "default")
        {
            _keyboards[inputScope ?? "default"] = keyboard;
        }

        public void UnregisterKeyboard(string? inputScope = "default")
        {
            inputScope = inputScope ?? "default";
            if (_keyboards.ContainsKey(inputScope))
                _keyboards.Remove(inputScope);
        }

        //probably better if it's internal. making it public for a small hack
        public void RequestKeyboard(string? inputScope)
        {
            inputScope = inputScope ?? "default";
            if (!_keyboards.TryGetValue(inputScope, out var keyboard))
                return;
            CurrentKeyboard = keyboard;
            CurrentKeyboard?.Control.UpdateLayout(new Rect(0, ScreenRect.Height - CurrentKeyboard.Height, ScreenRect.Width, CurrentKeyboard.Height));
            ActiveScreen?.ShowKeyboard();
            CurrentKeyboard?.OnKeyboardAppear();
        }

        //probably better if it's internal. making it public for a small hack
        public void HideKeyboard()
        {
            if (CurrentKeyboard == null)
                return;
            ActiveScreen?.HideKeyboard();
            CurrentKeyboard.OnKeyboardDisappear();
            CurrentKeyboard = null;
        }

        public void SetScreenSize(int width, int height)
        {
            ScreenRect = new Rect(width, height);
            ActiveScreen?.InvalidateLayout(true);
        }

        internal bool ApplyViewportSize(int width, int height)
        {
            if (Options.ScreenSizeMode != ScreenSizeMode.Viewport)
                return false;

            float scale;
            float logicalWidth, logicalHeight;
            PointF offset;

            var reference = Options.ReferenceSize;
            if (reference.X > 0 && reference.Y > 0 && width > 0 && height > 0)
            {
                // Letter-box: lay out at exactly the reference resolution and scale that uniformly to
                // fit the window (tighter axis wins), then centre it. Surplus window space becomes
                // bars rather than extra logical room, so the UI never distorts on odd aspects.
                scale = Math.Min(width / reference.X, height / reference.Y);
                logicalWidth = reference.X;
                logicalHeight = reference.Y;
                offset = new PointF((width - reference.X * scale) / 2f, (height - reference.Y * scale) / 2f);
            }
            else
            {
                // No reference set: layout maps 1:1 to the viewport, exactly as before.
                scale = 1;
                logicalWidth = width;
                logicalHeight = height;
                offset = new PointF(0, 0);
            }

            if (RenderScale == scale && RenderOffset.X == offset.X && RenderOffset.Y == offset.Y
                && ScreenRect.Width == logicalWidth && ScreenRect.Height == logicalHeight)
                return false;

            RenderScale = scale;
            RenderOffset = offset;
            ScreenRect = new Rect(logicalWidth, logicalHeight);
            ActiveScreen?.InvalidateLayout(true);
            return true;
        }

        public Stack<Screen> ScreenHistory { get; }
        public Screen? ActiveScreen => ScreenHistory.Count > 0 ? ScreenHistory.Peek() : null;

        public void NavigateToScreen<T>(T screen) where T : Screen
        {
            FocusedControl = null;
            screen.ScreenEngine = this;
            ScreenHistory.Push(screen);
        }

        public void NavigateBack()
        {
            if (ScreenHistory.Count == 0)
                return;
            FocusedControl = null;
            var screen = ScreenHistory.Pop();
            screen.ScreenEngine = null;
        }

        public void Update(GameTime gameTime)
        {
            ScreenSystem.TotalTime = gameTime.TotalGameTime;
            BatchFlushesThisFrame = 0;
            LayoutPassesThisFrame = 0;
            FramesPerSecond = gameTime.ElapsedGameTime.TotalSeconds > 0 ? 1 / gameTime.ElapsedGameTime.TotalSeconds : 0;
            ActiveScreen?.Update();
        }

        public void ToggleDebugOverlay()
        {
            DebugOverlayEnabled = !DebugOverlayEnabled;
        }

        internal void RecordBatchFlush()
        {
            BatchFlushesThisFrame++;
        }

        internal void RecordLayoutPass()
        {
            LayoutPassesThisFrame++;
        }

#if !ANDROID
        private void GameWindowTextInput(object? sender, TextInputEventArgs args)
        {
            ActiveScreen?.HandleTextInput(args.Character);
        }
#endif

        /// <summary>
        /// Routes a typed character into the active screen's focused control. Desktop backends call this
        /// from the game window's TextInput event; on Android the host activity/soft keyboard calls it directly.
        /// </summary>
        public void HandleTextInput(char character)
        {
            ActiveScreen?.HandleTextInput(character);
        }

        private bool _disposed;

        /// <summary>
        /// Unsubscribes from the game window's TextInput event and disposes the backdrop/post-process
        /// pipelines. Required for surface engines created via <see cref="CreateSurfaceEngine"/> (e.g.
        /// one per <see cref="UISurface"/>) so discarding a surface doesn't leak a handler on the shared
        /// game window or its GPU render targets.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
#if !ANDROID
            Game.Window.TextInput -= GameWindowTextInput;
#endif
            if (Options.AddComponentToGame && Game.Components.Contains(Component))
                Game.Components.Remove(Component);
            _backdrop?.Dispose();
            _postProcess?.Dispose();
        }
    }
}
