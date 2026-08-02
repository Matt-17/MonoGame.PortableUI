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
            PointF? pointerPosition = null)
        {
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
    }
}
