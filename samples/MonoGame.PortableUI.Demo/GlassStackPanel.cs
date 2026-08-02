using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Demo
{
    internal sealed class GlassStackPanel : StackPanel
    {
        public GlassStackPanel()
        {
            BorderWidth = 1;
        }

        public Brush? HighlightBrush { get; set; }
        public Brush? ShadowBrush { get; set; }
        public float BorderWidth
        {
            get { return BorderThickness.Top; }
            set { BorderThickness = new Thickness(value); }
        }

        protected override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            ShadowBrush?.Draw(spriteBatch, new Rect(rect.Left + 8, rect.Top + 10, rect.Width, rect.Height), RenderOpacity);
            base.OnDraw(spriteBatch, rect);

            if (HighlightBrush == null)
                return;

            HighlightBrush.Draw(spriteBatch, new Rect(rect.Left, rect.Top, rect.Width, 2), RenderOpacity);
            HighlightBrush.Draw(spriteBatch, new Rect(rect.Left, rect.Top, 2, rect.Height), RenderOpacity * 0.75f);
        }
    }
}
