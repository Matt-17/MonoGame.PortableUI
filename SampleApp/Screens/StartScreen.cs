using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Media;

namespace SampleApp.Screens
{
    public class StartScreen : Screen
    {
        public StartScreen()
        {
            var contextMenu = new Grid { Height = 50, Width = 50, BackgroundBrush = Color.MediumVioletRed };
            var child = new Button { Text = "Tt" };
            child.Click += Button_Click;
            contextMenu.AddChild(child);
            var button = new Button
            {
                BackgroundBrush = Color.DarkRed,
                Text = "Go to Screen 2",
                Margin = new Thickness(10),
                Height = 50,
                Width = 200,
                ContextMenu = contextMenu,
            };
            var button2 = new ToggleButton()
            {
                Text = "Unchecked",
                Width = 200,
                Height = 100,
                BackgroundBrush = new GradientBrush(Color.Red, Color.Orange),
                ToggleBrush = new GradientBrush(Color.YellowGreen, Color.Orange),
                ToggleTextColor = Color.White,
            };
            var button3 = new Button()
            {
                BackgroundBrush = Color.DarkGreen,
                Text = "33",
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
            //button.Click += Button_Click;
            button2.Checked += Button2_Checked;
            button3.LongTouch += Button3LongTouch;
            button3.Click += Button3_Click;
            var grid = new Grid()
            {
                BackgroundBrush = Color.DarkOrange,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(2, GridLengthUnit.Relative)},
                    new ColumnDefinition { Width = GridLength.Auto},
                    new ColumnDefinition { Width = new GridLength(20)},
                },
                RowDefinitions =
                {
                    new RowDefinition(),
                    new RowDefinition(),
                    new RowDefinition { Height = new GridLength(30)},
                    new RowDefinition { Height = GridLength.Auto }
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
                    new ProgressIndicator() { Margin = new Thickness(16)},
                    grid
                },

            };

            grid.AddChild(new Border
            {
                BorderColor = Color.Lime,
                BackgroundBrush = Color.LimeGreen,
                BorderWidth = new Thickness(5),
                Width = 45,
                Height = 45,
            }, 3, 1);
            grid.AddChild(new Border
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
                    new TextBox(),
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

            var image = new Image
            {
                Stretch = Stretch.Uniform,
                Source = SampleGame.GameInstance.Content.Load<Texture2D>("Images/ic_delete"),
                Margin = new Thickness(5)
            };

            var image2 = new Image
            {
                Stretch = Stretch.Uniform,
                Source = SampleGame.GameInstance.Content.Load<Texture2D>("Images/ic_delete"),
                Margin = new Thickness(5)
            };

            var image3 = new Image
            {
                Stretch = Stretch.Uniform,
                Source = SampleGame.GameInstance.Content.Load<Texture2D>("Images/ic_delete"),
            };


            var imageButton = new Button()
            {
                BackgroundBrush = Color.Red,
                Padding = new Thickness(0),
                Content = image,
            };
            grid.AddChild(imageButton, 0, 0);
            grid.AddChild(image2, 1, 0);
            grid.AddChild(image3, 2, 0);
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
        private void Button3LongTouch(object sender, EventArgs e)
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