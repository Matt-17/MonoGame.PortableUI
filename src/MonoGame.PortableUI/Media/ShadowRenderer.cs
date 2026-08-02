using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    internal static class ShadowRenderer
    {
        public static void Draw(SpriteBatch spriteBatch, Rect rect, ShadowStyle shadow, float opacity)
        {
            Draw(spriteBatch, rect, new CornerRadius(0), shadow, opacity);
        }

        public static void Draw(SpriteBatch spriteBatch, Rect rect, CornerRadius radius, ShadowStyle shadow, float opacity)
        {
            foreach (var layer in GetShadowLayers(rect, shadow))
            {
                var expansion = shadow.Inset ? 0 : Math.Max(0, (layer.Rect.Width - rect.Width) / 2);
                RoundedRectRenderer.DrawSolid(spriteBatch, layer.Rect, Expand(radius, expansion), Brush.ApplyOpacity(layer.Color, opacity));
            }
        }

        internal static IEnumerable<ShadowLayer> GetShadowLayers(Rect rect, ShadowStyle shadow)
        {
            if (shadow == null || shadow.Color.A == 0 || shadow.Opacity <= 0)
                yield break;

            var strength = MathHelper.Clamp(shadow.Opacity, 0, 1);
            var alpha = shadow.Color.A * strength;
            var blur = Math.Max(0, shadow.Blur);
            var spread = Math.Max(0, shadow.Spread);
            if (blur <= 0)
            {
                var flat = new Color(shadow.Color.R, shadow.Color.G, shadow.Color.B, (byte)MathHelper.Clamp(alpha, 0, 255));
                yield return new ShadowLayer(ApplyShadowRect(rect, shadow.Offset, spread, shadow.Inset), flat);
                yield break;
            }

            var layers = Math.Max(2, Math.Min(10, (int)Math.Ceiling(blur / 2)));
            var weightSum = 0f;
            for (var i = 0; i < layers; i++)
            {
                var t = i / (float)(layers - 1);
                weightSum += (1 - t) * (1 - t);
            }

            for (var i = 0; i < layers; i++)
            {
                var t = i / (float)(layers - 1);
                var weight = (1 - t) * (1 - t);
                var color = new Color(shadow.Color.R, shadow.Color.G, shadow.Color.B, (byte)MathHelper.Clamp(alpha * weight / weightSum, 0, 255));
                var expansion = spread + blur * t;
                yield return new ShadowLayer(ApplyShadowRect(rect, shadow.Offset, expansion, shadow.Inset), color);
            }
        }

        private static CornerRadius Expand(CornerRadius radius, float expansion)
        {
            if (radius.IsEmpty || expansion <= 0)
                return radius;

            return new CornerRadius(
                radius.TopLeft > 0 ? radius.TopLeft + expansion : 0,
                radius.TopRight > 0 ? radius.TopRight + expansion : 0,
                radius.BottomRight > 0 ? radius.BottomRight + expansion : 0,
                radius.BottomLeft > 0 ? radius.BottomLeft + expansion : 0);
        }

        private static Rect ApplyShadowRect(Rect rect, Vector2 offset, float expansion, bool inset)
        {
            if (inset)
                return new Rect(rect.Left + expansion, rect.Top + expansion, Math.Max(0, rect.Width - expansion * 2), Math.Max(0, rect.Height - expansion * 2));

            return new Rect(
                rect.Left + offset.X - expansion,
                rect.Top + offset.Y - expansion,
                rect.Width + expansion * 2,
                rect.Height + expansion * 2);
        }

        internal readonly struct ShadowLayer
        {
            public ShadowLayer(Rect rect, Color color)
            {
                Rect = rect;
                Color = color;
            }

            public Rect Rect { get; }

            public Color Color { get; }
        }
    }
}
