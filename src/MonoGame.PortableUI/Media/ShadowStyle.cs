using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Media
{
    public sealed class ShadowStyle
    {
        /// <summary>Shadow colour, straight alpha (<c>Color.Black.WithAlpha(0.35f)</c>, not <c>Color.Black * 0.35f</c>;
        /// see <see cref="ColorAlpha"/>).</summary>
        public Color Color { get; set; } = new Color(0, 0, 0, 90);

        /// <summary>Overall shadow strength, multiplied on top of the color's alpha (0..1).</summary>
        public float Opacity { get; set; } = 1;

        public Vector2 Offset { get; set; } = new Vector2(0, 2);

        public float Blur { get; set; } = 4;

        public float Spread { get; set; }

        public bool Inset { get; set; }

        /// <summary>
        ///     A further shadow drawn with this one (chain for several): e.g. neumorphism's light
        ///     top-left plus dark bottom-right pair.
        /// </summary>
        public ShadowStyle? Also { get; set; }

        public static ShadowStyle Level1()
        {
            return new ShadowStyle { Color = new Color(0, 0, 0, 70), Offset = new Vector2(0, 2), Blur = 4 };
        }

        public static ShadowStyle Level2()
        {
            return new ShadowStyle { Color = new Color(0, 0, 0, 85), Offset = new Vector2(0, 4), Blur = 8 };
        }

        public static ShadowStyle Level3()
        {
            return new ShadowStyle { Color = new Color(0, 0, 0, 100), Offset = new Vector2(0, 8), Blur = 14 };
        }
    }
}
