using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Effects;

namespace MonoGame.PortableUI
{
    // ProgressiveBlur: the content goes into a texture; the rows outside the bands are composited sharp, the bands
    // through the core ProgressiveBlurRenderer (if its shader cannot load, reported once, the content draws sharp).
    // The bands of each blur drawn this frame are kept, so shared-element flights and controls set aside above them
    // that belong to it get the same bands (Screen.DrawSharedFlights).
    public abstract partial class Screen
    {
        private ProgressiveBlur? _blurInProgress;
        private RenderTargetBinding[]? _blurPreviousTargets;

        private bool TryDrawProgressiveBlur(SpriteBatch spriteBatch, ProgressiveBlur control, RenderContext parentContext, RenderContext context)
        {
            // Nested blurs draw their content sharp inside the outer one.
            if (_blurInProgress != null)
                return false;
            var engine = ScreenEngine;
            if (engine == null || engine.EffectiveRenderQuality == RenderQuality.Low)
                return false;

            var rect = context.RenderRect;
            var radius = control.MaxRadius * context.Scale.Y;
            var topHeight = control.TopHeight * context.Scale.Y;
            var bottomHeight = control.BottomHeight * context.Scale.Y;
            // An own tint extent: the blur bands carry no tint, it is laid over them along its own rows.
            var ownTint = control.HasOwnTint;
            var topTint = ownTint && control.TopTintHeight.IsFixed() && control.TintOpacity > 0 ? control.TopTintHeight * context.Scale.Y : 0;
            var bottomTint = ownTint && control.BottomTintHeight.IsFixed() && control.TintOpacity > 0 ? control.BottomTintHeight * context.Scale.Y : 0;
            if (radius < 1)
                topHeight = bottomHeight = 0;
            if (rect.Width <= 0 || rect.Height <= 0 || (topHeight < 1 && bottomHeight < 1 && topTint < 1 && bottomTint < 1)
                || engine.ProgressiveBlurRenderer == null)
                return false;

            var device = spriteBatch.GraphicsDevice;
            var previousTargets = RenderTargetHelper.SnapshotRenderTargets(device, ref _blurPreviousTargets);
            var content = RentFrameTarget(device, rect);
            if (content == null)
                return false;
            device.SetRenderTarget(content);
            device.Clear(Color.Transparent);
            _blurInProgress = control;
            try
            {
                DrawControlBatched(spriteBatch, control, parentContext);
            }
            finally
            {
                _blurInProgress = null;
            }

            var tint = PremultipliedTint(control, parentContext.Opacity);
            if (ThemeTwins != null && ThemeTwins.TryGetValue(control, out var pair) && pair.Old is ProgressiveBlur old)
                tint = Color.Lerp(PremultipliedTint(old, parentContext.Opacity), tint, pair.Mix(ThemeTwinProgress, ThemeTwinDuration));
            var tints = new BlurTints(ownTint, topTint, bottomTint, control.TintSolidHeight * context.Scale.Y);
            var bands = new BlurBands(rect, radius, topHeight, bottomHeight, tint, tints, engine.DrawFrameNumber);
            control.DrawnBands = bands;
            ComposeBlurred(spriteBatch, engine, content, bands, previousTargets, parentContext.ChildClipRect, tintByCoverage: false);
            RenderTargetPool.Return(device, content);
            return true;
        }

        private static Color PremultipliedTint(ProgressiveBlur blur, float opacity)
        {
            var tint = blur.TintColor * (blur.TintColor.A / 255f * blur.TintOpacity * opacity);
            tint.A = (byte)Math.Round(blur.TintColor.A * blur.TintOpacity * opacity);
            return tint;
        }

