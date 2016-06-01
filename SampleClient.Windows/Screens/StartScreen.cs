using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                Width = 200
            };
            var button2 = new ToggleButton()
            {
                Text = "Unchecked",
                Width = 200,
                Height = 100,
                BackgroundBrush = new GradientBrush(Color.Red, Color.Orange),
                ToggleBrush = new GradientBrush(Color.YellowGreen, Color.Orange),
                ToggleTextColor = Color.White
            };
            var button3 = new Button()
            {
                BackgroundBrush = Color.DarkGreen,
                Text = "3",
                HoverTextColor = Color.White,
                PressedTextColor = Color.White
            };
            var button4 = new Button()
            {
                BackgroundBrush = Color.DarkGreen,
                Text = "4",
                Padding = new Thickness(10),
                Margin = new Thickness(10),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            button.Click += Button_Click;
            button2.Checked += Button2_Checked;
            button3.LongPress += Button3_LongClick;
            button3.Click += Button3_Click;
            var grid = new Grid()
            {
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
                },
                Width = 140,
                Height = 140
            };
            //grid.AddChild(button);
            //grid.AddChild(button2, 2, 2);
            //grid.AddChild(button4, 0, 1);
            //grid.AddChild(button3, 2, 2);
            var innerStackPanel = new StackPanel()
            {
                BackgroundBrush = Color.DeepSkyBlue,
                Orientation = Orientation.Horizontal,
                Children = {
                    button3,
                    button4,
                    grid
                },

            };

            grid.AddChild(new Border()
            {
                BorderColor = Color.Lime,
                BackgroundBrush = Color.LimeGreen,
                BorderWidth = new Thickness(5)
            }, 3, 1);
            grid.AddChild(new Border()
            {
                BackgroundBrush = Color.LimeGreen,
            }, 2, 2);

            var control = new Button
            {
                Text = "nummer 1",
                Padding = new Thickness(10),
            };
            control.Click += Control_Click;
            var stackPanel = new StackPanel()
            {
                Margin = new Thickness(20, 20),
                BackgroundBrush = Color.Blue,
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Right,
                //VerticalAlignment = VerticalAlignment.Top,
                Children = {
                    button2,
                    button,
                    innerStackPanel,
                    new ScrollViewer() {
                        Height = 100,
                        BackgroundBrush = Color.Khaki,
                        Content =
                        new StackPanel {
                            BackgroundBrush = Color.Lavender,
                            Margin = new Thickness(10),
                            Children = {
                                control,
                                new Button {
                                    Text = "nummer 2",
                                    Padding = new Thickness(10),
                                },
                                new Button {
                                    Text = "nummer 3",
                                    Padding = new Thickness(10),
                                },
                                new Button {
                                    Text = "nummer 4",
                                    Padding = new Thickness(10),
                                },
                                new Button {
                                    Text = "nummer 5",
                                    Padding = new Thickness(10),
                                },
                            }
                        }
                    }
                }
            };
            //   grid.AddChild(stackPanel, 3, 0, 0, 3);
            Content = stackPanel;
        }

        private void Control_Click(object sender, EventArgs e)
        {
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Click " + (sender as Button).Text);
        }
        private void Button3_LongClick(object sender, EventArgs e)
        {
            Debug.WriteLine("LongPress " + (sender as Button).Text);
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