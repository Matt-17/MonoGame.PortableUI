using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class ProgressIndicator : Control
    {
        public Brush Foreground { get; }

        public ProgressIndicator()
        {
            Width = 8;
            Foreground = Color.DarkBlue;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            rect -= Margin;
            var size = 8;// - (float)Math.Abs(Math.Sin(ScreenManager.Time.TotalSeconds * Math.PI/2)) * (4);
            var top = rect.Top + (float)((1 - Math.Abs(Math.Sin(ScreenManager.Time.TotalSeconds * Math.PI))) * (rect.Height - size));

            var rectangle = new Rect(rect.Left + (rect.Width - size) / 2, top, size, size);
            Foreground.Draw(spriteBatch, rectangle);
        }
    }
}