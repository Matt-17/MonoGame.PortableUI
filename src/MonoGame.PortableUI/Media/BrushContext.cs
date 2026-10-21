using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public readonly struct BrushContext
    {
        public BrushContext(
            Rect rect,
            CornerRadius radius,
            float opacity,
            GraphicsDevice device,
            float timeSeconds = 0,
            PointF? pointerPosition = null,
            float scale = 1)
        {
            Scale = scale > 0 ? scale : 1;
            Rect = rect;
            Radius = radius;
            Opacity = opacity;
            Device = device;
            TimeSeconds = timeSeconds;
            PointerPosition = pointerPosition;
        }

        public Rect Rect { get; }

        public CornerRadius Radius { get; }

        public float Opacity { get; }

        public GraphicsDevice Device { get; }

        public float TimeSeconds { get; }

        public PointF? PointerPosition { get; }

        /// <summary>Render pixels per layout unit (LayoutScale/HiDPI): brushes scale fixed sizes such as bevel lines by it.</summary>
        public float Scale { get; }

        private readonly Vector2 _motionScale;

        /// <summary>
        ///     How much motion (a Scale animation) scales the rect beyond its rest size; one at rest. Brushes with
        ///     size-keyed textures build them at <see cref="RestPixelWidth"/>/<see cref="RestPixelHeight"/> and stretch.
        /// </summary>
        internal Vector2 MotionScale
        {
            get => _motionScale == Vector2.Zero ? Vector2.One : _motionScale;
            init => _motionScale = value;
        }

        /// <summary>The rect's width in whole pixels at rest (without the motion scale).</summary>
        internal int RestPixelWidth => BrushTextureCache.PixelSize(Rect.Width / MotionScale.X);

        internal int RestPixelHeight => BrushTextureCache.PixelSize(Rect.Height / MotionScale.Y);

        /// <summary>The corner radius at rest (without the motion scale).</summary>
        internal CornerRadius RestRadius
        {
            get
            {
                var motion = MotionScale;
                var scale = Math.Min(motion.X, motion.Y);
                if (scale == 1 || Radius.IsEmpty)
                    return Radius;
                return new CornerRadius(Radius.TopLeft / scale, Radius.TopRight / scale, Radius.BottomRight / scale, Radius.BottomLeft / scale);
            }
        }
    }
}
