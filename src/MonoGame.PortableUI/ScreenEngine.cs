using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Effects;

namespace MonoGame.PortableUI
{
    public partial class ScreenEngine : IDisposable
    {
        public Game Game { get; set; }
        private Control? _focusedControl;
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

        /// <summary>
        ///     Sets the native render scale of a manually sized engine (e.g. a <see cref="UISurface"/>):
        ///     layout stays in <see cref="ScreenRect"/> units, drawing is scaled by <paramref name="scale"/>.
        /// </summary>
        internal void SetNativeRenderScale(float scale)
        {
            scale = scale > 0 ? scale : 1f;
            if (RenderScale == scale)
                return;
            RenderScale = scale;
            RenderOffset = new PointF(0, 0);
            foreach (var screen in ScreenHistory)
                screen.InvalidateLayout(true);
        }

        /// <summary>True when <see cref="RenderScale"/> is applied by the draw transform (LayoutScale)
        /// rather than by blitting a reference-size render target.</summary>
        internal bool ScalesNatively => RenderScale != 1 && !(Options.ReferenceSize.X > 0 && Options.ReferenceSize.Y > 0);

        /// <summary>
        ///     The control with keyboard/gamepad focus in this engine. Each engine (a window, a
        ///     <see cref="UISurface"/>, one player's computer in a game) has its own focus.
        /// </summary>
        public Control? FocusedControl
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

        /// <summary>The engine that owns <paramref name="control"/>'s focus: the one hosting its screen,
        /// or <see cref="Instance"/> for a control that is not attached to a screen yet.</summary>
        public static ScreenEngine? For(Control? control) => control?.Screen?.ScreenEngine ?? Instance;

        public Rect ScreenRect { get; set; }

        private readonly object _insetLock = new object();
        private Thickness _pendingSystemInsets;
        private float _pendingKeyboardInset;
        private bool _insetsPending;
        private Thickness _systemInsetPixels;
        private float _keyboardInsetPixels;

        /// <summary>
        ///     The part of each screen edge covered by notches, rounded corners, system bars or the
        ///     on-screen keyboard (bottom), in layout units. Zero on desktop.
        ///     <see cref="Controls.SafeAreaPanel"/> pads itself by it.
        /// </summary>
        public Thickness SafeAreaInsets { get; private set; }

        /// <summary>Height of the on-screen keyboard in layout units (0 when hidden).</summary>
        public float KeyboardInset { get; private set; }

        private Input.IOnScreenKeyboard? _onScreenKeyboard;

        /// <summary>
        ///     The platform software keyboard (from <see cref="ScreenEngineOptions.OnScreenKeyboard"/>).
        ///     Replaceable at runtime, e.g. to register Steam's keyboard once the Steam API is up.
        /// </summary>
        public Input.IOnScreenKeyboard OnScreenKeyboard
        {
            get => _onScreenKeyboard ??= Attach(Options.OnScreenKeyboard);
            set
            {
                if (ReferenceEquals(_onScreenKeyboard, value))
                    return;
                if (_onScreenKeyboard != null)
                    _onScreenKeyboard.VisibilityChanged -= OnScreenKeyboardVisibilityChanged;
                _onScreenKeyboard = Attach(value ?? Input.NullOnScreenKeyboard.Instance);
            }
        }

        private Input.IOnScreenKeyboard Attach(Input.IOnScreenKeyboard keyboard)
        {
            keyboard.VisibilityChanged += OnScreenKeyboardVisibilityChanged;
            return keyboard;
        }

        private void OnScreenKeyboardVisibilityChanged(object? sender, Input.OnScreenKeyboardEventArgs args)
        {
            // A keyboard that reports no height (floating, or Android where the insets listener
            // delivers the real IME height) only clears the inset when it hides.
            if (!args.IsVisible)
                SetKeyboardInset(0);
            else if (args.CoveredHeightPixels > 0)
                SetKeyboardInset(args.CoveredHeightPixels);
        }

        private readonly Accessibility.AccessibilityService _accessibilityService = new Accessibility.AccessibilityService();

        /// <summary>
        ///     The platform screen-reader bridge (null: none). The tree is only built while
        ///     <see cref="Accessibility.IAccessibilityBridge.IsScreenReaderActive"/> is true.
        /// </summary>
        public Accessibility.IAccessibilityBridge? AccessibilityBridge { get; set; }

