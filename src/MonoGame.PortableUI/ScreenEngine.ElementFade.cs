using System;
using System.Collections.Generic;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI
{
    // ThemeTransitionStyle.ElementFade: no picture of the old look. The old tree (a ghost) and the new one are paired
    // element by element (same place in the tree, or the same Tag among siblings), and each new element draws its old
    // twin over itself, each at its own pace (Control.ThemeFade): Slow elements blend evenly over their parent's time,
    // Flip elements stay near their old look, flip quickly when their parent passes the middle and settle within 80 % of
    // its time. Elements without a twin come in at their parent's pace; old elements without one go.
    public partial class ScreenEngine
    {
        // Share of the parent's time a Flip element uses, centred on the parent's middle.
        private const float FlipShare = 0.8f;

        private static void BuildThemeTwins(Screen ghost, Screen source, ThemeLayer layer)
        {
            var map = source.ThemeTwins ??= new Dictionary<Control, ThemeTwin>();
            map.Clear();
            source.ThemeTwinGhost = ghost;
            source.ThemeTwinDuration = layer.Duration;
            source.ThemeTwinProgress = Math.Min(1, Progress(layer));
            if (source.Content is { } fresh)
                Pair(ghost.Content, fresh, new ThemeTwin(null, 0, 1, false, false), map);
        }

        private static void Pair(Control? old, Control fresh, ThemeTwin parent, Dictionary<Control, ThemeTwin> map)
        {
            var surface = fresh.BackgroundBrush != null;
            var twin = fresh.ThemeFade switch
            {
                ThemeFadeMode.Slow => new ThemeTwin(old, parent.Start, parent.End, false, surface),
                ThemeFadeMode.Flip => Flip(old, parent, surface),
                _ => new ThemeTwin(old, parent.Start, parent.End, parent.Steep, surface)
            };
            map[fresh] = twin;

            var count = fresh.VisualChildCount;
            for (var i = 0; i < count; i++)
            {
                var child = fresh.GetVisualChild(i);
                Pair(MatchChild(old, child, i), child, twin, map);
            }
        }

        private static ThemeTwin Flip(Control? old, ThemeTwin parent, bool surface)
        {
            var middle = (parent.Start + parent.End) / 2;
            var half = (parent.End - parent.Start) * FlipShare / 2;
            return new ThemeTwin(old, middle - half, middle + half, true, surface);
        }

        // The old element in the same place: the sibling with the same Tag (rows, keyed items), else the same index.
        private static Control? MatchChild(Control? old, Control child, int index)
        {
            if (old == null)
                return null;
            var count = old.VisualChildCount;
            if (child.Tag != null)
            {
                for (var i = 0; i < count; i++)
                {
                    var candidate = old.GetVisualChild(i);
                    if (candidate.GetType() == child.GetType() && Equals(candidate.Tag, child.Tag))
                        return candidate;
                }
                return null;
            }
            if (index >= count)
                return null;
            var same = old.GetVisualChild(index);
            return same.GetType() == child.GetType() ? same : null;
        }
    }

    /// <summary>One element of an <see cref="ThemeTransitionStyle.ElementFade"/>: its old twin and when it changes over.</summary>
    internal readonly struct ThemeTwin
    {
        public ThemeTwin(Control? old, float start, float end, bool steep, bool surface)
        {
            Old = old;
            Start = start;
            End = end;
            Steep = steep;
            Surface = surface;
        }

        /// <summary>The element in the old look; null when the new element has none (it comes in instead).</summary>
        public Control? Old { get; }

        /// <summary>Its stretch of the change (0..1 of the whole).</summary>
        public float Start { get; }
        public float End { get; }

        /// <summary>Flip: slow at both ends, a quick crossing in the middle; else gently in and out.</summary>
        public bool Steep { get; }

        /// <summary>It has a background: the new one is drawn fully and the old one over it, fading out (text and other
        /// ink fade one into the other).</summary>
        public bool Surface { get; }

        /// <summary>How far it has changed to the new look at <paramref name="progress"/> of the whole change.</summary>
        public float Mix(float progress, TimeSpan duration)
        {
            var length = End - Start;
            if (length <= 0)
                return progress >= End ? 1 : 0;
            var local = Math.Clamp((progress - Start) / length, 0, 1);
            if (Steep)
                return ScreenEngine.FlipCurve(local, TimeSpan.FromTicks((long)(duration.Ticks * length)));
            return local * local * (3 - 2 * local);
        }
    }
}
