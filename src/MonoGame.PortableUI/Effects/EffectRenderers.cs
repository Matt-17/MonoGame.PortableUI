using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
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
    ///     Blurs bands of a <see cref="Controls.ProgressiveBlur"/>'s content, strongest at an edge row and fading to
    ///     sharp. Provided per device by the optional effects package through <see cref="EffectRenderers.ProgressiveBlur"/>.
    /// </summary>
    public interface IProgressiveBlurRenderer : IDisposable
    {
        GraphicsDevice GraphicsDevice { get; }

        /// <summary>Blurs <paramref name="band"/> of <paramref name="content"/> (pixels) into the renderer's own
        /// target <paramref name="slot"/> (0 or 1): radius <paramref name="radius"/> pixels at row
        /// <paramref name="edgeRow"/>, falling linearly to 0 over <paramref name="length"/> rows. Switches render
        /// targets; the caller rebinds its own. <paramref name="tint"/> (premultiplied) is mixed in with the same
        /// gradient: fully at the edge row, not at all where the band runs out.</summary>
        void Prepare(SpriteBatch spriteBatch, Texture2D content, Rectangle band, int edgeRow, int length, float radius, Color tint, int slot);

        /// <summary>Draws a prepared band at its place in the bound target, the sharp content mixed into its
        /// weakly blurred rows. The scissor of <paramref name="rasterizer"/> applies.</summary>
        void Draw(SpriteBatch spriteBatch, int slot, RasterizerState rasterizer);

        /// <summary>Returns the targets of this draw.</summary>
        void Release();
    }

    /// <summary>
    ///     Factories for the optional renderers. Null (no effects package) means post effects and
    ///     backdrop blur are skipped: the UI draws flat and glass brushes are not available.
    /// </summary>
    public static class EffectRenderers
    {
        public static Func<GraphicsDevice, IPostEffectRenderer>? PostEffects { get; set; }

        public static Func<GraphicsDevice, IBackdropRenderer>? Backdrop { get; set; }

        public static Func<GraphicsDevice, IProgressiveBlurRenderer>? ProgressiveBlur { get; set; }
    }
}
