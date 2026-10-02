using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    /// <summary>Which corners of a <see cref="ChamferBrush"/> are cut.</summary>
    [Flags]
    public enum ChamferCorners
    {
        None = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomRight = 4,
        BottomLeft = 8,
        Diagonal = TopLeft | BottomRight,
        All = TopLeft | TopRight | BottomRight | BottomLeft
    }

    /// <summary>
    ///     A panel/button face with 45° cut corners — the sci-fi/tactical game HUD shape. Optional
    ///     vertical gradient, frame and a glowing accent bar along one edge. Drawn as rows of solid
    ///     rects: device free, no textures, crisp at any scale (sizes are in layout units).
    /// </summary>
    public sealed class ChamferBrush : Brush
    {
        public ChamferBrush(Color top, Color bottom, Color border, float chamfer = 8, float borderThickness = 1, ChamferCorners corners = ChamferCorners.Diagonal)
        {
            Top = top;
            Bottom = bottom;
            Border = border;
            Chamfer = chamfer;
            BorderThickness = borderThickness;
            Corners = corners;
        }

        public ChamferBrush(Color fill, Color border, float chamfer = 8, float borderThickness = 1, ChamferCorners corners = ChamferCorners.Diagonal)
            : this(fill, fill, border, chamfer, borderThickness, corners)
        {
        }

        public Color Top { get; }
        public Color Bottom { get; }
        public Color Border { get; }
        public float Chamfer { get; }
        public float BorderThickness { get; }
        public ChamferCorners Corners { get; }

        /// <summary>Accent bar color drawn along the left edge inside the frame; null = none.</summary>
        public Color? Accent { get; init; }

        /// <summary>Accent bar width in layout units.</summary>
        public float AccentWidth { get; init; } = 3;

        public override void Draw(SpriteBatch spriteBatch, Rect rect) => Draw(spriteBatch, rect, 1);

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity) => Draw(spriteBatch, rect, opacity, 1);

        public override void Draw(SpriteBatch spriteBatch, in BrushContext context) => Draw(spriteBatch, context.Rect, context.Opacity, context.Scale);

        private void Draw(SpriteBatch spriteBatch, Rect rect, float opacity, float scale)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;
            var chamfer = MathF.Min(MathF.Round(Chamfer * scale), MathF.Min(rect.Width, rect.Height) / 2);
            var border = BorderThickness > 0 ? MathF.Max(1, MathF.Round(BorderThickness * scale)) : 0;
            if (border > 0 && Border.A > 0)
            {
                FillShape(spriteBatch, rect, chamfer, ApplyOpacity(Border, opacity), ApplyOpacity(Border, opacity));
                // Inner shape: the diagonal moves in by border·(√2 − 1) to keep the frame even.
                var inner = new Rect(rect.Left + border, rect.Top + border, rect.Width - 2 * border, rect.Height - 2 * border);
                FillShape(spriteBatch, inner, MathF.Max(0, chamfer - MathF.Round(border * 0.41f)), ApplyOpacity(Top, opacity), ApplyOpacity(Bottom, opacity));
                rect = inner;
                chamfer = MathF.Max(0, chamfer - MathF.Round(border * 0.41f));
            }
            else
            {
                FillShape(spriteBatch, rect, chamfer, ApplyOpacity(Top, opacity), ApplyOpacity(Bottom, opacity));
            }

            if (Accent is { } accent && rect.Height > 2 * chamfer)
            {
                var width = MathF.Max(1, MathF.Round(AccentWidth * scale));
                var top = (Corners & ChamferCorners.TopLeft) != 0 ? chamfer : 0;
                var bottom = (Corners & ChamferCorners.BottomLeft) != 0 ? chamfer : 0;
                spriteBatch.Draw(Primitives.Pixel(spriteBatch), new Rect(rect.Left, rect.Top + top, width, rect.Height - top - bottom), ApplyOpacity(accent, opacity));
            }
        }

        /// <summary>Fills this brush's outline with one color (hover/pressed overlays that must keep the cut corners).</summary>
        public void DrawShape(SpriteBatch spriteBatch, in BrushContext context, Color color)
        {
            var rect = context.Rect;
            if (rect.Width <= 0 || rect.Height <= 0)
                return;
            var chamfer = MathF.Min(MathF.Round(Chamfer * context.Scale), MathF.Min(rect.Width, rect.Height) / 2);
            var premultiplied = ApplyOpacity(color, context.Opacity);
            FillShape(spriteBatch, rect, chamfer, premultiplied, premultiplied);
        }

        /// <summary>A corner radius approximating the cut corners, for shadows/glows drawn behind the shape.</summary>
        public CornerRadius ShadowRadius(float scale)
        {
            var r = Chamfer * scale;
            return new CornerRadius(
                (Corners & ChamferCorners.TopLeft) != 0 ? r : 0,
                (Corners & ChamferCorners.TopRight) != 0 ? r : 0,
                (Corners & ChamferCorners.BottomRight) != 0 ? r : 0,
                (Corners & ChamferCorners.BottomLeft) != 0 ? r : 0);
        }

        private void FillShape(SpriteBatch spriteBatch, Rect rect, float chamfer, Color top, Color bottom)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;
            var pixel = Primitives.Pixel(spriteBatch);
            var rows = (int)chamfer;
            var gradient = top != bottom;
            // Cut rows: one strip per pixel row, inset by the remaining diagonal.
            for (var i = 0; i < rows; i++)
            {
                var inset = rows - i;
                DrawRow(spriteBatch, pixel, rect, rect.Top + i, inset, ChamferCorners.TopLeft, ChamferCorners.TopRight, Lerp(top, bottom, rect, rect.Top + i, gradient));
                DrawRow(spriteBatch, pixel, rect, rect.Bottom - 1 - i, inset, ChamferCorners.BottomLeft, ChamferCorners.BottomRight, Lerp(top, bottom, rect, rect.Bottom - 1 - i, gradient));
            }

            var middleTop = rect.Top + rows;
            var middleHeight = rect.Height - 2 * rows;
            if (middleHeight <= 0)
                return;
            if (!gradient)
            {
                spriteBatch.Draw(pixel, new Rect(rect.Left, middleTop, rect.Width, middleHeight), top);
                return;
            }
            // Gradient: 2px bands are visually smooth and halve the draw count.
            for (var y = 0f; y < middleHeight; y += 2)
                spriteBatch.Draw(pixel, new Rect(rect.Left, middleTop + y, rect.Width, MathF.Min(2, middleHeight - y)), Lerp(top, bottom, rect, middleTop + y, true));
        }

        private void DrawRow(SpriteBatch spriteBatch, Texture2D pixel, Rect rect, float y, float inset, ChamferCorners left, ChamferCorners right, Color color)
        {
            var l = (Corners & left) != 0 ? inset : 0;
            var r = (Corners & right) != 0 ? inset : 0;
            var width = rect.Width - l - r;
            if (width > 0)
                spriteBatch.Draw(pixel, new Rect(rect.Left + l, y, width, 1), color);
        }

        private static Color Lerp(Color top, Color bottom, Rect rect, float y, bool gradient)
        {
            if (!gradient || rect.Height <= 1)
                return top;
            return Color.Lerp(top, bottom, MathHelper.Clamp((y - rect.Top) / (rect.Height - 1), 0, 1));
        }
    }
}
