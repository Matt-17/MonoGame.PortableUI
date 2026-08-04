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

        public static int GetHash(List<GradientStop> stops)
        {
            var hash = new HashCode();
            foreach (var stop in GetOrdered(stops))
            {
                hash.Add(BitConverter.SingleToInt32Bits(stop.Offset));
                hash.Add(stop.Color.PackedValue);
            }
            return hash.ToHashCode();
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