        /// <summary>The last accessibility snapshot of the active screen (empty while no reader is active).</summary>
        public IReadOnlyList<Accessibility.AccessibilityNode> AccessibilityNodes => _accessibilityService.Nodes;

        /// <summary>Converts a rectangle in layout units to window pixels (scale and letter-box offset).</summary>
        public Rect LayoutToWindow(Rect rect)
            => new Rect(rect.Left * RenderScale + RenderOffset.X, rect.Top * RenderScale + RenderOffset.Y, rect.Width * RenderScale, rect.Height * RenderScale);

        /// <summary>Converts a window pixel position to layout units.</summary>
        public PointF WindowToLayout(PointF point)
            => new PointF((point.X - RenderOffset.X) / RenderScale, (point.Y - RenderOffset.Y) / RenderScale);

        private readonly System.Collections.Concurrent.ConcurrentQueue<Action> _gameThreadQueue = new System.Collections.Concurrent.ConcurrentQueue<Action>();

        /// <summary>
        ///     Runs <paramref name="action"/> on the game thread at the start of the next update. For
        ///     platform callbacks (screen-reader actions, IME events) that arrive on the UI thread.
        /// </summary>
        public void InvokeOnGameThread(Action action)
        {
            if (action == null)
                return;
            _gameThreadQueue.Enqueue(action);
            WakeUp();
        }

        private void DrainGameThreadQueue()
        {
            while (_gameThreadQueue.TryDequeue(out var action))
            {
                action();
                RequestRedraw();
            }
        }

        private void UpdateAccessibility()
        {
            var bridge = AccessibilityBridge;
            if (bridge == null || !bridge.IsScreenReaderActive)
            {
                if (_accessibilityService.Nodes.Count > 0)
                    _accessibilityService.Reset();
                return;
            }
            _accessibilityService.Update(ActiveScreen, bridge);
        }

        /// <summary>Raised on the game thread after <see cref="SafeAreaInsets"/> changed.</summary>
        public event EventHandler? SafeAreaChanged;

        /// <summary>
        ///     Reports the system insets (cutouts, status/navigation bars) in window pixels. Safe to call
        ///     from any thread (e.g. an Android insets listener); applied on the next update.
        /// </summary>
        public void SetSystemInsets(Thickness windowPixels)
        {
            lock (_insetLock)
            {
                _pendingSystemInsets = windowPixels;
                _pendingKeyboardInset = _insetsPending ? _pendingKeyboardInset : _keyboardInsetPixels;
                _insetsPending = true;
            }
        }

        /// <summary>Reports the on-screen keyboard height in window pixels (0 = hidden). Any thread.</summary>
        public void SetKeyboardInset(float windowPixels)
        {
            lock (_insetLock)
            {
                _pendingSystemInsets = _insetsPending ? _pendingSystemInsets : _systemInsetPixels;
                _pendingKeyboardInset = Math.Max(0, windowPixels);
                _insetsPending = true;
            }
        }

        private void ApplyPendingInsets(bool force)
        {
            lock (_insetLock)
            {
                if (_insetsPending)
                {
                    _systemInsetPixels = _pendingSystemInsets;
                    _keyboardInsetPixels = _pendingKeyboardInset;
                    _insetsPending = false;
                    force = true;
                }
            }
            if (!force)
                return;

            // Window pixels -> layout units: the letter-box bars already keep the UI clear of
            // whatever they cover, so only the part reaching into the scaled area counts.
            var scale = RenderScale > 0 ? RenderScale : 1f;
            var keyboard = Math.Max(0, _keyboardInsetPixels - RenderOffset.Y) / scale;
            var insets = new Thickness(
                Math.Max(0, _systemInsetPixels.Left - RenderOffset.X) / scale,
                Math.Max(0, _systemInsetPixels.Top - RenderOffset.Y) / scale,
                Math.Max(0, _systemInsetPixels.Right - RenderOffset.X) / scale,
                Math.Max(Math.Max(0, _systemInsetPixels.Bottom - RenderOffset.Y) / scale, keyboard));

            if (insets.Equals(SafeAreaInsets) && keyboard == KeyboardInset)
                return;
            RequestRedraw();
            var keyboardGrew = keyboard > KeyboardInset;
            SafeAreaInsets = insets;
            KeyboardInset = keyboard;
            foreach (var screen in ScreenHistory)
                screen.InvalidateLayout(true);
            // Keep the field being typed into visible above the keyboard.
            if (keyboardGrew)
                ActiveScreen?.RequestBringFocusIntoView();
            SafeAreaChanged?.Invoke(this, EventArgs.Empty);
        }

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

