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

            Foreground = theme.ProgressIndicatorForeground;
            Height = theme.ProgressIndicatorHeight;
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
        }

        public int MinSize { get; set; }
        public int MaxSize { get; set; }

        public override Size MeasureLayout()
        {
            // Fix the width before measuring (not after), so the Size this call returns already
            // reflects it instead of reporting 0 and forcing an extra invalidation/re-measure pass.
            if (!Width.IsFixed())
                Width = MaxSize;
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

            for (var i = 0; i < RectangleCount; i++)
            {
                _drawOrder[i] = i;
                _drawOrderValues[i] = Precalculate(i, MaxValue, out _);
            }
            Array.Sort(_drawOrderValues, _drawOrder);

            foreach (var i in _drawOrder)
                DrawRectangle(spriteBatch, rect, i, MaxValue);
        }

        private void DrawRectangle(SpriteBatch spriteBatch, Rect rect, int i, int maxValue)
        {
            var color = Foreground;
            double rad;
            var value = Precalculate(i, maxValue, out rad);
            color.A = (byte)(value * 204 + 51);
            var size = (float)(value * (MaxSize - MinSize) + MinSize);
            var top = rect.Top + (float)((1 - Math.Abs(Math.Sin(rad))) * (rect.Height - size));
            var rectangle = new Rect(rect.Left + (rect.Width - size) / 2, top, size, size);
            spriteBatch.Draw(SolidColorBrush.Pixel, rectangle, Brush.ApplyOpacity(color, RenderOpacity));
        }

        private static double Precalculate(int i, int maxValue, out double rad)
        {
            rad = ScreenSystem.TotalTime.TotalSeconds*Math.PI + Math.PI/maxValue*i;
            return Math.Abs(Math.Sin(rad - Math.PI/4));
        }
    }
}
