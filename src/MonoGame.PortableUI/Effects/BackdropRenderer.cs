using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Effects
{
    /// <summary>
    ///     Renders the scene behind a screen (an external texture, the screens below an overlay, the
    ///     screen background) and blurs it for the glass brushes drawn in that screen
    ///     (<see cref="BackdropSource"/>).
    /// </summary>
    public sealed class BackdropRenderer : IBackdropRenderer
    {
        private readonly BackdropManager _backdrop;
        private RenderTargetBinding[]? _previousTargets;

        public BackdropRenderer(GraphicsDevice graphicsDevice)
        {
            _backdrop = new BackdropManager(graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice)));
        }

        public GraphicsDevice GraphicsDevice => _backdrop.GraphicsDevice;

        /// <summary>The blur chain (diagnostics: <see cref="BackdropManager.BlurPassesThisFrame"/>).</summary>
        public BackdropManager Blur => _backdrop;

        public void Prepare(SpriteBatch spriteBatch, Rect pixelScreenRect, Texture2D? external, Brush? background)
        {
            var device = spriteBatch.GraphicsDevice;
            _backdrop.BeginFrame();
            var previousTargets = RenderTargetHelper.SnapshotRenderTargets(device, ref _previousTargets);
            var scene = _backdrop.EnsureSceneTarget((int)Math.Ceiling(pixelScreenRect.Width), (int)Math.Ceiling(pixelScreenRect.Height));
            device.SetRenderTarget(scene);
            device.Clear(Color.Transparent);
            spriteBatch.Begin();
            if (external != null)
                spriteBatch.Draw(external, new Rectangle(0, 0, scene.Width, scene.Height), Color.White);
            background?.Draw(spriteBatch, pixelScreenRect);
            spriteBatch.End();
            var blurred = _backdrop.Blur(spriteBatch, scene);
            if (previousTargets.Length == 0)
                device.SetRenderTarget(null);
            else
                device.SetRenderTargets(previousTargets);
            BackdropSource.Set(device, blurred, pixelScreenRect, scene);
        }

        public void Clear() => BackdropSource.Clear(GraphicsDevice);

        public void Dispose() => _backdrop.Dispose();
    }
}
