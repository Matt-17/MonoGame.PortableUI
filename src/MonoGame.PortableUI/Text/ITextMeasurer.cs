using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Text
{
    public interface ITextMeasurer
    {
        Vector2 MeasureString(string text);
    }
}
