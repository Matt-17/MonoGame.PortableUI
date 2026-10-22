using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Effects;

namespace MonoGame.PortableUI
{
    // A control drawn into an image instead of onto the screen: a card to share, a QR code to save.
    public abstract partial class Screen
    {
        /// <summary>
        ///     Draws <paramref name="content"/>, which must not be in a tree, alone into a PNG: laid out at
        ///     <paramref name="width"/> (layout units) and as tall as it wants to be, in this screen's theme and fonts, at
        ///     <paramref name="scale"/> pixels per unit (0: as the screen is drawn). Uncovered parts stay transparent: give
        ///     the content a background. Backdrop brushes (glass, progressive blur) have no backdrop here. Game thread only.
        /// </summary>
        internal byte[] RenderToPng(GraphicsDevice device, Control content, float width, float scale)
        {
            if (content.Parent != null)
                throw new InvalidOperationException("The content is already in a tree.");
            if (!(width > 0))
                throw new ArgumentOutOfRangeException(nameof(width));
            if (!(scale > 0))
                scale = ScreenEngine?.DrawScale ?? 1f;

            content.Parent = this;
            try
            {
                // Wrapping text learns its width when arranged: measure, arrange, and again while the height changes.
                var height = 0f;
                for (var pass = 0; pass < 3; pass++)
                {
                    var wanted = content.Measure().Height;
                    var measured = float.IsFinite(wanted) && wanted > 0 ? wanted : 1f;
                    if (pass > 0 && Math.Abs(measured - height) < 0.5f)
                        break;
                    height = measured;
                    content.UpdateLayout(new Rect(0, 0, width, height));
                }

                var pixelWidth = (int)Math.Ceiling(width * scale);
                var pixelHeight = (int)Math.Ceiling(height * scale);
                if (pixelWidth > MaxLayerSize || pixelHeight > MaxLayerSize)
                    throw new InvalidOperationException($"The image would be {pixelWidth}×{pixelHeight} pixels, more than {MaxLayerSize}.");

                var previousTargets = RenderTargetHelper.SnapshotRenderTargets(device);
                var previousScissor = device.ScissorRectangle;
                var previousViewport = device.Viewport;
                using var target = new RenderTarget2D(device, pixelWidth, pixelHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                using var spriteBatch = new SpriteBatch(device);
                try
                {
                    device.SetRenderTarget(target);
                    device.Clear(Color.Transparent);
                    var root = RenderContext.Root(new Rect(0, 0, width, height), scale);
                    device.ScissorRectangle = ToScissorRectangle(root.ScissorRect);
                    DrawControlBatched(spriteBatch, content, root);
                    FlushLayerComposites(spriteBatch);
                }
                finally
                {
                    if (previousTargets.Length == 0)
                        device.SetRenderTarget(null);
                    else
                        device.SetRenderTargets(previousTargets);
                    device.Viewport = previousViewport;
                    device.ScissorRectangle = previousScissor;
                }

                using var stream = new MemoryStream();
                target.SaveAsPng(stream, pixelWidth, pixelHeight);
                return stream.ToArray();
            }
            finally
            {
                content.Parent = null;
                // The screen itself did not change.
                ScreenEngine?.RequestRedraw();
            }
        }
    }
}
