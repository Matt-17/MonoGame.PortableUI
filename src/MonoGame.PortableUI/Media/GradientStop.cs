using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Media
{
    public readonly struct GradientStop
    {
        public GradientStop(float offset, Color color)
        {
            Offset = MathHelper.Clamp(offset, 0, 1);
            Color = color;
        }

        public float Offset { get; }

        public Color Color { get; }
    }
}
