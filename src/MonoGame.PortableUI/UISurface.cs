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
    /// <summary>
    ///     A UI stack rendered offscreen: into its own <see cref="Target"/> (<see cref="Draw"/>,
    ///     <see cref="DrawIfNeeded"/>) or into a tile of a host's target (<see cref="DrawTo"/>), for in-world
    ///     screens and HUD overlays. The host composites the picture itself.
    ///     <para>
    ///         <b>Draw surfaces before the frame, composite them after.</b> Drawing a surface binds render
    ///         targets and then rebinds the one that was bound before. MonoGame clears a target with
    ///         <see cref="RenderTargetUsage.DiscardContents"/> - the default of the back buffer and of new
    ///         render targets - whenever it is bound, so a surface drawn after the host drew its scene leaves
    ///         only the UI (the scene is gone). Update and draw the surfaces first (or whenever their
    ///         <see cref="NeedsRedraw"/> says so), then draw the scene and blit <see cref="Target"/> on top
    ///         with <c>BlendState.AlphaBlend</c> (the picture is premultiplied):
    ///     </para>
    ///     <code>
    ///     hud.DrawIfNeeded(gameTime);          // binds hud.Target, then the back buffer again
    ///     DrawScene();                         // the host's 3D/2D frame
    ///     batch.Begin(blendState: BlendState.AlphaBlend);
    ///     batch.Draw(hud.Target, viewport.Bounds, Color.White);
    ///     batch.End();
    ///     </code>
    ///     A host that must draw a surface mid-frame renders its scene into a
    ///     <see cref="RenderTargetUsage.PreserveContents"/> target (or sets the back buffer's
    ///     <c>PresentationParameters.RenderTargetUsage</c> to <c>PreserveContents</c> in
    ///     <c>GraphicsDeviceManager.PreparingDeviceSettings</c>, which costs a copy on some platforms).
    /// </summary>
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
        // Layout size; fractional when sized by pixels (SetPixelSize).
        private float _width;
        private float _height;
        // Exact target size set by SetPixelSize, 0 = derived from layout size x LayoutScale.
        private int _pixelWidth;
        private int _pixelHeight;
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
                Theme = theme ?? PortableTheme.CreateDefault(),
                DebugOverlayKey = null
            });
            Engine.SetScreenSize(_width, _height);
            Engine.NavigateToScreen(Screen);
#if ANDROID
            // Android recreates the GL surface on resume and render targets lose their contents; on
            // desktop they survive, so window activation must not redraw every surface at once.
            _game.Activated += OnFrameLost;
