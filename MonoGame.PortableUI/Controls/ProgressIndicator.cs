using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class ProgressIndicator : Control
    {
        public Color Foreground { get; }

        public ProgressIndicator()
        {
            Foreground = Color.DarkBlue;
            Height = 50;
            MinSize = 6;
            MaxSize = 12;
        }

        public int MinSize { get; set; }
        public int MaxSize { get; set; }
        public override Size MeasureLayout()
        {
            var size = base.MeasureLayout();
            if (!Width.IsFixed())
                Width = MaxSize;
            return size;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            rect -= Margin;
            const int maxValue = 5;

            var dict = new Dictionary<int, double>();
            for (int i = 0; i < maxValue-2; i++)
            {
                double d;
                var abs = Precalculate(i, maxValue, out d);
                dict.Add(i, abs);
            }
            var keyValuePairs = dict.OrderBy(x => x.Value);
            foreach (var pair in keyValuePairs)
            {
                DrawRectangle(spriteBatch, rect, pair.Key, maxValue);
            }
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
            new SolidColorBrush(color).Draw(spriteBatch, rectangle);
        }

        private static double Precalculate(int i, int maxValue, out double rad)
        {
            rad = ScreenManager.Time.TotalSeconds*Math.PI + Math.PI/maxValue*i;
            return Math.Abs(Math.Sin(rad - Math.PI/4));
        }
    }
}