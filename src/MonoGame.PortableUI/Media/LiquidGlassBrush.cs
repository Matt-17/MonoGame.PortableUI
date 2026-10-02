using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Effects;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    ///     Liquid glass: a nearly clear pane that refracts the scene behind it like a lens near its
    ///     rounded rim (with a slight chromatic fringe), catches light on the top-left edge and
    ///     barely frosts its body. Needs the backdrop pass (the screen's background or an external
    ///     scene) and shader support; otherwise it falls back to the frosted acrylic look.
    /// </summary>
    public sealed class LiquidGlassBrush : AcrylicBrush
    {
        public LiquidGlassBrush()
            : this(new Color(255, 255, 255, 30))
        {
        }

        public LiquidGlassBrush(Color tint)
            : base(tint)
        {
            BlurRadius = 20;
            GrainOpacity = 0.03f;
            SaturationBoost = 0.32f;
        }

        /// <summary>Displacement at the rim, in layout units.</summary>
        public float Refraction { get; set; } = 14;

        /// <summary>Width of the refracting rim, in layout units.</summary>
        public float Bezel { get; set; } = 16;

        /// <summary>Chromatic fringe as a fraction of the displacement (0 = none).</summary>
        public float Chroma { get; set; } = 0.12f;

        /// <summary>How frosted the body is: 0 = crystal clear, 1 = fully blurred.</summary>
        public float Frost { get; set; } = 0.18f;

        /// <summary>Strength of the specular rim and sheen.</summary>
        public float Highlight { get; set; } = 0.55f;

        public override void Draw(SpriteBatch spriteBatch, in BrushContext context)
        {
            if (TryDrawRefracted(spriteBatch, in context))
                return;
            base.Draw(spriteBatch, in context);
        }

        private bool TryDrawRefracted(SpriteBatch spriteBatch, in BrushContext context)
        {
            var rect = context.Rect;
            var device = spriteBatch.GraphicsDevice;
            if (rect.Width <= 1 || rect.Height <= 1
                || !BackdropSource.TryGetSharp(device, out var scene, out var blurred, out var screenRect) || scene == null || blurred == null
                || !EffectCache.TryGetEffect(device, EffectNames.LiquidGlass, out var effect) || effect == null)
                return false;

            var scaleX = scene.Width / Math.Max(1f, screenRect.Width);
            var scaleY = scene.Height / Math.Max(1f, screenRect.Height);
            var source = new Rectangle(
                (int)MathF.Floor((rect.Left - screenRect.Left) * scaleX),
                (int)MathF.Floor((rect.Top - screenRect.Top) * scaleY),
                Math.Max(1, (int)MathF.Ceiling(rect.Width * scaleX)),
                Math.Max(1, (int)MathF.Ceiling(rect.Height * scaleY)));

            var scale = context.Scale;
            var radius = Math.Max(Math.Max(context.Radius.TopLeft, context.Radius.TopRight), Math.Max(context.Radius.BottomLeft, context.Radius.BottomRight));
            var tint = Premultiply(TintColor).ToVector4();
            effect.Parameters["RectSize"]?.SetValue(new Vector2(rect.Width, rect.Height));
            effect.Parameters["Radius"]?.SetValue(radius);
            effect.Parameters["Bezel"]?.SetValue(Math.Min(Bezel * scale, Math.Min(rect.Width, rect.Height) / 2));
            effect.Parameters["Refraction"]?.SetValue(Refraction * scale * scaleX);
            effect.Parameters["Chroma"]?.SetValue(Chroma);
            effect.Parameters["Frost"]?.SetValue(MathHelper.Clamp(Frost, 0, 1));
            effect.Parameters["Tint"]?.SetValue(tint);
            effect.Parameters["Highlight"]?.SetValue(Highlight);
            effect.Parameters["UvMin"]?.SetValue(new Vector2(source.X / (float)scene.Width, source.Y / (float)scene.Height));
            effect.Parameters["UvMax"]?.SetValue(new Vector2(source.Right / (float)scene.Width, source.Bottom / (float)scene.Height));
            effect.Parameters["TexelSize"]?.SetValue(new Vector2(1f / scene.Width, 1f / scene.Height));

            // The control batch is suspended for one immediate draw with the glass shader.
            spriteBatch.End();
            Screen.BeginEffectBatch(spriteBatch, effect, SamplerState.LinearClamp);
            device.Textures[1] = blurred;
            device.SamplerStates[1] = SamplerState.LinearClamp;
            spriteBatch.Draw(scene, rect, source, Color.White * MathHelper.Clamp(context.Opacity, 0, 1));
            spriteBatch.End();
            device.Textures[1] = null;
            Screen.ResumeControlBatch(spriteBatch);
            return true;
        }
    }
}
