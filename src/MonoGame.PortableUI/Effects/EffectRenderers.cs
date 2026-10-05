using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Effects
{
    /// <summary>
    ///     Draws post effects (scanlines, bloom, curvature, ...). The core only hands it the UI drawn
    ///     offscreen; the optional effects package (CodeIX.PortableUI.Effects) provides one per device
    ///     through <see cref="EffectRenderers.PostEffects"/>.
    /// </summary>
    public interface IPostEffectRenderer : IDisposable
    {
        GraphicsDevice GraphicsDevice { get; }

        /// <summary>How many effects of <paramref name="effects"/> this renderer draws (enabled and
        /// supported). Zero skips the offscreen pass.</summary>
        int CountEnabled(IReadOnlyList<PostEffect> effects);

        /// <summary>Draws <paramref name="ui"/> (the UI rendered offscreen, <paramref name="sourceRect"/>
        /// of it or all of it) through <paramref name="effects"/> into <paramref name="screenRect"/> of the
        /// currently bound target.</summary>
        void Compose(SpriteBatch spriteBatch, Texture2D ui, IReadOnlyList<PostEffect> effects, Rect screenRect, Rect? sourceRect = null);
    }

    /// <summary>
    ///     Prepares the blurred picture of what lies behind the UI, which glass brushes sample. Provided
    ///     per device by the optional effects package through <see cref="EffectRenderers.Backdrop"/>.
    /// </summary>
    public interface IBackdropRenderer : IDisposable
    {
        GraphicsDevice GraphicsDevice { get; }

        /// <summary>Renders the scene behind the UI - <paramref name="external"/> (a game scene, the
        /// screens below an overlay) and/or the screen's <paramref name="background"/> - and blurs it for
        /// the screen draw that follows.</summary>
        void Prepare(SpriteBatch spriteBatch, Rect pixelScreenRect, Texture2D? external, Brush? background);

        /// <summary>Ends the screen draw: brushes drawn afterwards see no backdrop.</summary>
        void Clear();
    }

    /// <summary>
    ///     Factories for the optional renderers. Null (no effects package) means post effects and
    ///     backdrop blur are skipped: the UI draws flat and glass brushes are not available.
    /// </summary>
    public static class EffectRenderers
    {
        public static Func<GraphicsDevice, IPostEffectRenderer>? PostEffects { get; set; }

        public static Func<GraphicsDevice, IBackdropRenderer>? Backdrop { get; set; }
    }
}
