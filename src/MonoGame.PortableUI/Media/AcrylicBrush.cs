using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Media
{
    public class AcrylicBrush : FrostedGlassBrush
    {
        public AcrylicBrush()
            : this(new Color(32, 32, 32, 204))
        {
        }

        public AcrylicBrush(Color tintColor)
            : base(tintColor, new Color(255, 255, 255, 96), 14, 0.08f)
        {
        }

        public float SaturationBoost { get; set; } = 0.18f;
        public float LuminosityBoost { get; set; } = 0.08f;
    }
}