#endif
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
                _pixelWidth = _pixelHeight = 0;
                Engine.SetNativeRenderScale(value);
                DropTarget();
                _frameValid = false;
            }
        }

        private int PixelWidth => _pixelWidth > 0 ? _pixelWidth : Math.Max(1, (int)Math.Ceiling(_width * _layoutScale));
        private int PixelHeight => _pixelHeight > 0 ? _pixelHeight : Math.Max(1, (int)Math.Ceiling(_height * _layoutScale));

        /// <summary>Layout size in layout units (fractional after <see cref="SetPixelSize"/>).</summary>
        public Vector2 LayoutSize => new Vector2(_width, _height);

        /// <summary>Size of <see cref="Target"/> in pixels.</summary>
        public Point PixelSize => new Point(PixelWidth, PixelHeight);

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

        /// <summary>
        ///     True when the picture in <see cref="Target"/> is out of date: something changed, an
        ///     animation or the caret wants its next frame, a transition runs, or the target holds no
        ///     frame yet. Does not clear anything; <see cref="DrawIfNeeded"/> and <see cref="Draw"/> do.
        ///     Changes made while a surface is not drawn stay pending: skipping draws never loses one.
        ///     Pointer and key activity keep a surface drawing for 250 ms; the default
        ///     <see cref="InputSource"/> is the real mouse and keyboard, so give unfocused surfaces
        ///     <see cref="NullInputSource.Instance"/>.
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

        /// <summary>Lays the surface out at <paramref name="width"/> x <paramref name="height"/> layout units; the
        /// target is that times <see cref="LayoutScale"/>, rounded up.</summary>
        public void Resize(int width, int height)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);
            if (_width == width && _height == height && _pixelWidth == 0)
                return;

            _width = width;
            _height = height;
            _pixelWidth = _pixelHeight = 0;
            DropTarget();
            _frameValid = false;
            Engine.SetScreenSize(width, height);
        }

        /// <summary>
        ///     Sizes the surface by pixels: <see cref="Target"/> becomes exactly <paramref name="pixelWidth"/> x
        ///     <paramref name="pixelHeight"/> and the layout gets <c>pixels / layoutScale</c> units, fractional if
        ///     need be - for an overlay that must match the window pixel for pixel while the UI is designed at a
        ///     fixed height: <c>hud.SetPixelSize(w, h, h / 1080f)</c> lays a 1366x768 window out at 1920.9 x 1080
        ///     and renders 1366x768. Sets <see cref="LayoutScale"/>.
        /// </summary>
        public void SetPixelSize(int pixelWidth, int pixelHeight, float layoutScale)
        {
            pixelWidth = Math.Max(1, pixelWidth);
            pixelHeight = Math.Max(1, pixelHeight);
            layoutScale = layoutScale > 0 ? layoutScale : 1f;
            if (_pixelWidth == pixelWidth && _pixelHeight == pixelHeight && _layoutScale.Equals(layoutScale))
                return;

            if (!_layoutScale.Equals(layoutScale))
            {
                _layoutScale = layoutScale;
                Engine.SetNativeRenderScale(layoutScale);
            }
            _pixelWidth = pixelWidth;
            _pixelHeight = pixelHeight;
            _width = pixelWidth / layoutScale;
            _height = pixelHeight / layoutScale;
            DropTarget();
            _frameValid = false;
            Engine.SetScreenSize(_width, _height);
        }

        public void Update(GameTime gameTime)
        {
            if (!IsInteractive)
                return;

            Engine.Update(gameTime);
        }

        /// <summary>Draws the surface into <see cref="Target"/> unconditionally and returns it. Rebinds the
        /// previously bound target afterwards, which clears it unless it preserves its contents - draw
        /// surfaces before the host's frame (see <see cref="UISurface"/>).</summary>
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
        ///     Like <see cref="Draw"/>, call it before drawing the host's frame.
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
        ///     surfaces of the same pixel size and copies it into the tile. A smaller tile (a distant
        ///     screen) is rendered at a lower resolution - the next <see cref="DrawToResolutionSteps"/>
        ///     step that covers it, without a relayout and without changing pointer mapping - so it costs
        ///     less to draw and the copy never shrinks it by more than a third.
        ///     <paramref name="target"/> must use <see cref="RenderTargetUsage.PreserveContents"/>, or
        ///     binding it wipes the other tiles. A host that writes into the tile itself calls
        ///     <see cref="Invalidate"/>, or <see cref="DrawIfNeededTo"/> keeps skipping.
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
#if ANDROID
            _game.Activated -= OnFrameLost;
