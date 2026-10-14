using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    internal sealed class ToolTipPopup : Border
    {
        private readonly TextBlock _text = new TextBlock { Margin = 0 };

        public ToolTipPopup(string text)
        {
            Content = _text;
            Apply(text);
        }

        /// <summary>Takes the current theme and a new text: a screen keeps one popup and reuses it for every tooltip.</summary>
        internal void Apply(string text)
        {
            var theme = PortableTheme.ResolveCurrent();

            BackgroundBrush = theme.ToolTipBackgroundBrush;
            BorderColor = theme.ToolTipBorderBrush;
            BorderWidth = theme.ToolTipBorderWidth;
            Padding = theme.ToolTipPadding;
            _text.Text = text;
            _text.TextColor = theme.ToolTipTextColor;
        }
    }
}
