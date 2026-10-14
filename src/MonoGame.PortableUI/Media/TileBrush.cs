using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public class TileBrush : Brush
    {
        private Texture2D? _source;
        public Texture2D? Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }

        private float _scale = 1;
        public float Scale
        {
            get => _scale;
            set => SetProperty(ref _scale, value);
        }

        private Color _tintColor = Color.White;
        public Color TintColor
        {
            get => _tintColor;
            set => SetProperty(ref _tintColor, value);
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect)
        {
            MarkDrawn();
            Draw(spriteBatch, rect, 1);
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            MarkDrawn();
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

        /// <summary>
        /// The tiles covering <paramref name="targetRect"/>, row by row. A struct enumerator, not an iterator: tile and
        /// pattern brushes draw every frame, and an iterator was a new object per draw.
        /// </summary>
        internal static TileSegments GetTileSegments(Rect targetRect, int sourceWidth, int sourceHeight, float scale) =>
            new TileSegments(targetRect, sourceWidth, sourceHeight, scale);

        internal struct TileSegments
        {
            private readonly Rect _target;
            private readonly int _sourceWidth, _sourceHeight;
            private readonly float _scaleFactor, _tileWidth, _tileHeight;
            private readonly bool _empty;
            private float _top, _left;
            private bool _started;

            public TileSegments(Rect targetRect, int sourceWidth, int sourceHeight, float scale)
            {
                _target = targetRect;
                _sourceWidth = sourceWidth;
                _sourceHeight = sourceHeight;
                _empty = targetRect.Width <= 0 || targetRect.Height <= 0 || sourceWidth <= 0 || sourceHeight <= 0;
                _scaleFactor = Math.Max(0.001f, scale);
                _tileWidth = Math.Max(0.001f, sourceWidth * _scaleFactor);
                _tileHeight = Math.Max(0.001f, sourceHeight * _scaleFactor);
                _top = targetRect.Top;
                _left = targetRect.Left;
                _started = false;
                Current = default;
            }

            public TileSegment Current { get; private set; }

            public readonly TileSegments GetEnumerator() => this;

            /// <summary>All tiles at once (tests and one-off callers; drawing loops over the struct instead).</summary>
            public readonly TileSegment[] ToArray()
            {
                var tiles = new List<TileSegment>();
                foreach (var tile in this)
                    tiles.Add(tile);
                return tiles.ToArray();
            }

            // The rows from the top, each from the left edge, as nested loops would visit them.
            public bool MoveNext()
            {
                if (_empty)
                    return false;
                while (true)
                {
                    if (_started)
                        _left += _tileWidth;
                    _started = true;
                    if (_left >= _target.Right)
                    {
                        _top += _tileHeight;
                        _left = _target.Left;
                    }
                    if (_top >= _target.Bottom)
                        return false;
                    if (_left >= _target.Right)
                        continue;
                    var height = Math.Min(_tileHeight, _target.Bottom - _top);
                    var width = Math.Min(_tileWidth, _target.Right - _left);
                    if (width <= 0 || height <= 0)
                        continue;
                    var sourceRectangleHeight = Math.Min(_sourceHeight, Math.Max(1, (int)Math.Ceiling(height / _scaleFactor)));
                    var sourceRectangleWidth = Math.Min(_sourceWidth, Math.Max(1, (int)Math.Ceiling(width / _scaleFactor)));
                    Current = new TileSegment(new Rectangle(0, 0, sourceRectangleWidth, sourceRectangleHeight), new Rect(_left, _top, width, height));
                    return true;
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
