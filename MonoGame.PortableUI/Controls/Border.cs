using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class Border : ContentControl
    {
        public Brush BorderColor { get; set; }
        public Thickness BorderWidth { get; set; }

        private IEnumerable<Rect> GetBorderRects(Rect rect)
        {
            yield return new Rect(rect.Left, rect.Top, rect.Width, BorderWidth.Top);
            yield return new Rect(rect.Left, rect.Top, BorderWidth.Left, rect.Height);
            yield return new Rect(rect.Left + rect.Width - BorderWidth.Right, rect.Top, BorderWidth.Right, rect.Height);
            yield return new Rect(rect.Left, rect.Top + rect.Height - BorderWidth.Bottom, rect.Width, BorderWidth.Bottom);
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            foreach (var borderRect in GetBorderRects(rect))
            {
                BorderColor.Draw(spriteBatch, borderRect);
            }
            BackgroundBrush.Draw(spriteBatch, rect - BorderWidth);
        }
    }
}