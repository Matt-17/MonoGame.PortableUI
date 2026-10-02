using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public enum ClipMode
    {
        /// <summary>Draw only inside the shape.</summary>
        Inside,
        /// <summary>Draw everywhere except inside the shape (cut a hole).</summary>
        Outside
    }

    /// <summary>
    ///     A non-rectangular clip applied to a control and everything it contains
    ///     (<see cref="Controls.Control.Clip"/>). Rendered with an offscreen layer plus a coverage
    ///     mask (no stencil buffer, no shader), so it works on every MonoGame backend; nested clips
    ///     intersect. Clipping affects drawing only — hit-testing still uses the rectangular bounds.
    /// </summary>
    public abstract class ClipShape
    {
        public ClipMode Mode { get; set; } = ClipMode.Inside;

        /// <summary>Draws the shape's coverage (opaque white where it applies) for <paramref name="bounds"/>.</summary>
        /// <param name="spriteBatch">Batch drawing into the mask target.</param>
        /// <param name="bounds">The control's rect in render space.</param>
        /// <param name="scale">Render scale of the control: layout lengths (radius, inset) scale with it.</param>
        internal abstract void DrawMask(SpriteBatch spriteBatch, Rect bounds, float scale);
    }

    /// <summary>Rounded rectangle covering the control's bounds, optionally inset.</summary>
    public sealed class RoundedRectClip : ClipShape
    {
        public RoundedRectClip()
        {
        }

        public RoundedRectClip(CornerRadius radius, ClipMode mode = ClipMode.Inside)
        {
            Radius = radius;
            Mode = mode;
        }

        public CornerRadius Radius { get; set; }

        /// <summary>Shrinks the shape inside the bounds (e.g. by a border's thickness).</summary>
        public Thickness Inset { get; set; }

        internal override void DrawMask(SpriteBatch spriteBatch, Rect bounds, float scale)
        {
            var rect = bounds - new Thickness(Inset.Left * scale, Inset.Top * scale, Inset.Right * scale, Inset.Bottom * scale);
            if (rect.Width <= 0 || rect.Height <= 0)
                return;
            if (Radius.IsEmpty)
                spriteBatch.Draw(Primitives.Pixel(spriteBatch), rect, Color.White);
            else
                RoundedRectRenderer.DrawSolid(spriteBatch, rect,
                    new CornerRadius(Radius.TopLeft * scale, Radius.TopRight * scale, Radius.BottomRight * scale, Radius.BottomLeft * scale), Color.White);
        }
    }

    /// <summary>A <see cref="PathGeometry"/> mapped onto the control's bounds (its view box fills them).</summary>
    public sealed class PathClip : ClipShape
    {
        private Texture2D? _mask;
        private int _maskWidth;
        private int _maskHeight;
        private int _maskVersion = -1;
        private int _resetGeneration;

        public PathClip(PathGeometry geometry, ClipMode mode = ClipMode.Inside)
        {
            Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
            Mode = mode;
        }

        public PathGeometry Geometry { get; }

        internal override void DrawMask(SpriteBatch spriteBatch, Rect bounds, float scale)
        {
            var width = (int)Math.Ceiling(bounds.Width);
            var height = (int)Math.Ceiling(bounds.Height);
            if (width <= 0 || height <= 0)
                return;

            var device = spriteBatch.GraphicsDevice;
            var generation = PathRasterizer.GetResetGeneration(device);
            if (_mask == null || _mask.IsDisposed || width != _maskWidth || height != _maskHeight
                || _maskVersion != Geometry.Version || generation != _resetGeneration || !ReferenceEquals(_mask.GraphicsDevice, device))
            {
                _mask?.Dispose();
                _mask = PathRasterizer.CreateTexture(device, Geometry, width, height, Color.White);
                _maskWidth = width;
                _maskHeight = height;
                _maskVersion = Geometry.Version;
                _resetGeneration = generation;
            }

            spriteBatch.Draw(_mask, new Rectangle((int)bounds.Left, (int)bounds.Top, width, height), Color.White);
        }
    }
}
