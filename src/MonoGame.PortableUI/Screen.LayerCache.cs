using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Effects;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI
{
    // CacheMode.Bitmap: draw a subtree into a texture once, then composite that texture.
    public abstract partial class Screen
    {
        /// <summary>Re-renders forced by animation requests in a row before a layer is drawn live.</summary>
        private const int MaxAnimatedRebuilds = 3;

        /// <summary>How long (frames) an animated layer is drawn live before caching is tried again.</summary>
        private const int AnimatedLiveFrames = 30;

        private const int MaxLayerSize = 4096;

        /// <summary>The control whose layer is being rendered (drawn normally inside its own layer).</summary>
        private Control? _layerInProgress;

        // Consecutive layer composites under the same clip share one batch (one Begin/End instead of
        // one per texture). Closed before any other drawing and before the scissor changes.
        private bool _compositeBatchOpen;
        private Rectangle _compositeScissor;

        private void FlushLayerComposites(SpriteBatch spriteBatch)
        {
            if (!_compositeBatchOpen)
                return;
            _compositeBatchOpen = false;
            spriteBatch.End();
            ScreenEngine?.RecordBatchFlush();
        }

        /// <summary>
        ///     Draws <paramref name="control"/> from its cached texture, re-rendering it first when it is
        ///     invalid. False when the control must be drawn live (no engine, uncacheable content, too
        ///     large, over budget, animated).
        /// </summary>
        private bool TryDrawLayer(SpriteBatch spriteBatch, Control control, RenderContext parentContext, RenderContext context)
        {
            var engine = ScreenEngine;
            if (engine == null)
                return false;
            if (!TryDrawLayerCore(spriteBatch, engine, control, parentContext, context))
            {
                FlushLayerComposites(spriteBatch); // drawn live next
                return false;
            }
            return true;
        }

        private bool TryDrawLayerCore(SpriteBatch spriteBatch, ScreenEngine engine, Control control, RenderContext parentContext, RenderContext context)
        {
            // A transient scale (overscroll stretch, popup zoom) changes every frame: re-rendering the
            // texture each time would cost more than drawing live.
            var baseScale = engine.ScalesNatively ? engine.RenderScale : 1f;
            if (Math.Abs(context.Scale.X - baseScale) > 0.0001f || Math.Abs(context.Scale.Y - baseScale) > 0.0001f)
                return false;
            var cache = control.LayerCache;
            if (cache != null && engine.DrawFrameNumber < cache.LiveUntilFrame)
            {
                cache.LastUsedFrame = engine.DrawFrameNumber;
                return false;
            }

            var device = spriteBatch.GraphicsDevice;
            var renderRect = context.RenderRect;
            var valid = cache?.Target is { IsDisposed: false, IsContentLost: false } target
                && ReferenceEquals(target.GraphicsDevice, device)
                && !cache.Dirty
                && cache.Generation == engine.LayerCacheGeneration
                && cache.ScaleX == context.Scale.X && cache.ScaleY == context.Scale.Y
                && ScreenSystem.TotalTime < cache.RefreshAt;
            int left, top;
            if (valid)
            {
                left = (int)Math.Floor(renderRect.Left + cache!.OffsetX);
                top = (int)Math.Floor(renderRect.Top + cache.OffsetY);
            }
            else
            {
                // Pixel bounds of the subtree, overflow (shadows, focus rings) included.
                if (!TryMeasureLayer(control, context, out var bounds, out var uncacheable) || uncacheable)
                    return false;
                // The size must not depend on the sub-pixel position, or scrolling by fractions of a
                // pixel would change it by one and force a re-render: one spare pixel covers any
                // fraction, the tolerance absorbs float noise in the transformed bounds.
                left = (int)Math.Floor(bounds.Left);
                top = (int)Math.Floor(bounds.Top);
                var width = (int)Math.Ceiling(bounds.Width - 0.01f) + 1;
                var height = (int)Math.Ceiling(bounds.Height - 0.01f) + 1;
                if (width <= 0 || height <= 0 || width > MaxLayerSize || height > MaxLayerSize)
                    return false;

                FlushLayerComposites(spriteBatch); // re-rendering switches render targets
                if (!RenderLayer(spriteBatch, engine, control, parentContext, left, top, width, height))
                    return false;
                control.LayerCache!.OffsetX = bounds.Left - renderRect.Left;
                control.LayerCache.OffsetY = bounds.Top - renderRect.Top;
            }

            cache = control.LayerCache!;
            cache.LastUsedFrame = engine.DrawFrameNumber;

            // Composite: the texture already carries the subtree's own clipping and opacity; only the
            // ancestors' clip and opacity apply.
            var scissor = ToScissorRectangle(parentContext.ChildClipRect);
            if (!_compositeBatchOpen || scissor != _compositeScissor)
            {
                FlushLayerComposites(spriteBatch);
                device.ScissorRectangle = scissor;
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, rasterizerState: ScissorRasterizer);
                _compositeBatchOpen = true;
                _compositeScissor = scissor;
            }
            spriteBatch.Draw(cache.Target!, new Vector2(left, top), Color.White * parentContext.Opacity);
            return true;
        }

        private bool RenderLayer(SpriteBatch spriteBatch, ScreenEngine engine, Control control, RenderContext parentContext, int left, int top, int width, int height)
        {
            var cache = engine.AcquireLayerCache(control);
            var device = spriteBatch.GraphicsDevice;
            var pixels = (long)width * height;
            if (cache.Target == null || cache.Target.IsDisposed || !FitsLayer(cache.Width, width) || !FitsLayer(cache.Height, height)
                || !ReferenceEquals(cache.Target.GraphicsDevice, device))
            {
                if (!engine.LayerCacheFits(cache, pixels))
                    return false;
                engine.TrackLayerCachePixels(pixels - cache.Pixels);
                cache.Dispose();
                cache.Target = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
                cache.Width = width;
                cache.Height = height;
            }

            RenderTargetBinding[]? buffer = null; // per call: layers nest
            var previousTargets = RenderTargetHelper.SnapshotRenderTargets(device, ref buffer);
            var oldScissor = device.ScissorRectangle;
            var requestsBefore = engine.AnimationFrameRequests;
            var outerEarliest = engine.EarliestAnimationFrameAt;
            engine.EarliestAnimationFrameAt = TimeSpan.MaxValue;
            var outerLayer = _layerInProgress;
            _layerInProgress = control;
            try
            {
                device.SetRenderTarget(cache.Target);
                device.Clear(Color.Transparent);
                device.ScissorRectangle = new Rectangle(0, 0, cache.Width, cache.Height);
                DrawControlBatched(spriteBatch, control, parentContext.ForLayer(-left, -top, cache.Width, cache.Height));
            }
            finally
            {
                _layerInProgress = outerLayer;
                if (previousTargets.Length == 0)
                    device.SetRenderTarget(null);
                else
                    device.SetRenderTargets(previousTargets);
                device.ScissorRectangle = oldScissor;
            }

            var earliest = engine.EarliestAnimationFrameAt;
            // Requests raised inside also concern any layer this one is drawn into.
            engine.EarliestAnimationFrameAt = earliest < outerEarliest ? earliest : outerEarliest;
            var animated = engine.AnimationFrameRequests != requestsBefore;
            cache.Dirty = animated;
            cache.RefreshAt = earliest;
            cache.Generation = engine.LayerCacheGeneration;
            cache.ScaleX = parentContext.Scale.X * control.Scale.X;
            cache.ScaleY = parentContext.Scale.Y * control.Scale.Y;
            cache.AnimatedRebuilds = animated ? cache.AnimatedRebuilds + 1 : 0;
            if (cache.AnimatedRebuilds >= MaxAnimatedRebuilds)
            {
                cache.AnimatedRebuilds = 0;
                cache.LiveUntilFrame = engine.DrawFrameNumber + AnimatedLiveFrames;
            }
            engine.RecordBatchFlush();
            return true;
        }

        /// <summary>
        ///     Union of the subtree's render rects with their overflow, in pixels; and whether something
        ///     inside must be drawn live (glass sampling the backdrop, post-effect islands, overscroll
        ///     stretch). Clip shapes are fine: they compose in the coordinates of the current target.
        /// </summary>
        private static bool TryMeasureLayer(Control control, RenderContext context, out Rect bounds, out bool uncacheable)
        {
            uncacheable = false;
            var layout = Rect.Empty;
            var hasLayout = false;
            AccumulateLayer(control, true, ref layout, ref hasLayout, ref uncacheable);
            bounds = hasLayout ? context.ToRender(layout) : Rect.Empty;
            return hasLayout;
        }

        private static void AccumulateLayer(Control control, bool addToLayout, ref Rect layout, ref bool hasLayout, ref bool uncacheable)
        {
            if (!control.IsVisible || control.IsGone || uncacheable)
                return;
            if (control.BackgroundBrush is { RequiresBackdrop: true }
                || control is ThemeIsland { Theme.PostEffects.Count: > 0 }
                || control.OverscrollScale != Vector2.One)
            {
                uncacheable = true;
                return;
            }

            if (addToLayout)
            {
                var overflow = RenderContext.VisualOverflow(control);
                var rect = overflow > 0 ? control.ClippingRect + new Thickness(overflow) : control.ClippingRect;
                if (rect.Width > 0 && rect.Height > 0)
                {
                    layout = hasLayout ? Union(layout, rect) : rect;
                    hasLayout = true;
                }
            }

            // A control that clips its content keeps its children inside its own rect; they are still
            // checked for content that must be drawn live.
            var childrenAdd = addToLayout && !control.ClipsDescendants;
            var count = control.VisualChildCount;
            for (var i = 0; i < count; i++)
                AccumulateLayer(control.GetVisualChild(i), childrenAdd, ref layout, ref hasLayout, ref uncacheable);
        }

        /// <summary>A texture a pixel or two larger is reused rather than re-created.</summary>
        private static bool FitsLayer(int cached, int needed) => cached >= needed && cached <= needed + 2;

        private static Rect Union(Rect a, Rect b)
        {
            var left = Math.Min(a.Left, b.Left);
            var top = Math.Min(a.Top, b.Top);
            return new Rect(left, top, Math.Max(a.Right, b.Right) - left, Math.Max(a.Bottom, b.Bottom) - top);
        }
    }
}