        // A transparent-to-be target covering the frame (and the rect, if it reaches past it), so a subtree keeps drawing
        // at its own coordinates.
        private RenderTarget2D? RentFrameTarget(GraphicsDevice device, Rect rect)
        {
            var width = (int)Math.Ceiling(Math.Max(PixelScreenRect.Right, rect.Right));
            var height = (int)Math.Ceiling(Math.Max(PixelScreenRect.Bottom, rect.Bottom));
            return width <= 0 || height <= 0 ? null : RenderTargetPool.Rent(device, width, height);
        }

        /// <summary>
        ///     Composites <paramref name="content"/> (drawn at frame coordinates) into <paramref name="previousTargets"/>
        ///     with the bands of <paramref name="bands"/> blurred and tinted, clipped to <paramref name="clip"/>.
        ///     <paramref name="tintByCoverage"/>: tint only where the content draws (a layer over content that already has
        ///     its tint), not over its transparent parts.
        /// </summary>
        private void ComposeBlurred(SpriteBatch spriteBatch, ScreenEngine engine, RenderTarget2D content, in BlurBands bands,
            RenderTargetBinding[] previousTargets, Rect clip, bool tintByCoverage)
        {
            var device = spriteBatch.GraphicsDevice;
            var renderer = engine.ProgressiveBlurRenderer!;
            var width = content.Width;
            var height = content.Height;
            var rect = bands.Rect;

            // Bands in whole rows of the content texture, covering partly drawn edge rows; the bottom one never
            // reaches into the top one. Only the control's own columns blur, so its sides clamp instead of fading
            // into what lies beside it.
            var topEdge = (int)Math.Floor(rect.Top);
            var topLength = (int)Math.Round(bands.TopHeight);
            var bottomEdge = (int)Math.Ceiling(rect.Bottom);
            var bottomLength = (int)Math.Round(bands.BottomHeight);
            var top0 = Math.Clamp(topEdge, 0, height);
            var top1 = Math.Clamp(topEdge + topLength, top0, height);
            var bottom1 = Math.Clamp(bottomEdge, top1, height);
            var bottom0 = Math.Clamp(bottomEdge - bottomLength, top1, bottom1);
            var left = Math.Clamp((int)Math.Floor(rect.Left), 0, width);
            var right = Math.Clamp((int)Math.Ceiling(rect.Right), left, width);
            if (right <= left)
                top1 = top0 = bottom0 = bottom1;

            // Both bands blurred before the frame's target is bound again (a tiled GPU reloads it each time).
            var hasTop = top1 > top0;
            var hasBottom = bottom1 > bottom0;
            var bandTint = bands.Tints.Own ? Color.Transparent : bands.Tint;
            if (hasTop)
                hasTop = renderer.Prepare(spriteBatch, content, new Rectangle(left, top0, right - left, top1 - top0), topEdge, topLength, bands.Radius, bandTint, tintByCoverage, 0);
            if (hasBottom)
                hasBottom = renderer.Prepare(spriteBatch, content, new Rectangle(left, bottom0, right - left, bottom1 - bottom0), bottomEdge, bottomLength, bands.Radius, bandTint, tintByCoverage, 1);
            // A band the renderer could not blur (no shader) is drawn sharp with the rows around it.
            if (!hasTop)
                top1 = top0;
            if (!hasBottom)
                bottom0 = bottom1;

            if (previousTargets.Length == 0)
                device.SetRenderTarget(null);
            else
                device.SetRenderTargets(previousTargets);

            var previousScissor = device.ScissorRectangle;
            device.ScissorRectangle = ToScissorRectangle(clip);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, rasterizerState: ScissorRasterizer);
            DrawPart(spriteBatch, content, 0, 0, width, top0);
            if (hasTop)
                DrawBeside(spriteBatch, content, left, right, top0, top1);
            DrawPart(spriteBatch, content, 0, top1, width, bottom0);
            if (hasBottom)
                DrawBeside(spriteBatch, content, left, right, bottom0, bottom1);
            DrawPart(spriteBatch, content, 0, bottom1, width, height);
            spriteBatch.End();
            if (hasTop)
                renderer.Draw(spriteBatch, 0, ScissorRasterizer);
            if (hasBottom)
                renderer.Draw(spriteBatch, 1, ScissorRasterizer);
            renderer.Release();
            if (bands.Tints.Own && right > left)
            {
                // The tint rows start at the control's edges, independent of the bands' rows.
                var tints = bands.Tints;
                var tintTop = Math.Clamp(topEdge, 0, height);
                var tintBottom = Math.Clamp(bottomEdge, tintTop, height);
                var topRows = Math.Min((int)Math.Round(tints.TopHeight), tintBottom - tintTop);
                var bottomRows = Math.Min((int)Math.Round(tints.BottomHeight), tintBottom - tintTop);
                if (topRows > 0)
                    renderer.DrawTint(spriteBatch, content, new Rectangle(left, tintTop, right - left, topRows), top: true, tints.SolidHeight, bands.Tint, tintByCoverage, ScissorRasterizer);
                if (bottomRows > 0)
                    renderer.DrawTint(spriteBatch, content, new Rectangle(left, tintBottom - bottomRows, right - left, bottomRows), top: false, tints.SolidHeight, bands.Tint, tintByCoverage, ScissorRasterizer);
            }
            device.ScissorRectangle = previousScissor;
            engine.RecordBatchFlush();
        }

