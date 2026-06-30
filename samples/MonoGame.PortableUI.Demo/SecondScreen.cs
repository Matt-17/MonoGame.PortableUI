using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Demo
{
    public sealed class SecondScreen : Screen
    {
        public SecondScreen()
        {
            BackgroundBrush = new Color(24, 33, 43);

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = 24,
                BackgroundBrush = new Color(240, 243, 246)
            };

            panel.AddChild(new TextBlock
            {
                Text = "Second screen",
                TextColor = new Color(35, 38, 43),
                TextSize = 18,
                Margin = new Thickness(12, 12, 12, 4)
            });

            var back = new TextButton("Navigate back")
            {
                Height = 48,
                Margin = new Thickness(12, 8, 12, 12),
                BackgroundBrush = Color.White,
                TextColor = new Color(35, 38, 43)
            };
            back.Click += (sender, args) => ScreenEngine.NavigateBack();
            panel.AddChild(back);

            Content = panel;
        }
    }
}
