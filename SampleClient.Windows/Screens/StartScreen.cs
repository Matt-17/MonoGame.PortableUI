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
            var button = new Button()
            {
                Width = 240,
                Height = 50,
                Top = 50,
                Left = 100,
                BackgroundColor = Color.DarkRed,
                Text = "Go to Screen 2"
            };
            button.Click += Button_Click;
            Content = button;
        }

        private void Button_Click(object sender, System.EventArgs e)
        {
            ScreenEngine.NavigateToScreen(new SecondScreen());
        }
    }
}