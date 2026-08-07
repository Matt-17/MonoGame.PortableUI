using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI
{
    internal class ScreenComponent : DrawableGameComponent
    {
        private readonly ScreenEngine _screenEngine;
        private SpriteBatch? _spriteBatch;
        private RenderTarget2D? _scaleTarget;

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
        }

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

        public override void Draw(GameTime gameTime)
        {
            var screen = _screenEngine.ActiveScreen;
            if (_spriteBatch == null || screen == null)
                return;

            var scale = _screenEngine.RenderScale;
            var offset = _screenEngine.RenderOffset;
            var scaled = Math.Abs(scale - 1f) > 0.0001f || offset.X != 0 || offset.Y != 0;

            // No scaling/letter-boxing (reference resolution unset, or window == reference): draw the
            // screen straight to the back buffer, exactly as before.
            if (!scaled)
            {
                screen.Draw(_spriteBatch);
                return;
            }

            // Scaled path: render the UI at its fixed logical (reference) size into an offscreen
            // target, then blit it — uniformly scaled and centred — into the window. The surplus
            // window area stays black (letter-box bars), so the UI keeps its authored aspect ratio.
            var viewport = GraphicsDevice.Viewport;
            var logicalWidth = Math.Max(1, (int)Math.Ceiling(_screenEngine.ScreenRect.Width));
            var logicalHeight = Math.Max(1, (int)Math.Ceiling(_screenEngine.ScreenRect.Height));
            var target = EnsureScaleTarget(logicalWidth, logicalHeight);

            var previousTargets = Effects.RenderTargetHelper.SnapshotRenderTargets(GraphicsDevice);
            GraphicsDevice.SetRenderTarget(target);
            GraphicsDevice.Clear(Color.Transparent);
            screen.Draw(_spriteBatch);

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
            ApplyViewportSize();
            _screenEngine.Update(gameTime);
        }

        private void ApplyViewportSize()
        {
            var viewport = GraphicsDevice.Viewport;
            _screenEngine.ApplyViewportSize(viewport.Width, viewport.Height);
        }
    }
}
