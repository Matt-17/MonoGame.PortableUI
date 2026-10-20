using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Effects;

namespace MonoGame.PortableUI
{
    /// <summary>A control of a shared-element flight drawn once at rest; the flight moves and scales the texture.</summary>
    internal sealed class FlightSnapshot
    {
        public FlightSnapshot(RenderTarget2D target, Rect layoutRect)
        {
            Target = target;
            LayoutRect = layoutRect;
        }

        public RenderTarget2D Target { get; }

        /// <summary>What the texture shows, in layout units: the control at rest with its shadow and ink.</summary>
        public Rect LayoutRect { get; }
    }

    // Shared-element flights draw snapshots: re-drawing a control at a new size every frame made brushes, corner
    // masks and shadows build new textures for each in-between size (10-25 ms per flying card on a phone).
    public abstract partial class Screen
    {
        private Control? _snapshotRoot;
        private RenderTargetBinding[]? _snapshotPreviousTargets;

        /// <summary>
        ///     Draws <paramref name="control"/> from its flight snapshot (taken on first use), placed by
        ///     <paramref name="placement"/> (layout units). False when it cannot be snapshotted (then draw it live).
        /// </summary>
        private bool DrawFlightSnapshot(SpriteBatch spriteBatch, ScreenEngine engine, RenderContext root, Control control, Matrix placement, float opacity)
        {
            if (opacity <= 0)
                return true;
            var snapshot = engine.GetFlightSnapshot(control) ?? CaptureFlightSnapshot(spriteBatch, engine, root, control);
            if (snapshot == null)
                return false;

            var scale = engine.DrawScale;
            var dest = RenderContext.TransformRect(snapshot.LayoutRect, placement);
            var target = snapshot.Target;
            FlushLayerComposites(spriteBatch);
            var device = spriteBatch.GraphicsDevice;
            device.ScissorRectangle = ToScissorRectangle(root.ScissorRect);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, ScissorRasterizer);
            spriteBatch.Draw(target, new Vector2(dest.Left * scale, dest.Top * scale), null, Color.White * opacity, 0, Vector2.Zero,
                new Vector2(dest.Width * scale / target.Width, dest.Height * scale / target.Height), SpriteEffects.None, 0);
            spriteBatch.End();
            engine.RecordBatchFlush();
            return true;
        }

        private FlightSnapshot? CaptureFlightSnapshot(SpriteBatch spriteBatch, ScreenEngine engine, RenderContext root, Control control)
        {
            var overflow = RenderContext.VisualOverflow(control);
            var rest = control.ClippingRect + overflow;
            var scale = engine.DrawScale;
            var width = (int)Math.Ceiling(rest.Width * scale);
            var height = (int)Math.Ceiling(rest.Height * scale);
            if (width <= 0 || height <= 0 || width > MaxLayerSize || height > MaxLayerSize)
                return null;

            var device = spriteBatch.GraphicsDevice;
            FlushLayerComposites(spriteBatch);
            var previousTargets = RenderTargetHelper.SnapshotRenderTargets(device, ref _snapshotPreviousTargets);
            var previousScissor = device.ScissorRectangle;
            var target = RenderTargetPool.Rent(device, width, height);
            device.SetRenderTarget(target);
            device.Clear(Color.Transparent);

            // At rest: without the flight's own transform, and without shared elements nested in it that fly on their own.
            var role = control.SharedFlight;
            control.SharedFlight = SharedFlightRole.None;
            control.RenderTransformSuppressed = true;
            _snapshotRoot = control;
            try
            {
                DrawControlBatched(spriteBatch, control, root.Translated(-rest.Left * scale, -rest.Top * scale, width, height));
                FlushLayerComposites(spriteBatch);
            }
            finally
            {
                _snapshotRoot = null;
                control.RenderTransformSuppressed = false;
                control.SharedFlight = role;
            }

            if (previousTargets.Length == 0)
                device.SetRenderTarget(null);
            else
                device.SetRenderTargets(previousTargets);
            device.ScissorRectangle = previousScissor;

            var snapshot = new FlightSnapshot(target, rest);
            engine.StoreFlightSnapshot(control, snapshot);
            return snapshot;
        }

        /// <summary>While a snapshot is drawn: a nested control that flies on its own is left out of it.</summary>
        private bool SkipsInSnapshot(Control control)
            => _snapshotRoot != null && !ReferenceEquals(control, _snapshotRoot) && control.SharedFlight == SharedFlightRole.Flying;
    }
}
