using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public class TileBrush : Brush
    {
        public Texture2D? Source { get; set; }

        public float Scale { get; set; } = 1;

        public Color TintColor { get; set; } = Color.White;

        public override void Draw(SpriteBatch spriteBatch, Rect rect)
        {
            Draw(spriteBatch, rect, 1);
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            if (Source == null || rect.Width <= 0 || rect.Height <= 0)
                return;

            var tint = ApplyOpacity(TintColor, opacity);
            foreach (var tile in GetTileSegments(rect, Source.Width, Source.Height, Scale))
                spriteBatch.Draw(Source, tile.DestinationRect, tile.SourceRectangle, tint);
        }

        internal static IEnumerable<Rect> GetTileRects(Rect targetRect, int sourceWidth, int sourceHeight, float scale)
        {
            foreach (var tile in GetTileSegments(targetRect, sourceWidth, sourceHeight, scale))
                yield return tile.DestinationRect;
        }

        internal static IEnumerable<TileSegment> GetTileSegments(Rect targetRect, int sourceWidth, int sourceHeight, float scale)
        {
            if (targetRect.Width <= 0 || targetRect.Height <= 0 || sourceWidth <= 0 || sourceHeight <= 0)
                yield break;

            var scaleFactor = Math.Max(0.001f, scale);
            var tileWidth = Math.Max(0.001f, sourceWidth * scaleFactor);
            var tileHeight = Math.Max(0.001f, sourceHeight * scaleFactor);

            for (var top = targetRect.Top; top < targetRect.Bottom; top += tileHeight)
            {
                var height = Math.Min(tileHeight, targetRect.Bottom - top);
                var sourceRectangleHeight = Math.Min(sourceHeight, Math.Max(1, (int)Math.Ceiling(height / scaleFactor)));
                for (var left = targetRect.Left; left < targetRect.Right; left += tileWidth)
                {
                    var width = Math.Min(tileWidth, targetRect.Right - left);
                    var sourceRectangleWidth = Math.Min(sourceWidth, Math.Max(1, (int)Math.Ceiling(width / scaleFactor)));
                    if (width > 0 && height > 0)
                        yield return new TileSegment(
                            new Rectangle(0, 0, sourceRectangleWidth, sourceRectangleHeight),
                            new Rect(left, top, width, height));
                }
            }
        }

        internal readonly struct TileSegment
        {
            public TileSegment(Rectangle sourceRectangle, Rect destinationRect)
            {
                SourceRectangle = sourceRectangle;
                DestinationRect = destinationRect;
            }

            public Rectangle SourceRectangle { get; }

            public Rect DestinationRect { get; }
        }
    }
}
