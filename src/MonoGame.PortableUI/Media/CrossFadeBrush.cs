using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public sealed class CrossFadeBrush : Brush
    {
        private float _progress;

        public CrossFadeBrush(Brush from, Brush to, float progress = 0)
        {
            From = from;
            To = to;
            Progress = progress;
        }

        public Brush From { get; set; }
        public Brush To { get; set; }

        public float Progress
        {
            get { return _progress; }
            set { _progress = MathHelper.Clamp(value, 0, 1); }
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect)
        {
            Draw(spriteBatch, rect, 1);
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            From.Draw(spriteBatch, rect, opacity * (1 - Progress));
            To.Draw(spriteBatch, rect, opacity * Progress);
        }

        public override void Draw(SpriteBatch spriteBatch, in BrushContext context)
        {
            var fromContext = new BrushContext(
                context.Rect,
                context.Radius,
                context.Opacity * (1 - Progress),
                context.Device,
                context.TimeSeconds,
                context.PointerPosition);
            var toContext = new BrushContext(
                context.Rect,
                context.Radius,
                context.Opacity * Progress,
                context.Device,
                context.TimeSeconds,
                context.PointerPosition);

            From.Draw(spriteBatch, in fromContext);
            To.Draw(spriteBatch, in toContext);
        }
    }
}
