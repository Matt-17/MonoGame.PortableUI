using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    /// Shared stop-ordering/hashing/evaluation logic used by <see cref="LinearGradientBrush"/> and
    /// <see cref="RadialGradientBrush"/> (previously duplicated byte-for-byte in both).
    /// </summary>
    internal static class GradientStops
    {
        public static IReadOnlyList<GradientStop> GetOrdered(List<GradientStop> stops)
        {
            if (stops.Count == 0)
                return new[] { new GradientStop(0, Color.Transparent), new GradientStop(1, Color.Transparent) };
            if (stops.Count == 1)
                return new[] { new GradientStop(0, stops[0].Color), new GradientStop(1, stops[0].Color) };
            return stops.OrderBy(stop => stop.Offset).ToArray();
        }

        /// <summary>
        /// Hash of the stops in offset order, the same value <see cref="GetOrdered"/> would give. Called by every gradient
        /// brush on every draw (its cache key), so it sorts on the stack instead of through LINQ: the ordered copy used to be
        /// several hundred bytes of garbage per gradient per frame.
        /// </summary>
        public static int GetHash(List<GradientStop> stops)
        {
            var hash = new HashCode();
            if (stops.Count <= 1)
            {
                // GetOrdered's two-stop stand-ins for an empty or single-stop list.
                var color = stops.Count == 0 ? Color.Transparent : stops[0].Color;
                Add(ref hash, new GradientStop(0, color));
                Add(ref hash, new GradientStop(1, color));
                return hash.ToHashCode();
            }
            Span<GradientStop> ordered = stops.Count <= 64 ? stackalloc GradientStop[stops.Count] : new GradientStop[stops.Count];
            // Insertion sort: stable like OrderBy (stops at one offset make a hard edge and keep their order), and the
            // lists are short.
            for (var i = 0; i < ordered.Length; i++)
            {
                var stop = stops[i];
                var j = i;
                while (j > 0 && ordered[j - 1].Offset > stop.Offset)
                {
                    ordered[j] = ordered[j - 1];
                    j--;
                }
                ordered[j] = stop;
            }
            foreach (var stop in ordered)
                Add(ref hash, stop);
            return hash.ToHashCode();

            static void Add(ref HashCode hash, GradientStop stop)
            {
                hash.Add(BitConverter.SingleToInt32Bits(stop.Offset));
                hash.Add(stop.Color.PackedValue);
            }
        }

        public static Color Evaluate(IReadOnlyList<GradientStop> stops, float offset)
        {
            offset = MathHelper.Clamp(offset, 0, 1);
            var previous = stops[0];
            for (var i = 1; i < stops.Count; i++)
            {
                var next = stops[i];
                if (offset > next.Offset)
                {
                    previous = next;
                    continue;
                }

                var span = Math.Max(0.0001f, next.Offset - previous.Offset);
                return Color.Lerp(previous.Color, next.Color, (offset - previous.Offset) / span);
            }

            return stops[stops.Count - 1].Color;
        }
    }
}
