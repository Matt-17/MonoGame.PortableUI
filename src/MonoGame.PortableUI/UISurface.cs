using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Effects;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI
{
    public sealed class UISurface : IDisposable
    {
        private readonly Game _game;
        private RenderTarget2D? _target;
        // Where the last finished frame went (the own Target or a host's tile) and whether it is
        // still there: cleared when the target is replaced, resized or its device/surface is lost.
        private RenderTarget2D? _frameTarget;
        private Rectangle _frameRect;
        private bool _frameValid;
        private GraphicsDevice? _watchedDevice;
        private RenderTargetBinding[]? _previousTargets;
        private int _width;
        private int _height;
        private float _layoutScale = 1f;

        public UISurface(Game game, Screen screen, int width, int height, PortableTheme? theme = null)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            Screen = screen ?? throw new ArgumentNullException(nameof(screen));
            _width = Math.Max(1, width);
            _height = Math.Max(1, height);
            Engine = ScreenEngine.CreateSurfaceEngine(game, new ScreenEngineOptions
            {
                AddComponentToGame = false,
                ScreenSizeMode = ScreenSizeMode.Manual,
                Theme = theme ?? PortableTheme.CreateDefault()
            });
            Engine.SetScreenSize(_width, _height);
            Engine.NavigateToScreen(Screen);
            _game.Activated += OnFrameLost;
        }

        public ScreenEngine Engine { get; }

        /// <summary>
        ///     Effects of the in-world display this surface is shown on (e.g. CRT curvature,
        ///     scanlines); independent of the theme. Shortcut for <c>Engine.Options.PostEffects</c>.
        /// </summary>
        public IReadOnlyList<PostEffect> PostEffects
        {
            get => Engine.Options.PostEffects;
            set => Engine.Options.PostEffects = value;
        }

        /// <summary>
        ///     Which post-effect stages this surface draws (shortcut for <c>Engine.Options.PostEffectMode</c>).
        ///     <see cref="PortableUI.PostEffectMode.None"/> renders the UI flat for a host that applies
        ///     curvature, scanlines and glass in its own shader; <see cref="IsPointOnDisplay"/> and the
        ///     <c>Map…</c> methods then ignore the display's barrel too.
        /// </summary>
        public PostEffectMode PostEffectMode
        {
            get => Engine.Options.PostEffectMode;
            set => Engine.Options.PostEffectMode = value;
        }

        public Screen Screen { get; }
        public RenderTarget2D Target => EnsureTarget();
        public PortableTheme Theme
        {
            get { return Engine.Options.Theme; }
            set { Engine.Options.Theme = value; }
        }

        /// <summary>
        /// Optional external scene texture the screen's glass brushes blur behind the UI
        /// (see <see cref="Screen.ExternalBackdrop"/>).
        /// </summary>
        public Texture2D? ExternalBackdrop
        {
            get { return Screen.ExternalBackdrop; }
            set { Screen.ExternalBackdrop = value; }
        }

        /// <summary>
        ///     Pixels per layout unit: the surface is laid out at its width × height but rendered at
        ///     width × height × LayoutScale (HiDPI, density-independent layouts). Default 1.
        /// </summary>
        public float LayoutScale
        {
            get => _layoutScale;
            set
            {
                value = value > 0 ? value : 1f;
                if (_layoutScale.Equals(value))
                    return;
                _layoutScale = value;
                Engine.SetNativeRenderScale(value);
                DropTarget();
                _frameValid = false;
            }
        }

        private int PixelWidth => Math.Max(1, (int)Math.Ceiling(_width * _layoutScale));
        private int PixelHeight => Math.Max(1, (int)Math.Ceiling(_height * _layoutScale));

        public bool IsInteractive { get; set; } = true;
        /// <summary>
        ///     True while this surface receives the keyboard: the game window's text input then goes
        ///     to this surface only (not to the main UI, not to other surfaces). Set it directly for a
        ///     single in-world screen, or let a <see cref="SurfaceFocusManager"/> switch between several.
        /// </summary>
        public bool HasKeyboardFocus
        {
            // Derived from the single owner, so focusing one surface unfocuses any other.
            get => ReferenceEquals(ScreenEngine.GetTextInputOwner(_game), Engine);
            set
            {
                if (value)
                    ScreenEngine.SetTextInputOwner(_game, Engine);
                else if (HasKeyboardFocus)
                    ScreenEngine.SetTextInputOwner(_game, null);
            }
        }
        public float ScaleFactor { get; set; } = 1;

        /// <summary>Text-mode grid of this surface's display (shortcut for <c>Engine.Options.TextGrid</c>); per surface.</summary>
        public TextGrid? TextGrid
        {
            get => Engine.Options.TextGrid;
            set => Engine.Options.TextGrid = value;
        }

        /// <summary>
        ///     Draws the theme's pointer (<see cref="PortableTheme.Cursor"/>) inside the surface, bent by
        ///     its display effects. Shortcut for <c>Engine.Options.ShowSoftwareCursor</c>; the pointer
        ///     position comes from <see cref="InputSource"/>.
        /// </summary>
        public bool ShowSoftwareCursor
        {
            get => Engine.Options.ShowSoftwareCursor;
            set => Engine.Options.ShowSoftwareCursor = value;
        }
        public IInputSource InputSource
        {
            get { return Screen.InputSource; }
            set { Screen.InputSource = value ?? NullInputSource.Instance; }
        }

        /// <summary>Unused: post effects run on <see cref="ScreenEngine.PostProcess"/> of <see cref="Engine"/>.</summary>
        [Obsolete("Never used for drawing; post effects run on Engine.PostProcess. Always null.")]
        public PostProcessManager? PostProcessManager => null;

        /// <summary>
        ///     True when the picture in <see cref="Target"/> is out of date: something changed, an
        ///     animation or the caret wants its next frame, a transition runs, or the target holds no
        ///     frame yet. Does not clear anything; <see cref="DrawIfNeeded"/> and <see cref="Draw"/> do.
        ///     Changes made while a surface is not drawn stay pending: skipping draws never loses one.
        /// </summary>
        public bool NeedsRedraw => !HasFrame || Engine.PeekRedrawRequest();

        /// <summary>
        ///     When this surface next wants a frame, on the <see cref="ScreenSystem.TotalTime"/> clock:
        ///     <see cref="TimeSpan.Zero"/> when one is due now, <see cref="TimeSpan.MaxValue"/> when idle,
        ///     otherwise the scheduled time (e.g. the next caret blink) - for host schedulers that
        ///     spread surface draws over frames.
        /// </summary>
        public TimeSpan NextRedrawDue => HasFrame ? Engine.NextRedrawDue : TimeSpan.Zero;

        /// <summary>Frames drawn into <see cref="Target"/>, and calls of <see cref="DrawIfNeeded"/> that
        /// had nothing to draw.</summary>
        public long FramesDrawn => Engine.FramesDrawn;

        /// <inheritdoc cref="FramesDrawn"/>
        public long FramesSkipped => Engine.FramesSkipped;

        /// <summary>Marks the whole picture out of date, layer caches included - e.g. after the host
        /// changed something the UI cannot see.</summary>
        public void Invalidate()
        {
            Engine.InvalidateLayerCaches();
            Engine.RequestRedraw();
        }

        public void Resize(int width, int height)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);
            if (_width == width && _height == height)
                return;

            _width = width;
            _height = height;
            DropTarget();
            _frameValid = false;
            Engine.SetScreenSize(width, height);
        }

        public void Update(GameTime gameTime)
        {
            if (!IsInteractive)
                return;

            Engine.Update(gameTime);
        }

        /// <summary>Draws the surface into <see cref="Target"/> unconditionally and returns it.</summary>
        public RenderTarget2D Draw(GameTime gameTime)
        {
            Engine.ConsumeRedrawRequest();
            var target = EnsureTarget();
            Render(target, null);
            return target;
        }

        /// <summary>
        ///     Draws into <see cref="Target"/> only when <see cref="NeedsRedraw"/>; otherwise the previous
        ///     picture stays (the target preserves its contents). Returns whether it drew. Call it for the
        ///     surfaces that are seen: the requests of unseen ones stay pending until they are drawn.
        /// </summary>
        public bool DrawIfNeeded(GameTime gameTime)
        {
            // Consumed before drawing, so a request raised while drawing (an animation's next frame)
            // carries over to the next call.
            var requested = Engine.ConsumeRedrawRequest();
            var target = EnsureTarget();
            if (!requested && HasFrameAt(target, target.Bounds))
            {
                Engine.RecordFrame(false);
                return false;
            }
            Render(target, null);
            return true;
        }

        /// <summary>
        ///     Draws the surface into <paramref name="destination"/> of a host's render target, e.g. this
        ///     surface's tile in a screen atlas, replacing what was there (alpha included). The surface then
        ///     needs no <see cref="Target"/> of its own: it renders into a scratch target shared by all
        ///     surfaces of the same pixel size and copies it into the tile, scaled to the tile's size - a
        ///     smaller tile is a lower resolution for a distant screen. <paramref name="target"/> must use
        ///     <see cref="RenderTargetUsage.PreserveContents"/>, or binding it wipes the other tiles.
        /// </summary>
        public void DrawTo(RenderTarget2D target, Rectangle destination, GameTime gameTime)
        {
            ArgumentNullException.ThrowIfNull(target);
            Engine.ConsumeRedrawRequest();
            Render(target, destination);
        }

        /// <summary>
        ///     <see cref="DrawTo"/> only when <see cref="NeedsRedraw"/> or the tile moved; otherwise the
        ///     tile keeps the previous picture. Returns whether it drew.
        /// </summary>
        public bool DrawIfNeededTo(RenderTarget2D target, Rectangle destination, GameTime gameTime)
        {
            ArgumentNullException.ThrowIfNull(target);
            var requested = Engine.ConsumeRedrawRequest();
            if (!requested && HasFrameAt(target, destination))
            {
                Engine.RecordFrame(false);
                return false;
            }
            Render(target, destination);
            return true;
        }

        public void Dispose()
        {
            HasKeyboardFocus = false;
            _game.Activated -= OnFrameLost;
            if (_watchedDevice != null)
                _watchedDevice.DeviceReset -= OnFrameLost;
            _watchedDevice = null;
            _target?.Dispose();
            _frameTarget = null;
            Engine.Dispose();
        }

        private bool HasFrame => _frameTarget != null && HasFrameAt(_frameTarget, _frameRect);

        private bool HasFrameAt(RenderTarget2D target, Rectangle rect)
            => _frameValid && ReferenceEquals(_frameTarget, target) && _frameRect == rect
               && !target.IsDisposed && ReferenceEquals(target.GraphicsDevice, _game.GraphicsDevice);

        /// <summary>Draws the stack into <paramref name="target"/>; with a <paramref name="destination"/>
        /// through a shared scratch target copied into that rectangle.</summary>
        private void Render(RenderTarget2D target, Rectangle? destination)
        {
            var device = _game.GraphicsDevice;
            WatchDevice(device);
            // One batch per device for all surfaces, not one each (rented: a surface may draw another).
            var spriteBatch = SharedSpriteBatches.Rent(device);
            var scratch = destination.HasValue ? ScratchTargets.Rent(device, PixelWidth, PixelHeight) : null;
            try
            {
                // Restore whatever was bound (e.g. the host screen's post-FX target), not just null.
                var previousTargets = Effects.RenderTargetHelper.SnapshotRenderTargets(device, ref _previousTargets);
                // A full target of exactly the surface's size: every offscreen pass (clip shapes, post
                // FX, glass) keeps working in the coordinates it expects.
                device.SetRenderTarget(scratch ?? target);
                device.Clear(Color.Transparent);
                // The whole stack: overlays/modals pushed on this surface's engine and its toasts too.
                Engine.DrawStack(spriteBatch);
                if (scratch != null)
                {
                    device.SetRenderTarget(target);
                    var rect = destination!.Value;
                    var sampler = rect.Width == scratch.Width && rect.Height == scratch.Height ? SamplerState.PointClamp : SamplerState.LinearClamp;
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, sampler, DepthStencilState.None, RasterizerState.CullNone);
                    spriteBatch.Draw(scratch, rect, Color.White);
                    spriteBatch.End();
                }
                if (previousTargets.Length == 0)
                    device.SetRenderTarget(null);
                else
                    device.SetRenderTargets(previousTargets);
            }
            finally
            {
                if (scratch != null)
                    ScratchTargets.Return(device, scratch);
                SharedSpriteBatches.Return(device, spriteBatch);
            }
            _frameTarget = target;
            _frameRect = destination ?? target.Bounds;
            _frameValid = true;
            Engine.RecordFrame(true);
        }

        private void WatchDevice(GraphicsDevice device)
        {
            if (ReferenceEquals(_watchedDevice, device))
                return;
            if (_watchedDevice != null)
                _watchedDevice.DeviceReset -= OnFrameLost;
            _watchedDevice = device;
            device.DeviceReset += OnFrameLost;
        }

        // Render target contents do not survive a lost device or surface (Android resume).
        private void OnFrameLost(object? sender, EventArgs args)
        {
            _frameValid = false;
            Engine.InvalidateLayerCaches();
            Engine.RequestRedraw();
        }

        private void DropTarget()
        {
            if (_target != null && ReferenceEquals(_frameTarget, _target))
                _frameValid = false;
            _target?.Dispose();
            _target = null;
        }

        private RenderTarget2D EnsureTarget()
        {
            if (_target is { IsDisposed: false } && _target.Width == PixelWidth && _target.Height == PixelHeight
                && ReferenceEquals(_target.GraphicsDevice, _game.GraphicsDevice))
                return _target;

            DropTarget();
            // PreserveContents: Screen.Draw may switch to blur/post-FX targets mid-frame and come back,
            // and DrawIfNeeded keeps the last picture between draws.
            _target = new RenderTarget2D(_game.GraphicsDevice, PixelWidth, PixelHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            return _target;
        }

        /// <summary>
        ///     True when <paramref name="surfacePoint"/> (surface units, e.g. from mapping a ray onto the
        ///     quad) lies on the visible picture — inside the surface and, with a CRT barrel among the
        ///     theme or display effects, inside the curved image rather than its dark border. Hosts use
        ///     it to route input and to switch between the in-game and the system cursor.
        /// </summary>
        public bool IsPointOnDisplay(PointF surfacePoint)
        {
            var rect = new Rect(0, 0, _width, _height);
            var distortion = BarrelDistortion();
            if (distortion > 0)
                surfacePoint = Effects.PostProcessManager.InverseBarrel(surfacePoint, rect, distortion);
            // Half a pixel of slack: a point exactly on the edge survives the barrel round trip.
            return surfacePoint.X > -0.5f && surfacePoint.Y > -0.5f && surfacePoint.X < _width + 0.5f && surfacePoint.Y < _height + 0.5f;
        }

        /// <summary>The UI point under a point of the (curved) picture — undoes a CRT barrel.</summary>
        public PointF MapDisplayToUi(PointF surfacePoint)
        {
            var distortion = BarrelDistortion();
            return distortion > 0 ? Effects.PostProcessManager.InverseBarrel(surfacePoint, new Rect(0, 0, _width, _height), distortion) : surfacePoint;
        }

        /// <summary>Where a UI point appears on the (curved) picture — applies a CRT barrel.</summary>
        public PointF MapUiToDisplay(PointF uiPoint)
        {
            var distortion = BarrelDistortion();
            return distortion > 0 ? Effects.PostProcessManager.ForwardBarrel(uiPoint, new Rect(0, 0, _width, _height), distortion) : uiPoint;
        }

        private float BarrelDistortion()
        {
            // Only display effects curve the picture (a theme's curvature is ignored), the same
            // rule the screen's input mapping follows. A host drawing the display stage itself
            // (PostEffectMode below All) maps through its own curve.
            if (Engine.Options.PostEffectMode != PostEffectMode.All)
                return 0;
            var display = Engine.Options.PostEffects;
            var barrel = display.Count > 0 ? Screen.FindEnabledBarrel(display) : null;
            return barrel == null ? 0 : MathHelper.Clamp(barrel.Distortion, 0, 0.5f);
        }

        /// <summary>Scratch targets for <see cref="DrawTo"/>, shared by all surfaces of a device and pixel
        /// size; a surface drawn while another one draws rents a second one.</summary>
        private static class ScratchTargets
        {
            private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<GraphicsDevice, Dictionary<(int, int), Stack<RenderTarget2D>>> Pools = new();

            public static RenderTarget2D Rent(GraphicsDevice device, int width, int height)
            {
                var pool = Pool(device, width, height);
                while (pool.Count > 0)
                {
                    var target = pool.Pop();
                    if (!target.IsDisposed)
                        return target;
                }
                // PreserveContents: offscreen passes switch targets mid-frame and come back.
                return new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }

            public static void Return(GraphicsDevice device, RenderTarget2D target)
            {
                if (!target.IsDisposed)
                    Pool(device, target.Width, target.Height).Push(target);
            }

            private static Stack<RenderTarget2D> Pool(GraphicsDevice device, int width, int height)
            {
                var pools = Pools.GetValue(device, static _ => new Dictionary<(int, int), Stack<RenderTarget2D>>());
                if (!pools.TryGetValue((width, height), out var pool))
                    pools[(width, height)] = pool = new Stack<RenderTarget2D>();
                return pool;
            }
        }

        /// <summary>SpriteBatches shared by all surfaces of a device; nested surface draws rent a second one.</summary>
        private static class SharedSpriteBatches
        {
            private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<GraphicsDevice, Stack<SpriteBatch>> Pools = new();

            public static SpriteBatch Rent(GraphicsDevice device)
            {
                var pool = Pools.GetValue(device, static _ => new Stack<SpriteBatch>());
                while (pool.Count > 0)
                {
                    var batch = pool.Pop();
                    if (!batch.IsDisposed)
                        return batch;
                }
                return new SpriteBatch(device);
            }

            public static void Return(GraphicsDevice device, SpriteBatch batch)
            {
                if (!batch.IsDisposed)
                    Pools.GetValue(device, static _ => new Stack<SpriteBatch>()).Push(batch);
            }
        }
    }
}