#endif
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
            var resolution = destination is { } tile ? DrawResolutionFor(tile.Width, tile.Height) : 1f;
            var scratch = destination.HasValue
                ? RenderTargetPool.Rent(device, ScaledPixels(PixelWidth, resolution), ScaledPixels(PixelHeight, resolution))
                : null;
            try
            {
                // Restore whatever was bound (e.g. the host screen's post-FX target), not just null.
                var previousTargets = Effects.RenderTargetHelper.SnapshotRenderTargets(device, ref _previousTargets);
                // A full target of exactly the size drawn: every offscreen pass (clip shapes, post FX,
                // glass) keeps working in the coordinates it expects.
                device.SetRenderTarget(scratch ?? target);
                device.Clear(Color.Transparent);
                // The whole stack: overlays/modals pushed on this surface's engine and its toasts too.
                // A small tile draws the UI smaller (draw-only: layout and pointer mapping stay).
                Engine.DrawResolution = resolution;
                try
                {
                    Engine.DrawStack(spriteBatch);
                }
                finally
                {
                    Engine.DrawResolution = 1f;
                }
                LastDrawResolution = resolution;
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
                    RenderTargetPool.Return(device, scratch);
                SharedSpriteBatches.Return(device, spriteBatch);
            }
            _frameTarget = target;
            _frameRect = destination ?? target.Bounds;
            _frameValid = true;
            Engine.RecordFrame(true);
        }

        /// <summary>
        ///     Layer-cache pixels (<see cref="Controls.CacheMode.Bitmap"/>) all surfaces of a game share;
        ///     surfaces drawn now evict the caches of those not drawn for the longest time. Default: three
        ///     1920x1080 screens (~25 MB).
        /// </summary>
        public static long SharedLayerCacheBudgetPixels { get; set; } = 1920L * 1080L * 3;

        /// <summary>Resolution factor of the last draw: 1 for <see cref="Draw"/> and full-size tiles,
        /// a <see cref="DrawToResolutionSteps"/> value for smaller ones.</summary>
        public float LastDrawResolution { get; private set; } = 1f;

        /// <summary>
        ///     The resolutions <see cref="DrawTo"/> renders a smaller tile at, as fractions of the
        ///     surface's pixel size. A tile renders at the smallest step that still covers it, so the copy
        ///     shrinks by at most a third (no shimmer) and scratch targets and glyph sizes stay few.
        /// </summary>
        public static IReadOnlyList<float> DrawToResolutionSteps { get; } = new[] { 1f, 0.75f, 0.5f, 0.375f, 0.25f, 0.1875f, 0.125f };

        internal float DrawResolutionFor(int tileWidth, int tileHeight)
        {
            var needed = Math.Max(tileWidth / (float)PixelWidth, tileHeight / (float)PixelHeight);
            var steps = DrawToResolutionSteps;
            var resolution = steps[0];
            // Steps descend: keep the smallest one that still covers the tile (a tiny epsilon keeps a
            // tile of exactly half size at 1/2 despite rounding).
            for (var i = 1; i < steps.Count && steps[i] >= needed - 0.0001f; i++)
                resolution = steps[i];
            return resolution;
        }

        private static int ScaledPixels(int pixels, float resolution)
            => resolution >= 1f ? pixels : Math.Max(1, (int)Math.Ceiling(pixels * resolution));

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
            surfacePoint = PostEffectGeometry.DisplayToUi(DisplayGeometry(), surfacePoint, rect);
            // Half a pixel of slack: a point exactly on the edge survives the barrel round trip.
            return surfacePoint.X > -0.5f && surfacePoint.Y > -0.5f && surfacePoint.X < _width + 0.5f && surfacePoint.Y < _height + 0.5f;
        }

        /// <summary>The UI point under a point of the (curved) picture — undoes a CRT barrel.</summary>
        public PointF MapDisplayToUi(PointF surfacePoint)
        {
            return PostEffectGeometry.DisplayToUi(DisplayGeometry(), surfacePoint, new Rect(0, 0, _width, _height));
        }

        /// <summary>Where a UI point appears on the (curved) picture — applies a CRT barrel.</summary>
        public PointF MapUiToDisplay(PointF uiPoint)
        {
            return PostEffectGeometry.UiToDisplay(DisplayGeometry(), uiPoint, new Rect(0, 0, _width, _height));
        }

        /// <summary>The display effects that move pixels (curvature), or null. Only display effects
        /// curve the picture (a theme's curvature is ignored), the same rule the screen's input mapping
        /// follows; a host drawing the display stage itself (PostEffectMode below All) maps through its
        /// own curve.</summary>
        private IReadOnlyList<PostEffect>? DisplayGeometry()
        {
            if (Engine.Options.PostEffectMode != PostEffectMode.All)
                return null;
            var display = Engine.Options.PostEffects;
            return PostEffectGeometry.MovesPixels(display) ? display : null;
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
