using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Demo
{
    /// <summary>
    ///     Detail screen of the shared-element demo: its header card has the same <see cref="Control.SharedElementKey"/>
    ///     as a card on the Motion tab, so navigating here (and back) flies the card between both places.
    /// </summary>
    public sealed class HeroScreen : Screen
    {
        public const string CardKey = "motion-hero-card";

        public HeroScreen(DemoThemePreset themePreset, Color cardColor, string title)
        {
            var palette = themePreset.Palette;
            BackgroundBrush = palette.BackgroundBrush ?? themePreset.BackgroundColor;

            var root = new Grid
            {
                Margin = 24,
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(220) },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition()
                }
            };

            var cardTitle = new TextBlock
            {
                Text = title,
                TextColor = Color.White,
                TextSize = 36,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            root.AddChild(new Border
            {
                CornerRadius = 16,
                BackgroundBrush = new SolidColorBrush(cardColor),
                SharedElementKey = CardKey,
                Content = cardTitle
            });

            root.AddChild(new TextBlock
            {
                Text = "The card flew here from the Motion tab: same SharedElementKey on both screens.",
                TextColor = palette.MutedText,
                TextSize = 16,
                Margin = new Thickness(0, 16, 0, 12)
            }, row: 1);

            var back = new TextButton("Back")
            {
                Height = 44,
                Width = 140,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            back.Click += (_, _) => ScreenEngine?.NavigateBack();
            root.AddChild(back, row: 2);
            Content = root;
        }
    }
}
