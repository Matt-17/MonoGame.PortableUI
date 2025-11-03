using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Text
{
    public sealed class ApproximateTextMeasurer : ITextMeasurer
    {
        public static ApproximateTextMeasurer Default { get; } = new ApproximateTextMeasurer();

        public Vector2 MeasureString(string text)
        {
            text = text ?? string.Empty;
            return new Vector2(text.Length * 8, 18);
        }
    }
}
