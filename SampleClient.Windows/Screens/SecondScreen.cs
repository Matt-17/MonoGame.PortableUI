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
            BackgroundColor = Color.LimeGreen;
            var button = new Button()
            {
                Width = 240,
                Height = 50,
                Margin = new Thickness(5),
                BackgroundColor = Color.DarkRed,
                Text = "Click this button"
            };
            button.Click += Button_Click;

            var stackPanel = new StackPanel();
            stackPanel.AddChild(button);
            button = new Button()
            {
                Width = 240,
                Height = 50,
                Margin = new Thickness(5),
                BackgroundColor = Color.DarkRed,
                Text = "Back to Screen 1"
            };
            button.Click += Button1_Click;
            stackPanel.AddChild(button);
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