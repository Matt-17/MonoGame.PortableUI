using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    ///     A rounded fill that is smooth away from its corners (a gradient) as a 9-slice: the corner
    ///     squares keep their element pixels (and the corner mask), the bands between them are
    ///     <see cref="Band"/> texels stretched to the element's size. A 992x330 card with 24 px corners
    ///     becomes a 112x112 texture instead of an element-sized one. The fill must be linear (or close
    ///     to it) across the bands - stretching interpolates between the texels.
    /// </summary>
    internal readonly struct StretchedCorners
    {
        internal const int Band = 64;

        private StretchedCorners(int width, int height, int left, int right, int top, int bottom, CornerRadius radius)
        {
            Width = width;
            Height = height;
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
            Radius = radius;
            TextureWidth = width > left + right + Band ? left + right + Band : width;
            TextureHeight = height > top + bottom + Band ? top + bottom + Band : height;
        }

        /// <summary>The element's size in pixels the fill is computed for.</summary>
        public int Width { get; }

        public int Height { get; }

        public int Left { get; }

        public int Right { get; }

        public int Top { get; }

        public int Bottom { get; }

        /// <summary>The radius clamped to the element.</summary>
        public CornerRadius Radius { get; }

        public int TextureWidth { get; }

        public int TextureHeight { get; }

        public static StretchedCorners For(int width, int height, CornerRadius radius)
        {
            radius = RoundedRectRenderer.Clamp(radius, new Rect(0, 0, width, height));
            return new StretchedCorners(
                width,
                height,
                Ceiling(Math.Max(radius.TopLeft, radius.BottomLeft)),
                Ceiling(Math.Max(radius.TopRight, radius.BottomRight)),
                Ceiling(Math.Max(radius.TopLeft, radius.TopRight)),
                Ceiling(Math.Max(radius.BottomLeft, radius.BottomRight)),
                radius);
        }

        /// <summary>The element pixel (index, as in an element-sized texture) texel column <paramref name="x"/> stands for.</summary>
        public float ElementX(int x) => ElementCoordinate(x, Width, TextureWidth, Left, Right);

        public float ElementY(int y) => ElementCoordinate(y, Height, TextureHeight, Top, Bottom);

        /// <summary>Masks the corners of texture data laid out as this 9-slice.</summary>
        public void ApplyCornerMask(Color[] data)
        {
            RoundedRectRenderer.ApplyCornerMask(data, TextureWidth, TextureHeight, Radius);
        }

        /// <summary>Draws the texture onto <paramref name="rect"/>, which the element pixels span (as an element-sized texture would).</summary>
        public void Draw(SpriteBatch spriteBatch, Texture2D texture, Rect rect, Color color)
        {
            // Snapped to whole pixels like the element-sized texture drawn onto the Rect (its implicit Rectangle).
            Rectangle target = rect;
            if (TextureWidth == Width && TextureHeight == Height)
            {
                spriteBatch.Draw(texture, target, color);
                return;
            }

            var scaleX = target.Width / (float)Width;
            var scaleY = target.Height / (float)Height;
            for (var row = 0; row < 3; row++)
            {
                if (!Slice(row, Height, TextureHeight, Top, Bottom, out var sourceY, out var sourceHeight, out var elementY, out var elementHeight))
                    continue;
                for (var column = 0; column < 3; column++)
                {
                    if (!Slice(column, Width, TextureWidth, Left, Right, out var sourceX, out var sourceWidth, out var elementX, out var elementWidth))
                        continue;
                    spriteBatch.Draw(
                        texture,
                        new Vector2(target.X + elementX * scaleX, target.Y + elementY * scaleY),
                        new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                        color,
                        0,
                        Vector2.Zero,
                        new Vector2(elementWidth * scaleX / sourceWidth, elementHeight * scaleY / sourceHeight),
                        SpriteEffects.None,
                        0);
                }
            }
        }

        private static bool Slice(int index, int size, int textureSize, int start, int end, out int source, out int sourceSize, out int element, out int elementSize)
        {
            if (textureSize == size)
            {
                // Not stretched on this axis: one slice covers it.
                source = element = 0;
                sourceSize = elementSize = size;
                return index == 0;
            }

            switch (index)
            {
                case 0:
                    source = element = 0;
                    sourceSize = elementSize = start;
                    break;
                case 1:
                    source = element = start;
                    sourceSize = Band;
                    elementSize = size - start - end;
                    break;
                default:
                    source = start + Band;
                    element = size - end;
                    sourceSize = elementSize = end;
                    break;
            }

            return sourceSize > 0;
        }

        private static float ElementCoordinate(int texel, int size, int textureSize, int start, int end)
        {
            if (textureSize == size || texel < start)
                return texel;
            if (texel >= start + Band)
                return size - end + (texel - start - Band);
            // A band texel's centre lands where the stretched slice samples it.
            var stretch = (size - start - end) / (float)Band;
            return start + (texel - start + 0.5f) * stretch - 0.5f;
        }

        private static int Ceiling(float value) => (int)Math.Ceiling(value);
    }
}
