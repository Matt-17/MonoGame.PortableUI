using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    internal sealed class ToolTipPopup : Border
    {
        public ToolTipPopup(string text)
        {
            BackgroundBrush = new Color(31, 35, 39, 238);
            BorderColor = new Color(255, 255, 255, 90);
            BorderWidth = 1;
            Padding = new Thickness(8, 5, 8, 6);
            Content = new TextBlock
            {
                Text = text,
                TextColor = Color.White,
                Margin = 0
            };
        }
    }
}
