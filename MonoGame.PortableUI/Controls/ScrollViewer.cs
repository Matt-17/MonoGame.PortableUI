using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class ScrollViewer : ContentControl
    {
        public override Size MeasureLayout()
        {
            return base.MeasureLayout();
        }

        public override void UpdateLayout(Rect rect)
        {                                               
            base.UpdateLayout(rect);
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);


            Content?.OnDraw(spriteBatch, Content.BoundingRect);
        }                          


    }
}