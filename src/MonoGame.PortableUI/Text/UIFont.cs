using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Text
{
    /// <summary>
    ///     A font backend that measures and draws at any pixel size. Text controls use it instead of
    ///     their <see cref="SpriteFont"/> when one is set (<see cref="Controls.TextBlock.DynamicFont"/>
    ///     or <see cref="FontManager.DefaultDynamicFont"/>). The FontStashSharp backend lives in the
    ///     optional MonoGame.PortableUI.FontStashSharp package; <see cref="SpriteFontUIFont"/> adapts a
    ///     baked SpriteFont to the same contract.
    /// </summary>
    public abstract class UIFont
    {
        /// <summary>Size used when a control does not ask for one (TextSize 0).</summary>
        public abstract float DefaultSize { get; }

        /// <summary>Distance between baselines at <paramref name="pixelSize"/>.</summary>
        public abstract float GetLineHeight(float pixelSize);

        public abstract Vector2 MeasureString(string text, float pixelSize);

        public abstract Vector2 MeasureString(StringBuilder text, float pixelSize);

        /// <summary>
        ///     Draws <paramref name="text"/> with its top-left at <paramref name="position"/>.
        ///     <paramref name="scale"/> is the control's render transform; a rasterizing backend should
        ///     render glyphs at <c>pixelSize * scale</c> so scaled text stays sharp.
        /// </summary>
        public abstract void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float pixelSize, Vector2 scale);

        public abstract void DrawString(SpriteBatch spriteBatch, StringBuilder text, Vector2 position, Color color, float pixelSize, Vector2 scale);
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
