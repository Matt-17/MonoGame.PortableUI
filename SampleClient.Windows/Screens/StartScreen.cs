using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace SampleClient.Screens
{
    public class StartScreen : Screen
    {
        public StartScreen()
        {
            var button = new Button
            {
                BackgroundBrush = Color.DarkRed,
                Text = "Go to Screen 2",
                Margin = new Thickness(10),
                Height = 50,
                Width = 100
            };
            var button2 = new ToggleButton()
            {
                Text = "Unchecked",
                //Width = 200,
                Height = 100,
                BackgroundBrush = new GradientBrush(Color.Red, Color.Orange),
                ToggleBrush = new GradientBrush(Color.YellowGreen, Color.Orange)
            };
            var button3 = new Button()
            {
                BackgroundBrush = Color.DarkGreen,
                Text = "3",
                Width = 50,
                Height = 200
            };
            var button4 = new Button()
            {
                BackgroundBrush = Color.DarkGreen,
                Text = "4",
                Width = 50,
                //Height = 100
            };
            button.Click += Button_Click;
            button2.Checked += Button2_Checked;

            var grid = new Grid()
            {                   
                Margin = new Thickness(100, 50),
                BackgroundBrush = Color.DarkOrange,
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
                    new RowDefinition { Height = new GridLength(30)},
                    new RowDefinition { Height = new GridLength(3.5f, GridLengthUnit.Relative) }
                }
            };
            //grid.AddChild(button);
            //grid.AddChild(button2, 2, 2);
            //grid.AddChild(button4, 0, 1);
            //grid.AddChild(button3, 2, 2);
            var border = new Border()
            {
                BorderColor = Color.Lime,
                BackgroundBrush = Color.LimeGreen,
                Height = 50,
                BorderWidth = new Thickness(5)
            };
            var innerStackPanel = new StackPanel()
            {
                BackgroundBrush = Color.DeepSkyBlue,
                Orientation = Orientation.Horizontal,
                Children = {
                    button3,
                    button4,      
                },
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            var stackPanel = new StackPanel()
            {
                Padding = new Thickness(50, 50),
                Margin = new Thickness(20, 20),
                BackgroundBrush = Color.Blue,
                Orientation = Orientation.Vertical,
                //HorizontalAlignment = HorizontalAlignment.Right,
                //VerticalAlignment = VerticalAlignment.Top,
                Children = {
                    button2,
                    button,
                    grid,
                }
            };
            //   grid.AddChild(stackPanel, 3, 0, 0, 3);
            Content = stackPanel;
        }

        private void Button2_Checked(object sender, CheckedEventArgs e)
        {
            var button = sender as ToggleButton;
            if (button == null)
                return;
            button.Text = e.IsChecked ? "Checked" : "Unchecked";
            button.Width = e.IsChecked ? 700 : 200;
            InvalidateLayout(true);
        }

        private void Button_Click(object sender, EventArgs e)
        {
            ScreenEngine.NavigateToScreen(new SecondScreen());
        }
    }
}