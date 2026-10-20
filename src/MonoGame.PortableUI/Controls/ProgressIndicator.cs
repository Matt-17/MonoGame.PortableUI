using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class ProgressIndicator : Control
    {
        public Color Foreground { get; set; }

        public ProgressIndicator()
        {
            var theme = PortableTheme.ResolveCurrent();

            IsFocusable = false;
            Foreground = theme.ProgressIndicatorForeground;
            Height = theme.ProgressIndicatorHeight;
            IndicatorStyle = theme.ProgressIndicatorStyle;
            StrokeWidth = theme.ProgressIndicatorStrokeWidth;
            MinSize = 6;
            MaxSize = 12;
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

            if (Foreground.Equals(oldTheme.ProgressIndicatorForeground))
                Foreground = newTheme.ProgressIndicatorForeground;
            if (Height.Equals(oldTheme.ProgressIndicatorHeight))
                Height = newTheme.ProgressIndicatorHeight;
            if (IndicatorStyle == oldTheme.ProgressIndicatorStyle)
                IndicatorStyle = newTheme.ProgressIndicatorStyle;
            if (StrokeWidth.Equals(oldTheme.ProgressIndicatorStrokeWidth))
                StrokeWidth = newTheme.ProgressIndicatorStrokeWidth;
        }

        /// <summary>Squares (default) or Material's rotating arc.</summary>
        public ProgressIndicatorStyle IndicatorStyle { get; set; }

        /// <summary>Stroke width of the <see cref="ProgressIndicatorStyle.CircularArc"/> (Material: 4).</summary>
        public float StrokeWidth { get; set; } = 4;

        /// <summary>
        ///     Axis the <see cref="ProgressIndicatorStyle.CircularArc"/> spins around: Z (default) turns it in the
        ///     screen plane; X and Y also flip the ring in space around the horizontal or vertical axis.
        /// </summary>
        public ProgressIndicatorAxis RotationAxis { get; set; }

        /// <summary>Turns the arc counterclockwise.</summary>
        public bool Reverse { get; set; }

        // One flip about the X or Y axis per this many seconds.
        private const double FlipSeconds = 2.4;

        public int MinSize { get; set; }
        public int MaxSize { get; set; }

        public override Size MeasureLayout()
        {
            // Fix the width before measuring (not after), so the Size this call returns already
            // reflects it instead of reporting 0 and forcing an extra invalidation/re-measure pass.
            if (!Width.IsFixed())
                Width = IndicatorStyle == ProgressIndicatorStyle.CircularArc && Height.IsFixed() ? Height : MaxSize;
            return base.MeasureLayout();
        }

        private const int MaxValue = 5;
        private const int RectangleCount = MaxValue - 2;

        // Reused across frames instead of allocating a Dictionary + LINQ OrderBy on every OnDraw,
        // which ran continuously while the spinner is visible.
        private readonly int[] _drawOrder = { 0, 1, 2 };
        private readonly double[] _drawOrderValues = new double[RectangleCount];

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            if (IndicatorStyle == ProgressIndicatorStyle.CircularArc)
            {
                DrawArc(spriteBatch, rect);
                ScreenEngine.RequestAnimationFrame();
                return;
            }

            for (var i = 0; i < RectangleCount; i++)
            {
                _drawOrder[i] = i;
                _drawOrderValues[i] = Precalculate(i, MaxValue, out _);
            }
            Array.Sort(_drawOrderValues, _drawOrder);

            foreach (var i in _drawOrder)
                DrawRectangle(spriteBatch, rect, i, MaxValue);
            ScreenEngine.RequestAnimationFrame();
        }

        private void DrawRectangle(SpriteBatch spriteBatch, Rect rect, int i, int maxValue)
        {
            var color = Foreground;
            double rad;
            var value = Precalculate(i, maxValue, out rad);
            color.A = (byte)(value * 204 + 51);
            // Sizes are layout units; rect is in render space (LayoutScale).
            var size = ToRender((float)(value * (MaxSize - MinSize) + MinSize));
            var top = rect.Top + (float)((1 - Math.Abs(Math.Sin(rad))) * (rect.Height - size));
            var rectangle = new Rect(rect.Left + (rect.Width - size) / 2, top, size, size);
            spriteBatch.Draw(Primitives.Pixel(spriteBatch), rectangle, Brush.ApplyOpacity(color, RenderOpacity));
        }

        // Material indeterminate timing: the whole arc turns once per RotationSeconds; every ArcSeconds
        // the head runs ahead (sweep grows from Min to Max) and then the tail catches up (sweep shrinks),
        // and the next cycle starts where the tail stopped.
        private const double RotationSeconds = 1.568;
        private const double ArcSeconds = 1.333;
        private const float MinSweepDegrees = 10;
        private const float MaxSweepDegrees = 270;

        private void DrawArc(SpriteBatch spriteBatch, Rect rect)
        {
            var size = Math.Min(rect.Width, rect.Height);
            var stroke = Math.Max(1, ToRender(StrokeWidth));
            // Material draws the arc inset from the 48 dp box (39.6 dp across the stroke, measured).
            var radius = (size * 0.825f - stroke) / 2;
            if (radius <= 0)
                return;
            var center = new Vector2(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);

            var seconds = ScreenSystem.TotalTime.TotalSeconds;
            var cycle = Math.Floor(seconds / ArcSeconds);
            var phase = (float)(seconds / ArcSeconds - cycle);
            var grow = MaxSweepDegrees - MinSweepDegrees;
            var baseAngle = (float)(seconds / RotationSeconds * 360 + cycle * grow) - 90;
            // Seen along the flip axis the ring is a circle squeezed by the cosine of the flip angle.
            var flip = MathF.Cos((float)(seconds / FlipSeconds * Math.PI * 2));
            var squeeze = RotationAxis switch
            {
                ProgressIndicatorAxis.X => new Vector2(1, flip),
                ProgressIndicatorAxis.Y => new Vector2(flip, 1),
                _ => Vector2.One
            };
            var direction = Reverse ? -1f : 1f;
            float tail, head;
            if (phase < 0.5f)
            {
                tail = baseAngle;
                head = baseAngle + MinSweepDegrees + grow * Ease(phase * 2);
            }
            else
            {
                tail = baseAngle + grow * Ease((phase - 0.5f) * 2);
                head = baseAngle + MaxSweepDegrees;
            }

            // Overlapping anti-aliased dots a third of the stroke apart draw a smooth stroke with round caps.
            var sweepRadians = MathHelper.ToRadians(head - tail);
            var steps = Math.Max(2, (int)Math.Ceiling(sweepRadians * radius / (stroke / 3)));
            var color = Brush.ApplyOpacity(Foreground, RenderOpacity);
            var dotRadius = new CornerRadius(stroke / 2);
            for (var i = 0; i <= steps; i++)
            {
                var angle = MathHelper.ToRadians(tail) + sweepRadians * i / steps;
                var point = center + new Vector2(MathF.Cos(angle), direction * MathF.Sin(angle)) * squeeze * radius;
                RoundedRectRenderer.DrawSolid(spriteBatch, new Rect(point.X - stroke / 2, point.Y - stroke / 2, stroke, stroke), dotRadius, color);
            }
        }

        // Material's "fast out, slow in" (cubic bezier 0.4, 0, 0.2, 1), approximated.
        private static float Ease(float t)
        {
            t = Math.Clamp(t, 0, 1);
            return t < 0.5f ? 4 * t * t * t : 1 - MathF.Pow(-2 * t + 2, 3) / 2;
        }

        private static double Precalculate(int i, int maxValue, out double rad)
        {
            rad = ScreenSystem.TotalTime.TotalSeconds*Math.PI + Math.PI/maxValue*i;
            return Math.Abs(Math.Sin(rad - Math.PI/4));
        }
    }
}
