using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Text
{
    /// <summary>
    ///     SpriteFont text for controls in motion. <c>SpriteBatch.DrawString</c> draws every glyph as a quad
    ///     exactly the size of its ink: at a fractional position the filtered, half-covered pixel along
    ///     the quad's edge falls outside it and is cut, and each glyph pops a pixel when its edge crosses
    ///     a pixel centre - text shivers while it moves or scales. Here each glyph's quad takes the
    ///     transparent texel around its ink along (where the atlas has one), so its edges stay soft and
    ///     glide. Same layout as DrawString (bearings, spacing, line breaks).
    /// </summary>
    internal static class SubpixelSpriteText
    {
        private sealed class PaddedGlyphs
        {
            public readonly Dictionary<char, int> Index = new Dictionary<char, int>();
            public SpriteFont.Glyph[] Glyphs = Array.Empty<SpriteFont.Glyph>();
            public Rectangle[] Source = Array.Empty<Rectangle>();
            public Vector2[] Pad = Array.Empty<Vector2>();
            public int DefaultIndex = -1;
        }

        private static readonly ConditionalWeakTable<SpriteFont, PaddedGlyphs> Cache = new ConditionalWeakTable<SpriteFont, PaddedGlyphs>();

        public static void DrawString(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, Color color, Vector2 scale)
        {
            var glyphs = Cache.GetValue(font, Build);
            var state = new LineState();
            for (var i = 0; i < text.Length; i++)
                DrawChar(spriteBatch, font, glyphs, text[i], position, color, scale, ref state);
        }

        public static void DrawString(SpriteBatch spriteBatch, SpriteFont font, StringBuilder text, Vector2 position, Color color, Vector2 scale)
        {
            var glyphs = Cache.GetValue(font, Build);
            var state = new LineState();
            for (var i = 0; i < text.Length; i++)
                DrawChar(spriteBatch, font, glyphs, text[i], position, color, scale, ref state);
        }

        private struct LineState
        {
            public Vector2 Offset;
            public bool NotFirst;
        }

        private static void DrawChar(SpriteBatch spriteBatch, SpriteFont font, PaddedGlyphs glyphs, char c, Vector2 position, Color color, Vector2 scale, ref LineState state)
        {
            if (c == '\r')
                return;
            if (c == '\n')
            {
                state.Offset.X = 0;
                state.Offset.Y += font.LineSpacing;
                state.NotFirst = false;
                return;
            }

            if (!glyphs.Index.TryGetValue(c, out var index))
            {
                index = glyphs.DefaultIndex;
                if (index < 0)
                    return;
            }

            var glyph = glyphs.Glyphs[index];
            if (!state.NotFirst)
            {
                state.Offset.X = Math.Max(glyph.LeftSideBearing, 0);
                state.NotFirst = true;
            }
            else
            {
                state.Offset.X += font.Spacing + glyph.LeftSideBearing;
            }

            var source = glyphs.Source[index];
            if (source.Width > 0 && source.Height > 0)
            {
                var at = state.Offset + new Vector2(glyph.Cropping.X, glyph.Cropping.Y) - glyphs.Pad[index];
                spriteBatch.Draw(font.Texture, position + at * scale, source, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }

            state.Offset.X += glyph.Width + glyph.RightSideBearing;
        }

        // Grows each glyph's source rect by one texel on every side whose strip is free of other glyphs.
        private static PaddedGlyphs Build(SpriteFont font)
        {
            var glyphs = font.Glyphs;
            var result = new PaddedGlyphs
            {
                Glyphs = glyphs,
                Source = new Rectangle[glyphs.Length],
                Pad = new Vector2[glyphs.Length]
            };
            var texture = new Rectangle(0, 0, font.Texture.Width, font.Texture.Height);
            for (var i = 0; i < glyphs.Length; i++)
            {
                result.Index[glyphs[i].Character] = i;
                var bounds = glyphs[i].BoundsInTexture;
                var left = Free(glyphs, i, new Rectangle(bounds.Left - 1, bounds.Top - 1, 1, bounds.Height + 2), texture) ? 1 : 0;
                var right = Free(glyphs, i, new Rectangle(bounds.Right, bounds.Top - 1, 1, bounds.Height + 2), texture) ? 1 : 0;
                var top = Free(glyphs, i, new Rectangle(bounds.Left - 1, bounds.Top - 1, bounds.Width + 2, 1), texture) ? 1 : 0;
                var bottom = Free(glyphs, i, new Rectangle(bounds.Left - 1, bounds.Bottom, bounds.Width + 2, 1), texture) ? 1 : 0;
                result.Source[i] = new Rectangle(bounds.Left - left, bounds.Top - top, bounds.Width + left + right, bounds.Height + top + bottom);
                result.Pad[i] = new Vector2(left, top);
            }

            if (font.DefaultCharacter is { } fallback && result.Index.TryGetValue(fallback, out var defaultIndex))
                result.DefaultIndex = defaultIndex;
            return result;
        }

        private static bool Free(SpriteFont.Glyph[] glyphs, int self, Rectangle strip, Rectangle texture)
        {
            if (strip.Left < texture.Left || strip.Top < texture.Top || strip.Right > texture.Right || strip.Bottom > texture.Bottom)
                return false;
            for (var j = 0; j < glyphs.Length; j++)
            {
                if (j != self && glyphs[j].BoundsInTexture.Intersects(strip))
                    return false;
            }

            return true;
        }
    }
}
