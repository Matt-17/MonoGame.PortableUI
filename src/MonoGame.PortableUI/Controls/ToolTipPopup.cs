using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    internal sealed class ToolTipPopup : Border
    {
        public ToolTipPopup(string text)
        {
            var theme = PortableTheme.ResolveCurrent();

            BackgroundBrush = theme.ToolTipBackgroundBrush;
            BorderColor = theme.ToolTipBorderBrush;
            BorderWidth = theme.ToolTipBorderWidth;
            Padding = theme.ToolTipPadding;
            Content = new TextBlock
            {
                Text = text,
                TextColor = theme.ToolTipTextColor,
                Margin = 0
            };
        }
    }
}