        /// <summary>The <see cref="ProgressiveBlur"/> around <paramref name="control"/> if it drew its bands this frame
        /// (flights and set-aside controls are drawn with them), else null.</summary>
        private static ProgressiveBlur? DrawnBlurAround(Control control, ScreenEngine engine)
        {
            for (var parent = control.Parent; parent != null; parent = parent.Parent)
            {
                if (parent is ProgressiveBlur blur)
                    return blur.DrawnBands is { } bands && bands.Frame == engine.DrawFrameNumber ? blur : null;
            }

            return null;
        }

        private static void DrawPart(SpriteBatch spriteBatch, Texture2D texture, int left, int top, int right, int bottom)
        {
            if (right > left && bottom > top)
                spriteBatch.Draw(texture, new Vector2(left, top), new Rectangle(left, top, right - left, bottom - top), Color.White);
        }

        // What the content draws beside a band's columns (overflow) stays sharp.
        private static void DrawBeside(SpriteBatch spriteBatch, Texture2D content, int left, int right, int top, int bottom)
        {
            DrawPart(spriteBatch, content, 0, top, left, bottom);
            DrawPart(spriteBatch, content, right, top, content.Width, bottom);
        }
    }

    /// <summary>Where a <see cref="ProgressiveBlur"/> was drawn in a frame and how its bands look, in pixels.</summary>
    internal readonly struct BlurBands
    {
        public BlurBands(Rect rect, float radius, float topHeight, float bottomHeight, Color tint, BlurTints tints, long frame)
        {
            Tints = tints;
            Rect = rect;
            Radius = radius;
            TopHeight = topHeight;
            BottomHeight = bottomHeight;
            Tint = tint;
            Frame = frame;
        }

        public Rect Rect { get; }
        public float Radius { get; }
        public float TopHeight { get; }
        public float BottomHeight { get; }
        /// <summary>Premultiplied, ancestor opacity included.</summary>
        public Color Tint { get; }
        public BlurTints Tints { get; }
        public long Frame { get; }
    }

    /// <summary>A <see cref="ProgressiveBlur"/>'s tint with its own extent (<see cref="Own"/>), in pixels; otherwise the
    /// tint follows the blur bands.</summary>
    internal readonly struct BlurTints
    {
        public BlurTints(bool own, float topHeight, float bottomHeight, float solidHeight)
        {
            Own = own;
            TopHeight = topHeight;
            BottomHeight = bottomHeight;
            SolidHeight = solidHeight;
        }

        public bool Own { get; }
        public float TopHeight { get; }
        public float BottomHeight { get; }
        public float SolidHeight { get; }
    }
}
