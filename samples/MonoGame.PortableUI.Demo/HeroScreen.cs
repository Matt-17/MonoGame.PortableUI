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

        public HeroScreen(DemoThemePreset themePreset, Color cardColor, string title, ScreenTransition transition)
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

            // A scrim and caption over where the card lands: drawn above the flying card the whole time, so the card
            // does not pass over them and then suddenly sit under them.
            root.AddChild(new Border
            {
                Height = 72,
                VerticalAlignment = VerticalAlignment.Bottom,
                CornerRadius = new CornerRadius(0, 0, 16, 16),
                BackgroundBrush = new LinearGradientBrush(Color.Transparent, new Color(0, 0, 0, 150)),
                DrawsAboveSharedFlights = true,
                Content = new TextBlock
                {
                    Text = "Caption above the flight",
                    TextColor = Color.White,
                    TextSize = 18,
                    Margin = new Thickness(20, 0, 0, 12),
                    VerticalAlignment = VerticalAlignment.Bottom
                }
            });

            root.AddChild(new TextBlock
            {
                Text = $"Screen transition: {transition}. The card flew here from the Motion tab: same SharedElementKey on both screens.",
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
