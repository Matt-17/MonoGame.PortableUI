using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Text
{
    /// <summary>
    ///     A font backend that measures and draws at any pixel size. Text controls use it instead of
    ///     their <see cref="SpriteFont"/> when one is set (<see cref="Controls.TextBlock.DynamicFont"/>
    ///     or <see cref="FontManager.DefaultDynamicFont"/>). The FontStashSharp backend
    ///     (<see cref="FontStashUIFont"/>) ships in the core; <see cref="SpriteFontUIFont"/> adapts a
    ///     baked SpriteFont to the same contract.
    /// </summary>
    public abstract class UIFont
    {
        /// <summary>Size used when a control does not ask for one (TextSize 0).</summary>
        public abstract float DefaultSize { get; }

        /// <summary>Height of a line of text at <paramref name="pixelSize"/> (a single line's box).</summary>
        public abstract float GetLineHeight(float pixelSize);

        /// <summary>
        ///     Distance between the baselines of wrapped lines. Equal to <see cref="GetLineHeight"/> unless the
        ///     font pads only the first and last line (Android's includeFontPadding).
        /// </summary>
        public virtual float GetLineSpacing(float pixelSize) => GetLineHeight(pixelSize);

        public abstract Vector2 MeasureString(string text, float pixelSize);

        public abstract Vector2 MeasureString(StringBuilder text, float pixelSize);

        /// <summary>
        ///     Draws <paramref name="text"/> with its top-left at <paramref name="position"/>.
        ///     <paramref name="scale"/> is the control's render transform; a rasterizing backend should
        ///     render glyphs at <c>pixelSize * scale</c> so scaled text stays sharp.
        /// </summary>
        public abstract void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float pixelSize, Vector2 scale);

        public abstract void DrawString(SpriteBatch spriteBatch, StringBuilder text, Vector2 position, Color color, float pixelSize, Vector2 scale);

        /// <summary>
        ///     Like <see cref="DrawString(SpriteBatch, string, Vector2, Color, float, Vector2)"/>, rasterizing glyphs at
        ///     <paramref name="rasterScale"/> instead of <paramref name="scale"/>: text in motion (a breathing or
        ///     popping control) keeps the glyphs of its resting size and scales them, instead of re-rasterizing
        ///     at every intermediate size - which makes glyphs hop between pixels.
        /// </summary>
        public virtual void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float pixelSize, Vector2 scale, Vector2 rasterScale)
            => DrawString(spriteBatch, text, position, color, pixelSize, scale);

        public virtual void DrawString(SpriteBatch spriteBatch, StringBuilder text, Vector2 position, Color color, float pixelSize, Vector2 scale, Vector2 rasterScale)
            => DrawString(spriteBatch, text, position, color, pixelSize, scale);

        /// <summary>
        ///     How far glyph ink can reach outside the <see cref="MeasureString(string, float)"/> box at
        ///     <paramref name="pixelSize"/>, in pixels: e.g. descenders and commas of a font that measures only
        ///     the cap height, or an outline the font stamps itself. Text controls widen their drawing clip by it
        ///     (see <see cref="Controls.Control.InkOverflow"/>); layout is unaffected. Default: none.
        /// </summary>
        public virtual Common.Thickness GetInkOverflow(float pixelSize) => default;
    }

    /// <summary><see cref="UIFont"/> over a baked <see cref="SpriteFont"/>: sizes other than the baked one are scaled.</summary>
    public sealed class SpriteFontUIFont : UIFont
    {
        private readonly float _bakedSize;

        /// <param name="font">The baked font.</param>
        /// <param name="bakedSize">Point size the font was built at (0: looked up via <see cref="FontManager.GetBakedSize"/>,
        /// falling back to the line spacing).</param>
        public SpriteFontUIFont(SpriteFont font, float bakedSize = 0)
        {
            Font = font ?? throw new ArgumentNullException(nameof(font));
            if (bakedSize <= 0)
                bakedSize = FontManager.GetBakedSize(font);
            _bakedSize = bakedSize > 0 ? bakedSize : font.LineSpacing;
        }

        public SpriteFont Font { get; }

        public override float DefaultSize => _bakedSize;

        private float Scale(float pixelSize) => pixelSize > 0 ? pixelSize / _bakedSize : 1f;

        public override float GetLineHeight(float pixelSize) => Font.LineSpacing * Scale(pixelSize);

        public override Vector2 MeasureString(string text, float pixelSize) => Font.MeasureString(text ?? "") * Scale(pixelSize);

        public override Vector2 MeasureString(StringBuilder text, float pixelSize) => Font.MeasureString(text) * Scale(pixelSize);

        public override void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float pixelSize, Vector2 scale)
            => spriteBatch.DrawString(Font, text, position, color, 0, Vector2.Zero, scale * Scale(pixelSize), SpriteEffects.None, 0);

        public override void DrawString(SpriteBatch spriteBatch, StringBuilder text, Vector2 position, Color color, float pixelSize, Vector2 scale)
            => spriteBatch.DrawString(Font, text, position, color, 0, Vector2.Zero, scale * Scale(pixelSize), SpriteEffects.None, 0);
    }
}
