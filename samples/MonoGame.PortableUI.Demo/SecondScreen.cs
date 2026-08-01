using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Demo
{
    public sealed class SecondScreen : Screen
    {
        public SecondScreen()
        {
            BackgroundBrush = C64Theme.Blue;

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = 24,
                BackgroundBrush = C64Theme.Blue
            };

            panel.AddChild(new TextBlock
            {
                Text = "Second screen",
                TextColor = C64Theme.White,
                TextSize = 18,
                Margin = new Thickness(12, 12, 12, 4)
            });

            var back = new TextButton("Navigate back")
            {
                Height = 48,
                Margin = new Thickness(12, 8, 12, 12),
                BackgroundBrush = C64Theme.DarkBlue,
                TextColor = C64Theme.White,
                HoverTextColor = C64Theme.White,
                PressedTextColor = C64Theme.Blue
            };
            back.Click += (sender, args) => ScreenEngine?.NavigateBack();
            panel.AddChild(back);

            Content = panel;
        }
    }
}
