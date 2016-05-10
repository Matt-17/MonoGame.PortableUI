using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace SampleClient.Screens
{
    public class StartScreen : Screen
    {
        public StartScreen()
        {
            BackgroundColor = Color.CornflowerBlue;
            var button = new Button
            {
                BackgroundColor = Color.DarkRed,
                Text = "Go to Screen 2",
                Margin = new Thickness(10),
                Height = 50,
                //Width = 100
            };
            var button2 = new Button()
            {
                BackgroundColor = Color.Yellow,
                Text = "Test 2",
                //Width = 200,
                Height = 100
            };
            var button3 = new Button()
            {
                BackgroundColor = Color.DarkGreen,
                Text = "3"
            };
            var button4 = new Button()
            {
                BackgroundColor = Color.DarkGreen,
                Text = "4"
            };
            button.Click += Button_Click;
            button2.Click += Button2_Click;
            var grid = new Grid()
            {
                //Width = 240,
                Height = 250,
                Margin = new Thickness(100, 50),
                BackgroundColor = Color.DarkOrange,
                ColumnDefinitions = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Width = new GridLength(2, GridLengthUnit.Relative)},
                    new ColumnDefinition(),
                    new ColumnDefinition { Width = new GridLength(20)},
                },
                RowDefinitions = new List<RowDefinition>
                {
                    new RowDefinition(),
                    new RowDefinition(),
                    new RowDefinition { Height = new GridLength(50)},
                    new RowDefinition { Height = new GridLength(3.5f, GridLengthUnit.Relative) }
                }
            };
            //grid.AddChild(button);
            //grid.AddChild(button2, 2, 2);
            //grid.AddChild(button4, 1, 1);
            //grid.AddChild(button3, 3, 1);
            var stackPanel = new StackPanel()
            {
                Width = 600,
                Height = 400,
                Padding = new Thickness(50, 50),
                Margin = new Thickness(20, 20),
                BackgroundColor = Color.Blue,
                Orientation = Orientation.Vertical,
                Children = { button2, button }
            };
            //stackPanel.AddChild(button3);
            //stackPanel.AddChild(button4);
            Content = stackPanel;
        }

        private void Button2_Click(object sender, EventArgs e)
        {

        }

        private void Button_Click(object sender, EventArgs e)
        {
            ScreenEngine.NavigateToScreen(new SecondScreen());
        }
    }
}