        private ClipLayerPool? _clipLayers;

        /// <summary>Offscreen layers for non-rectangular clips, bound to <paramref name="device"/>.</summary>
        internal ClipLayerPool GetClipLayers(Microsoft.Xna.Framework.Graphics.GraphicsDevice device)
        {
            if (_clipLayers == null || !ReferenceEquals(_clipLayers.GraphicsDevice, device))
            {
                _clipLayers?.Dispose();
                _clipLayers = new ClipLayerPool(device);
            }
            return _clipLayers;
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
            else if (Options.LayoutScale > 0 && Math.Abs(Options.LayoutScale - 1) > 0.0001f && width > 0 && height > 0)
            {
                // Density scaling: lay out in dp, draw natively through the root transform.
                scale = Options.LayoutScale;
                logicalWidth = width / scale;
                logicalHeight = height / scale;
                offset = new PointF(0, 0);
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
            ApplyPendingInsets(force: true);
            return true;
        }

        public Stack<Screen> ScreenHistory { get; }
        public Screen? ActiveScreen => ScreenHistory.Count > 0 ? ScreenHistory.Peek() : null;

        public void NavigateToScreen<T>(T screen) where T : Screen
        {
            Push(screen, isOverlay: false, ScreenTransition.None);
        }

        /// <summary>Replaces the visible screen with <paramref name="screen"/>, animated.</summary>
        public void NavigateToScreen(Screen screen, ScreenTransition transition)
        {
            Push(screen, isOverlay: false, transition);
        }

        /// <summary>
        ///     Pushes <paramref name="screen"/> on top of the current one without hiding it (pause
        ///     menu over the HUD, dialogs). The screens below keep being drawn but are frozen: only
        ///     the top screen updates and receives input. Close it with <see cref="NavigateBack()"/>.
        /// </summary>
        public void PushOverlay(Screen screen, ScreenTransition transition = ScreenTransition.Fade)
        {
            Push(screen, isOverlay: true, transition);
        }

        private ToastService? _toasts;

        /// <summary>Self-dismissing messages shown above every screen (see <see cref="ToastService"/>).</summary>
        public ToastService Toasts => _toasts ??= new ToastService(this);

        internal ToastService? ToastsIfCreated => _toasts;

        /// <summary>Draws the visible screen stack bottom to top (overlays over what they cover), a
        /// screen still playing its exit transition, then toasts on top of everything.</summary>
        internal void DrawStack(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            var previous = EnterDraw();
            try
            {
                var screens = VisibleScreens;
                var start = DrawCoveredScreensForGlass(spriteBatch, screens);
                for (var i = start; i < screens.Count; i++)
                    screens[i].Draw(spriteBatch);
                for (var i = 0; i < screens.Count; i++)
                    screens[i].StackBackdrop = null;
                LeavingScreen?.Draw(spriteBatch);
                if (_toasts is { HasContent: true } toasts)
                    toasts.Layer.Draw(spriteBatch);
            }
            finally
            {
                ExitDraw(previous);
            }
        }

        private RenderTarget2D? _stackBackdropTarget;
        private RenderTargetBinding[]? _stackPreviousTargets;

        /// <summary>
        ///     Nested glass: when an overlay (modal, sheet) higher in the stack has glass, the screens
        ///     below it are rendered into an offscreen picture first, shown as usual, and handed to the
        ///     overlays as their backdrop — so the overlay's glass blurs/refracts the real UI under it,
        ///     not just the wallpaper. Returns the index of the first screen still to draw.
        /// </summary>
        private int DrawCoveredScreensForGlass(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, IReadOnlyList<Screen> screens)
        {
            var first = -1;
            for (var i = 1; i < screens.Count; i++)
            {
                if (screens[i].IsOverlay && screens[i].RequiresBackdropNow)
                {
                    first = i;
                    break;
                }
            }
            if (first < 0 || EffectiveRenderQuality == RenderQuality.Low)
                return 0;

            var device = spriteBatch.GraphicsDevice;
            var width = device.Viewport.Width;
            var height = device.Viewport.Height;
            if (_stackBackdropTarget == null || _stackBackdropTarget.IsDisposed || _stackBackdropTarget.Width != width || _stackBackdropTarget.Height != height)
            {
                _stackBackdropTarget?.Dispose();
                _stackBackdropTarget = new RenderTarget2D(device, Math.Max(1, width), Math.Max(1, height), false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }

            var previousTargets = Effects.RenderTargetHelper.SnapshotRenderTargets(device, ref _stackPreviousTargets);
            device.SetRenderTarget(_stackBackdropTarget);
            device.Clear(Color.Transparent);
            for (var i = 0; i < first; i++)
                screens[i].Draw(spriteBatch);
            if (previousTargets.Length == 0)
                device.SetRenderTarget(null);
            else
                device.SetRenderTargets(previousTargets);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(_stackBackdropTarget, new Rectangle(0, 0, width, height), Color.White);
            spriteBatch.End();
            RecordBatchFlush();

            for (var i = first; i < screens.Count; i++)
                screens[i].StackBackdrop = _stackBackdropTarget;
            return first;
        }

        /// <summary>True after keyboard/gamepad input, false after pointer input.</summary>
        public bool KeyboardNavigationActive { get; internal set; }

        /// <summary>
        ///     Opens <paramref name="content"/> as a modal over the current screen (scrim, input and
        ///     focus captured, dismissable per <see cref="ModalOptions"/>). Close it with
        ///     <see cref="ModalScreen.Close"/> or let the user cancel it.
        /// </summary>
        public ModalScreen ShowModal(Control content, ModalOptions? options = null)
        {
            var modal = new ModalScreen(content, options);
            PushOverlay(modal, modal.PresentationTransition);
            return modal;
        }

        /// <summary>Duration of screen transitions.</summary>
        public TimeSpan TransitionDuration { get; set; } = TimeSpan.FromMilliseconds(250);

        private void Push(Screen screen, bool isOverlay, ScreenTransition transition)
        {
            FinishTransition();
            var previous = ActiveScreen;
            if (previous != null)
            {
                // Remembered so NavigateBack can hand focus back to where the user was.
                previous.SavedFocus = FocusedControl?.Screen == previous ? FocusedControl : null;
                // The screen being covered must not keep a drag, mouse capture or pressed state alive.
                previous.OnNavigationFrom(this);
                previous.RaiseNavigatedFrom();
            }

            FocusedControl = null;
            screen.ScreenEngine = this;
            screen.IsOverlay = isOverlay;
            screen.PushTransition = transition;
            ScreenHistory.Push(screen);
            screen.InvalidateLayout(true);
            RebuildVisibleScreens();
            StartTransition(screen, transition, entering: true);
            screen.RaiseNavigatedTo();
        }

        /// <summary>Pops the top screen; by default it leaves with the reverse of its push transition.</summary>
        public void NavigateBack()
        {
            if (ScreenHistory.Count == 0)
                return;
            NavigateBack(ScreenHistory.Peek().PushTransition);
        }

        public void NavigateBack(ScreenTransition transition)
        {
            if (ScreenHistory.Count == 0)
                return;
            FinishTransition();
            FocusedControl = null;
            var screen = ScreenHistory.Pop();
            screen.OnNavigationFrom(this);
            screen.RaiseNavigatedFrom();
            RebuildVisibleScreens();

            var revealed = ActiveScreen;
            // Resizes only invalidate the active screen, so the revealed one may be stale.
            revealed?.InvalidateLayout(true);
            if (revealed?.SavedFocus is { } saved)
            {
                revealed.SavedFocus = null;
                if (saved.Screen == revealed && saved.IsEffectivelyInteractive)
                    FocusedControl = saved;
            }

            if (transition == ScreenTransition.None)
                screen.ScreenEngine = null;
            else
                StartTransition(screen, transition, entering: false);
            revealed?.RaiseNavigatedTo();
        }

        // Bottom-to-top list of what is drawn: the top screen and every screen below it down to
        // (and including) the first non-overlay one. Rebuilt on navigation, read every frame.
        private readonly List<Screen> _visibleScreens = new List<Screen>();

        internal IReadOnlyList<Screen> VisibleScreens
        {
            get
            {
                // ScreenHistory is public; resync if it was changed directly.
                var top = ActiveScreen;
                if (_visibleScreens.Count == 0 ? top != null : !ReferenceEquals(_visibleScreens[_visibleScreens.Count - 1], top))
                    RebuildVisibleScreens();
                return _visibleScreens;
            }
        }

        /// <summary>The popped screen while its exit transition plays (drawn on top), else null.</summary>
        internal Screen? LeavingScreen => _transitionEntering ? null : _transitionScreen;

        private void RebuildVisibleScreens()
        {
            _visibleScreens.Clear();
            foreach (var screen in ScreenHistory)
            {
                _visibleScreens.Insert(0, screen);
                if (!screen.IsOverlay)
                    break;
            }
        }

        private Screen? _transitionScreen;
        private ScreenTransition _transitionKind;
        private bool _transitionEntering;
        private TimeSpan _transitionStart;

        private void StartTransition(Screen screen, ScreenTransition transition, bool entering)
        {
            if (transition == ScreenTransition.None || TransitionDuration <= TimeSpan.Zero)
            {
                screen.SetTransitionVisual(1, Vector2.Zero);
                return;
            }

            _transitionScreen = screen;
            _transitionKind = transition;
            _transitionEntering = entering;
            _transitionStart = ScreenSystem.TotalTime;
            ApplyTransition(0);
        }

        private void UpdateTransition()
        {
            if (_transitionScreen == null)
                return;

            var elapsed = (ScreenSystem.TotalTime - _transitionStart).TotalMilliseconds;
            var progress = (float)Math.Clamp(elapsed / Math.Max(1, TransitionDuration.TotalMilliseconds), 0, 1);
            if (progress >= 1)
                FinishTransition();
            else
                ApplyTransition(progress);
        }

        private void ApplyTransition(float progress)
        {
            var screen = _transitionScreen!;
            // Ease-out cubic; leaving plays the same curve backwards.
            var eased = 1 - (float)Math.Pow(1 - progress, 3);
            var visible = _transitionEntering ? eased : 1 - eased;
            var hidden = 1 - visible;
            switch (_transitionKind)
            {
                case ScreenTransition.Fade:
                    screen.SetTransitionVisual(visible, Vector2.Zero);
                    break;
                case ScreenTransition.SlideFromRight:
                    screen.SetTransitionVisual(1, new Vector2(ScreenRect.Width * hidden, 0));
                    break;
                case ScreenTransition.SlideFromBottom:
                    screen.SetTransitionVisual(1, new Vector2(0, ScreenRect.Height * hidden));
                    break;
            }
        }

        /// <summary>Jumps a running transition to its end (also before starting another one).</summary>
        private void FinishTransition()
        {
            var screen = _transitionScreen;
            if (screen == null)
                return;

            _transitionScreen = null;
            screen.SetTransitionVisual(1, Vector2.Zero);
            if (!_transitionEntering)
                screen.ScreenEngine = null;
        }

        /// <summary>Whether a screen transition is currently animating.</summary>
        public bool IsTransitioning => _transitionScreen != null;

        public void Update(GameTime gameTime)
        {
            ScreenSystem.TotalTime = gameTime.TotalGameTime;
            MarkGameThread();
            BatchFlushesThisFrame = 0;
            LayoutPassesThisFrame = 0;
            FramesPerSecond = gameTime.ElapsedGameTime.TotalSeconds > 0 ? 1 / gameTime.ElapsedGameTime.TotalSeconds : 0;
            DrainGameThreadQueue();
            UpdateRenderQuality();
            UpdateTransition();
            ApplyPendingInsets(force: false);
            ActiveScreen?.Update();
            _toasts?.Update();
            UpdateAccessibility();
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
            NoteInputActivity();
            ActiveScreen?.HandleTextInput(args.Character);
        }
#endif

        /// <summary>
        /// Routes a typed character into the active screen's focused control. Desktop backends call this
        /// from the game window's TextInput event; on Android the host activity/soft keyboard calls it directly.
        /// </summary>
        public void HandleTextInput(char character)
        {
            NoteInputActivity();
            ActiveScreen?.HandleTextInput(character);
        }

        /// <summary>
        /// Routes an editing command (Backspace, Delete, Enter, ...) from a platform keyboard into the
        /// focused control, for soft keyboards whose key presses are too short for polled keyboard state.
        /// </summary>
        public void HandleKeyCommand(KeyboardCommand command)
        {
            NoteInputActivity();
            ActiveScreen?.HandleKeyCommand(command);
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
            _stackBackdropTarget?.Dispose();
            _wake.Dispose();
        }
    }
}
