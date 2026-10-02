using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI
{
    internal class ScreenComponent : DrawableGameComponent
    {
        private readonly ScreenEngine _screenEngine;
        private SpriteBatch? _spriteBatch;
        private RenderTarget2D? _scaleTarget;
        private RenderTargetBinding[]? _previousTargets;
        private bool? _hostFixedTimeStep;
        private bool _drawPending;
        private long _lastDrawTimestamp;
        private int _viewportWidth;
        private int _viewportHeight;

        internal ScreenComponent(ScreenEngine screenEngine, Game game) : base(game)
        {
            _screenEngine = screenEngine;
            UpdateOrder = int.MaxValue;
            DrawOrder = int.MaxValue;
        }

        public override void Initialize()
        {
            base.Initialize();
            ApplyViewportSize();
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            // The frame on screen may be gone after these (Android recreates the surface on resume).
            Game.Activated += OnFrameLost;
            GraphicsDevice.DeviceReset += OnFrameLost;
        }

        private void OnFrameLost(object? sender, EventArgs args) => _screenEngine.RequestRedrawFor(ScreenEngine.RedrawGrace);

        protected override void LoadContent()
        {
            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            _scaleTarget?.Dispose();
            _scaleTarget = null;
            base.UnloadContent();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Game.Activated -= OnFrameLost;
                if (GraphicsDevice != null)
                    GraphicsDevice.DeviceReset -= OnFrameLost;
                RestoreHostTimeStep();
            }
            base.Dispose(disposing);
        }

        public override void Draw(GameTime gameTime)
        {
            _drawPending = false;
            _lastDrawTimestamp = Stopwatch.GetTimestamp();
            _screenEngine.RecordFrame(true);
            if (_spriteBatch == null || _screenEngine.ActiveScreen == null && _screenEngine.LeavingScreen == null)
                return;

            // Offscreen passes (clip shapes, overscroll stretch, blur) switch render targets mid-frame.
            // With the default DiscardContents MonoGame clears the back buffer when switching back,
            // wiping everything drawn before the pass (seen on Android as black areas).
            var presentation = GraphicsDevice.PresentationParameters;
            if (presentation.RenderTargetUsage != RenderTargetUsage.PreserveContents)
                presentation.RenderTargetUsage = RenderTargetUsage.PreserveContents;

            var scale = _screenEngine.RenderScale;
            var offset = _screenEngine.RenderOffset;
            var scaled = Math.Abs(scale - 1f) > 0.0001f || offset.X != 0 || offset.Y != 0;

            // No scaling/letter-boxing (reference resolution unset, or window == reference): draw the
            // screen straight to the back buffer, exactly as before.
            if (!scaled || _screenEngine.ScalesNatively)
            {
                DrawScreens(_spriteBatch);
                return;
            }

            // Scaled path: render the UI at its fixed logical (reference) size into an offscreen
            // target, then blit it — uniformly scaled and centred — into the window. The surplus
            // window area stays black (letter-box bars), so the UI keeps its authored aspect ratio.
            var viewport = GraphicsDevice.Viewport;
            var logicalWidth = Math.Max(1, (int)Math.Ceiling(_screenEngine.ScreenRect.Width));
            var logicalHeight = Math.Max(1, (int)Math.Ceiling(_screenEngine.ScreenRect.Height));
            var target = EnsureScaleTarget(logicalWidth, logicalHeight);

            var previousTargets = Effects.RenderTargetHelper.SnapshotRenderTargets(GraphicsDevice, ref _previousTargets);
            GraphicsDevice.SetRenderTarget(target);
            GraphicsDevice.Clear(Color.Transparent);
            DrawScreens(_spriteBatch);

            if (previousTargets.Length == 0)
                GraphicsDevice.SetRenderTarget(null);
            else
                GraphicsDevice.SetRenderTargets(previousTargets);

            var destination = new Rectangle(
                (int)Math.Round(offset.X),
                (int)Math.Round(offset.Y),
                (int)Math.Round(_screenEngine.ScreenRect.Width * scale),
                (int)Math.Round(_screenEngine.ScreenRect.Height * scale));

            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp);
            _spriteBatch.Draw(target, destination, Color.White);
            _spriteBatch.End();
        }

        /// <summary>Draws the visible stack bottom to top (overlays over the screens they cover),
        /// then a screen that is still playing its exit transition.</summary>
        private void DrawScreens(SpriteBatch spriteBatch) => _screenEngine.DrawStack(spriteBatch);

        private RenderTarget2D EnsureScaleTarget(int width, int height)
        {
            if (_scaleTarget != null && _scaleTarget.Width == width && _scaleTarget.Height == height
                && !_scaleTarget.IsDisposed && ReferenceEquals(_scaleTarget.GraphicsDevice, GraphicsDevice))
                return _scaleTarget;

            _scaleTarget?.Dispose();
            // PreserveContents: Screen.Draw may switch to blur/post-FX targets mid-frame and return.
            _scaleTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color,
                DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            return _scaleTarget;
        }

        public override void Update(GameTime gameTime)
        {
            var started = Stopwatch.GetTimestamp();
            ApplyViewportSize();
            _screenEngine.Update(gameTime);

            if (_screenEngine.Options.RenderMode != RenderMode.OnDemand)
            {
                RestoreHostTimeStep();
                WaitForFrameSlot();
                return;
            }

            // A fixed time step catching up runs several updates before one draw: a frame requested
            // in an earlier update of this tick must not be suppressed by a later one.
            if (_drawPending || _screenEngine.ConsumeRedrawRequest())
            {
                _drawPending = true;
                RestoreHostTimeStep();
                WaitForFrameSlot();
                return;
            }

            // Idle: keep the last frame on screen (no draw, no present) and sleep out the rest of
            // the idle period in one go. A fixed time step would instead spin in 1 ms sleeps up to
            // the display rate and then catch up with several updates per tick.
            _screenEngine.RecordFrame(false);
            Game.SuppressDraw();
            if (_hostFixedTimeStep == null)
            {
                _hostFixedTimeStep = Game.IsFixedTimeStep;
                Game.IsFixedTimeStep = false;
            }
            var remaining = _screenEngine.Options.IdleUpdateInterval - Stopwatch.GetElapsedTime(started);
            if (remaining.TotalMilliseconds >= 1)
                Thread.Sleep(remaining);
        }

        /// <summary>
        ///     Frame cap (<see cref="ScreenEngine.EffectiveMaxFrameRate"/>): waits until the next frame is
        ///     due in one sleep, so the draw that follows lands on the cap. Skipping draws instead would
        ///     leave a variable time step without vsync spinning through updates.
        /// </summary>
        private void WaitForFrameSlot()
        {
            var maxFrameRate = _screenEngine.EffectiveMaxFrameRate;
            if (maxFrameRate <= 0 || _lastDrawTimestamp == 0)
                return;
            var remaining = TimeSpan.FromSeconds(1.0 / maxFrameRate) - Stopwatch.GetElapsedTime(_lastDrawTimestamp);
            if (remaining.TotalMilliseconds >= 1)
                Thread.Sleep(remaining);
        }

        /// <summary>Hands the game loop its own time step back once frames are drawn again.</summary>
        private void RestoreHostTimeStep()
        {
            if (_hostFixedTimeStep is not { } fixedTimeStep)
                return;
            _hostFixedTimeStep = null;
            Game.IsFixedTimeStep = fixedTimeStep;
        }

        private void ApplyViewportSize()
        {
            var viewport = GraphicsDevice.Viewport;
            if (viewport.Width != _viewportWidth || viewport.Height != _viewportHeight)
            {
                _viewportWidth = viewport.Width;
                _viewportHeight = viewport.Height;
                _screenEngine.RequestRedraw();
            }
            _screenEngine.ApplyViewportSize(viewport.Width, viewport.Height);
        }
    }
}
