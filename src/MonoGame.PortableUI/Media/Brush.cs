using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    ///     Base of all fills. Colours given to brushes (and to <c>TextColor</c>, <c>TintColor</c>, shadows and
    ///     themes) are straight alpha; brushes premultiply when drawing. Do not pass MonoGame's
    ///     premultiplied <c>Color * float</c> - use <see cref="ColorAlpha.WithAlpha(Color, float)"/>.
    /// </summary>
    public abstract class Brush
    {
        private DrawnBy _drawnBy;

        /// <summary>Bumped when a brush (or text effect) changes whose engine is unknown; engines poll it each update.</summary>
        internal static int GlobalVersion => DrawnBy.GlobalVersion;

        /// <summary>Remembers the engine drawing this brush, so a change redraws only that engine. Brushes with
        /// settable look call it first in their Draw overrides.</summary>
        protected void MarkDrawn() => _drawnBy.Mark();

        /// <summary>
        ///     Call from property setters when the brush's look changed: the engine that draws it (or, when
        ///     unknown or shared, every engine) draws a new frame and drops its cached layers, so the change
        ///     shows in <see cref="RenderMode.OnDemand"/>, on surfaces and inside <c>CacheMode.Bitmap</c> layers.
        /// </summary>
        protected void OnChanged() => _drawnBy.Changed();

        /// <summary>Sets a property's field and calls <see cref="OnChanged"/> when the value differs.</summary>
        protected void SetProperty<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            OnChanged();
        }

        public virtual bool RequiresBackdrop => false;

        /// <summary>
        ///     Frame drawn by the brush itself (bevel lines, rings, cut-corner borders), in layout units:
        ///     containers such as ListBox keep their content inside it, as they do for a border.
        /// </summary>
        public virtual Thickness ContentInset => default;

        public abstract void Draw(SpriteBatch spriteBatch, Rect rect);

        public virtual void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            Draw(spriteBatch, rect);
        }

        public virtual void Draw(SpriteBatch spriteBatch, in BrushContext context)
        {
            Draw(spriteBatch, context.Rect, context.Opacity);
        }

        /// <summary>
        ///     For brushes that cannot render rounded corners (backdrop samplers like frosted
        ///     glass): a straight-alpha solid approximation used by small rounded chrome such as
        ///     hover/pressed overlays. Null means the brush renders corner radii itself.
        /// </summary>
        protected internal virtual Color? RoundedFallbackColor => null;

        /// <summary>
        ///     Applies opacity and converts to premultiplied alpha. SpriteBatch's default AlphaBlend
        ///     state expects premultiplied colors; passing straight-alpha colors over-brightens every
        ///     translucent draw (they blend additively instead of compositing).
        /// </summary>
        public static Color ApplyOpacity(Color color, float opacity)
        {
            opacity = MathHelper.Clamp(opacity, 0, 1);
            var alpha = (int)MathHelper.Clamp(color.A * opacity, 0, 255);
            return Color.FromNonPremultiplied(color.R, color.G, color.B, alpha);
        }

        /// <summary>Converts a straight-alpha color to premultiplied alpha for SpriteBatch drawing.</summary>
        public static Color Premultiply(Color color)
        {
            return color.A == 255 ? color : Color.FromNonPremultiplied(color.R, color.G, color.B, color.A);
        }

        public static implicit operator Brush(Color color)
        {
            return new SolidColorBrush(color);
        }
    }
}
