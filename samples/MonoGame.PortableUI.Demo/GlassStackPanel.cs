using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Demo
{
    internal sealed class GlassStackPanel : StackPanel
    {
        public Brush? BorderBrush { get; set; }
        public Brush? HighlightBrush { get; set; }
        public Brush? ShadowBrush { get; set; }
        public float BorderWidth { get; set; } = 1;

        protected override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            ShadowBrush?.Draw(spriteBatch, new Rect(rect.Left + 8, rect.Top + 10, rect.Width, rect.Height), RenderOpacity);
            base.OnDraw(spriteBatch, rect);

            if (BorderBrush != null && BorderWidth > 0)
                DrawBorder(spriteBatch, rect, BorderWidth, BorderBrush, RenderOpacity);

            if (HighlightBrush == null)
                return;

            HighlightBrush.Draw(spriteBatch, new Rect(rect.Left, rect.Top, rect.Width, 2), RenderOpacity);
            HighlightBrush.Draw(spriteBatch, new Rect(rect.Left, rect.Top, 2, rect.Height), RenderOpacity * 0.75f);
        }

        private static void DrawBorder(SpriteBatch spriteBatch, Rect rect, float width, Brush brush, float opacity)
        {
            brush.Draw(spriteBatch, new Rect(rect.Left, rect.Top, rect.Width, width), opacity);
            brush.Draw(spriteBatch, new Rect(rect.Left, rect.Top, width, rect.Height), opacity);
            brush.Draw(spriteBatch, new Rect(rect.Right - width, rect.Top, width, rect.Height), opacity);
            brush.Draw(spriteBatch, new Rect(rect.Left, rect.Bottom - width, rect.Width, width), opacity);
        }
    }
}
