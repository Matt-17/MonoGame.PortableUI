using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace SampleClient.Screens
{
    public class SecondScreen : Screen
    {
        public SecondScreen()
        {
            BackgroundBrush = new Color(Color.LimeGreen, 0.4f);
            var button = new RadioButton()
            {
                Width = 240,
                Height = 50,
                Margin = new Thickness(5),
                BackgroundBrush = Color.DarkRed,
                Text = "Click this button",

            };
            button.Click += Button_Click;

            var stackPanel = new StackPanel();
            stackPanel.Children.Add(button);
            button = new RadioButton()
            {
                Width = 240,
                Height = 50,
                Margin = new Thickness(5),
                BackgroundBrush = Color.DarkRed,
                Text = "Back to Screen 1"
            };
            button.Click += Button1_Click;
            stackPanel.Children.Add(button);
            for (int i = 0; i < 4; i++)
            {
                stackPanel.Children.Add(new RadioButton
                {
                    BackgroundBrush = new Color(0, 70, 110),
                    ToggleBrush = Color.Yellow,
                    Text = $"Button {i}",
                    Width = 80,
                    Height = 50,
                    RadioGroup = "Tab"
                });
            }
            Content = stackPanel;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            ScreenEngine.NavigateBack();
        }

        private void Button_Click(object sender, System.EventArgs e)
        {
            var button = sender as Button;
            if (button != null)
                button.Text = "This button was clicked";
        }
    }
}