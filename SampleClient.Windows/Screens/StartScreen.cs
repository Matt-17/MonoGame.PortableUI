using Microsoft.Xna.Framework;
using MonoGame.PortableUI;
using MonoGame.PortableUI.Controls;

namespace SampleClient.Screens
{
    public class StartScreen : Screen
    {
        public StartScreen()
        {
            BackgroundColor = Color.CornflowerBlue;
            var button = new Button(SampleGame.GameInstance)
            {
                Width = 240,
                Height = 50,
                Top = 50,
                Left = 100,
                BackgroundColor = Color.DarkRed,
                Text = "Click this button"
            };
            button.Click += Button_Click;
            Content = button;
        }

        private void Button_Click(object sender, System.EventArgs e)
        {
            var button = sender as Button;
            if (button != null)
                button.Text = "This button was clicked";
        }
    }
}