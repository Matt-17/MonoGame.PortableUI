using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Effects;

namespace MonoGame.PortableUI
{
    // ProgressiveBlur: the content goes into a texture; the rows outside the bands are composited sharp, the bands
    // through the core ProgressiveBlurRenderer (if its shader cannot load, reported once, the content draws sharp).
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
            if (rect.Width <= 0 || rect.Height <= 0 || radius < 1 || (topHeight < 1 && bottomHeight < 1)
                || engine.ProgressiveBlurRenderer is not { } renderer)
                return false;

            var device = spriteBatch.GraphicsDevice;
            // Full-frame target so the subtree keeps drawing at its own coordinates.
            var width = (int)Math.Ceiling(Math.Max(PixelScreenRect.Right, rect.Right));
            var height = (int)Math.Ceiling(Math.Max(PixelScreenRect.Bottom, rect.Bottom));
            if (width <= 0 || height <= 0)
                return false;

            var previousTargets = RenderTargetHelper.SnapshotRenderTargets(device, ref _blurPreviousTargets);
            var previousScissor = device.ScissorRectangle;
            var content = RenderTargetPool.Rent(device, width, height);
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

            // Bands in whole rows of the content texture, covering partly drawn edge rows; the bottom one never
            // reaches into the top one. Only the control's own columns blur, so its sides clamp instead of fading
            // into what lies beside it.
            var topEdge = (int)Math.Floor(rect.Top);
            var topLength = (int)Math.Round(topHeight);
            var bottomEdge = (int)Math.Ceiling(rect.Bottom);
            var bottomLength = (int)Math.Round(bottomHeight);
            var top0 = Math.Clamp(topEdge, 0, height);
            var top1 = Math.Clamp(topEdge + topLength, top0, height);
            var bottom1 = Math.Clamp(bottomEdge, top1, height);
            var bottom0 = Math.Clamp(bottomEdge - bottomLength, top1, bottom1);
            var left = Math.Clamp((int)Math.Floor(rect.Left), 0, width);
            var right = Math.Clamp((int)Math.Ceiling(rect.Right), left, width);
            if (right <= left)
                top1 = top0 = bottom0 = bottom1;

            var tint = control.TintColor * (control.TintColor.A / 255f * control.TintOpacity * parentContext.Opacity);
            tint.A = (byte)Math.Round(control.TintColor.A * control.TintOpacity * parentContext.Opacity);
            // Both bands blurred before the frame's target is bound again (a tiled GPU reloads it each time).
            var hasTop = top1 > top0;
            var hasBottom = bottom1 > bottom0;
            if (hasTop)
                hasTop = renderer.Prepare(spriteBatch, content, new Rectangle(left, top0, right - left, top1 - top0), topEdge, topLength, radius, tint, 0);
            if (hasBottom)
                hasBottom = renderer.Prepare(spriteBatch, content, new Rectangle(left, bottom0, right - left, bottom1 - bottom0), bottomEdge, bottomLength, radius, tint, 1);
            // A band the renderer could not blur (no shader) is drawn sharp with the rows around it.
            if (!hasTop)
                top1 = top0;
            if (!hasBottom)
                bottom0 = bottom1;

            if (previousTargets.Length == 0)
                device.SetRenderTarget(null);
            else
                device.SetRenderTargets(previousTargets);

            device.ScissorRectangle = ToScissorRectangle(parentContext.ChildClipRect);
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
            device.ScissorRectangle = previousScissor;
            engine.RecordBatchFlush();

            RenderTargetPool.Return(device, content);
            return true;
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
}
