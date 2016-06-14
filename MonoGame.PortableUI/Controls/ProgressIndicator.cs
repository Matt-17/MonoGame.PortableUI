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
                dict.Add(i, GetSize(i, maxValue));
            }
            var keyValuePairs = dict.OrderBy(x => x.Value);
            foreach (var pair in keyValuePairs)
            {
                GetValue(spriteBatch, rect, pair.Key, maxValue);
            }
        }

        private double GetSize(int a, int max)
        {
            var d = ScreenManager.Time.TotalSeconds * Math.PI + Math.PI / max * a;
            return Math.Abs(Math.Cos(d + Math.PI / 4));
        }

        private void GetValue(SpriteBatch spriteBatch, Rect rect, int a, int max)
        {
            var d = ScreenManager.Time.TotalSeconds * Math.PI + Math.PI / max * a;
            var abs = Math.Abs(Math.Cos(d + Math.PI / 4));
            var size = (float)(abs * (MaxSize - MinSize) + MinSize);
            var top = rect.Top + (float)((1 - Math.Abs(Math.Sin(d))) * (rect.Height - size));
            var rectangle = new Rect(rect.Left + (rect.Width - size) / 2, top, size, size);
            var color = Foreground;
            color.A = (byte)(abs * 204 + 51);
            new SolidColorBrush(color).Draw(spriteBatch, rectangle);
        }
    }
}