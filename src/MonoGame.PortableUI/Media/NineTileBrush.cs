using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public class NineTileBrush : Brush
    {
        private Texture2D? _source;
        public Texture2D? Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }

        private Thickness _sliceMargins;
        public Thickness SliceMargins
        {
            get => _sliceMargins;
            set => SetProperty(ref _sliceMargins, value);
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
            // On the stack: a nine-tile brush draws every frame, and an iterator with its slice tables was garbage per draw.
            Span<NineTileSegment> segments = stackalloc NineTileSegment[9];
            var count = FillSegments(rect, Source.Width, Source.Height, SliceMargins, segments);
            for (var i = 0; i < count; i++)
                spriteBatch.Draw(Source, segments[i].DestinationRect, segments[i].SourceRectangle, tint);
        }

        /// <summary>The segments as an array (tests and one-off callers).</summary>
        internal static NineTileSegment[] GetSegments(Rect targetRect, int sourceWidth, int sourceHeight, Thickness sliceMargins)
        {
            Span<NineTileSegment> segments = stackalloc NineTileSegment[9];
            return segments[..FillSegments(targetRect, sourceWidth, sourceHeight, sliceMargins, segments)].ToArray();
        }

        /// <summary>Writes the up to nine segments into <paramref name="segments"/>, row by row; returns how many.</summary>
        internal static int FillSegments(Rect targetRect, int sourceWidth, int sourceHeight, Thickness sliceMargins, Span<NineTileSegment> segments)
        {
            if (targetRect.Width <= 0 || targetRect.Height <= 0 || sourceWidth <= 0 || sourceHeight <= 0)
                return 0;

            var sourceLeft = Clamp(sliceMargins.Left, 0, sourceWidth);
            var sourceRight = Clamp(sliceMargins.Right, 0, sourceWidth - sourceLeft);
            var sourceTop = Clamp(sliceMargins.Top, 0, sourceHeight);
            var sourceBottom = Clamp(sliceMargins.Bottom, 0, sourceHeight - sourceTop);

            var targetHorizontal = FitEdges(sourceLeft, sourceRight, targetRect.Width);
            var targetVertical = FitEdges(sourceTop, sourceBottom, targetRect.Height);

            ReadOnlySpan<SourceSlice> sourceColumns =
            [
                new SourceSlice(0, sourceLeft),
                new SourceSlice(sourceLeft, sourceWidth - sourceLeft - sourceRight),
                new SourceSlice(sourceWidth - sourceRight, sourceRight)
            ];
            ReadOnlySpan<SourceSlice> sourceRows =
            [
                new SourceSlice(0, sourceTop),
                new SourceSlice(sourceTop, sourceHeight - sourceTop - sourceBottom),
                new SourceSlice(sourceHeight - sourceBottom, sourceBottom)
            ];
            ReadOnlySpan<TargetSlice> targetColumns =
            [
                new TargetSlice(targetRect.Left, targetHorizontal.First),
                new TargetSlice(targetRect.Left + targetHorizontal.First, targetRect.Width - targetHorizontal.First - targetHorizontal.Second),
                new TargetSlice(targetRect.Right - targetHorizontal.Second, targetHorizontal.Second)
            ];
            ReadOnlySpan<TargetSlice> targetRows =
            [
                new TargetSlice(targetRect.Top, targetVertical.First),
                new TargetSlice(targetRect.Top + targetVertical.First, targetRect.Height - targetVertical.First - targetVertical.Second),
                new TargetSlice(targetRect.Bottom - targetVertical.Second, targetVertical.Second)
            ];

            var count = 0;
            for (var row = 0; row < 3; row++)
            {
                if (sourceRows[row].Length <= 0 || targetRows[row].Length <= 0)
                    continue;

                for (var column = 0; column < 3; column++)
                {
                    if (sourceColumns[column].Length <= 0 || targetColumns[column].Length <= 0)
                        continue;

                    segments[count++] = new NineTileSegment(
                        new Rectangle(
                            (int)Math.Round(sourceColumns[column].Start),
                            (int)Math.Round(sourceRows[row].Start),
                            (int)Math.Round(sourceColumns[column].Length),
                            (int)Math.Round(sourceRows[row].Length)),
                        new Rect(
                            targetColumns[column].Start,
                            targetRows[row].Start,
                            targetColumns[column].Length,
                            targetRows[row].Length));
                }
            }
            return count;
        }

        private static EdgePair FitEdges(float first, float second, float available)
        {
            first = Math.Max(0, first);
            second = Math.Max(0, second);
            available = Math.Max(0, available);

            var sum = first + second;
            if (sum <= available || sum <= 0)
                return new EdgePair(first, second);

            var scale = available / sum;
            return new EdgePair(first * scale, second * scale);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (max < min)
                max = min;
            return Math.Max(min, Math.Min(value, max));
        }

        internal readonly struct NineTileSegment
        {
            public NineTileSegment(Rectangle sourceRectangle, Rect destinationRect)
            {
                SourceRectangle = sourceRectangle;
                DestinationRect = destinationRect;
            }

            public Rectangle SourceRectangle { get; }

            public Rect DestinationRect { get; }
        }

        private readonly struct SourceSlice
        {
            public SourceSlice(float start, float length)
            {
                Start = start;
                Length = length;
            }

            public float Start { get; }
            public float Length { get; }
        }

        private readonly struct TargetSlice
        {
            public TargetSlice(float start, float length)
            {
                Start = start;
                Length = length;
            }

            public float Start { get; }
            public float Length { get; }
        }

        private readonly struct EdgePair
        {
            public EdgePair(float first, float second)
            {
                First = first;
                Second = second;
            }

            public float First { get; }
            public float Second { get; }
        }
    }
}
