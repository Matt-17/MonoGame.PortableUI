using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Media
{
    public sealed class ShadowStyle
    {
        public Color Color { get; set; } = new Color(0, 0, 0, 90);

        /// <summary>Overall shadow strength, multiplied on top of the color's alpha (0..1).</summary>
        public float Opacity { get; set; } = 1;

        public Vector2 Offset { get; set; } = new Vector2(0, 2);

        public float Blur { get; set; } = 4;

        public float Spread { get; set; }

        public bool Inset { get; set; }

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
