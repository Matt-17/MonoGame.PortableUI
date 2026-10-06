using System;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    ///     Helpers for PortableUI's colour convention: every colour the API takes (brush colours,
    ///     <c>TextColor</c>, <c>Image.TintColor</c>, <see cref="ShadowStyle.Color"/>, theme colours) is
    ///     <b>straight alpha</b> - R, G, B are the full colour and A says how opaque it is. PortableUI
    ///     premultiplies when it draws.
    ///     <para>
    ///         MonoGame's <c>Color.White * 0.07f</c> scales all four channels, so it is already
    ///         <i>premultiplied</i>: passed to PortableUI it is multiplied a second time and comes out at
    ///         0.07 x 0.07 = 0.5 % instead of 7 % (and <c>Color.White * 0.94f</c> turns grey). Write
    ///         <c>Color.White.WithAlpha(0.07f)</c> instead, or convert with <see cref="ToStraightAlpha"/>.
    ///     </para>
    /// </summary>
    public static class ColorAlpha
    {
        /// <summary>The colour with its alpha replaced (straight alpha, 0..1); R, G, B stay untouched.</summary>
        public static Color WithAlpha(this Color color, float alpha)
            => new Color(color.R, color.G, color.B, (byte)MathF.Round(MathHelper.Clamp(alpha, 0f, 1f) * 255f));

        /// <summary>The colour with its alpha replaced (straight alpha, 0..255).</summary>
        public static Color WithAlpha(this Color color, byte alpha)
            => new Color(color.R, color.G, color.B, alpha);

        /// <summary>
        ///     Converts a premultiplied colour (MonoGame's <c>Color * float</c>) into the straight alpha
        ///     PortableUI expects: <c>(Color.White * 0.07f).ToStraightAlpha()</c> is white at 7 %.
        /// </summary>
        public static Color ToStraightAlpha(this Color premultiplied)
        {
            var a = premultiplied.A;
            if (a == 0)
                return Color.Transparent;
            if (a == 255)
                return premultiplied;
            return new Color(
                (byte)Math.Min(255, (premultiplied.R * 255 + a / 2) / a),
                (byte)Math.Min(255, (premultiplied.G * 255 + a / 2) / a),
                (byte)Math.Min(255, (premultiplied.B * 255 + a / 2) / a),
                a);
        }
    }
}
