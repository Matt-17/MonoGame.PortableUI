using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Demo
{
    public sealed class AdventureRoomScreen : Screen
    {
        private readonly Game _game;
        private readonly UISurface _computerSurface;
        private readonly Image _monitorImage;
        private readonly TextBlock _status;

        public AdventureRoomScreen(Game game)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            BackgroundBrush = new GradientBrush(new Color(23, 20, 18), new Color(64, 48, 38), GradientDirection.Vertical);
            _computerSurface = new UISurface(_game, new DosComputerScreen(), 640, 400, ThemeRegistry.Resolve("dos").CreateTheme())
            {
                InputSource = new VirtualInputSource(),
                SoftwareCursorPosition = new PointF(42, 42),
                SoftwareCursorColor = new Color(255, 255, 85)
            };
            _monitorImage = new Image
            {
                Stretch = Stretch.Fill
            };
            _status = new TextBlock
            {
                Text = "Adventure Room - DOS UISurface on monitor prop",
                TextColor = Color.White,
                TextSize = 14,
                Margin = new Thickness(12)
            };
            Content = CreateLayout();
        }

        protected override void OnBeforeDraw(SpriteBatch spriteBatch)
        {
            var gameTime = new GameTime(ScreenSystem.TotalTime, TimeSpan.FromMilliseconds(16));
            _computerSurface.Update(gameTime);
            _monitorImage.Source = _computerSurface.Draw(gameTime);
        }

        private Control CreateLayout()
        {
            var root = new Grid
            {
                Margin = 18,
                RowDefinitions =
                {
                    new RowDefinition(),
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            var room = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(),
                    new ColumnDefinition { Width = new GridLength(640) },
                    new ColumnDefinition()
                },
                RowDefinitions =
                {
                    new RowDefinition(),
                    new RowDefinition { Height = new GridLength(430) },
                    new RowDefinition()
                }
            };

            var monitor = new Border
            {
                BackgroundBrush = new SolidColorBrush(new Color(14, 14, 16)),
                BorderBrush = new SolidColorBrush(new Color(98, 89, 79)),
                BorderThickness = 10,
                CornerRadius = 8,
                Padding = 18,
                Shadow = ShadowStyle.Level3(),
                Content = _monitorImage
            };
            room.AddChild(monitor, row: 1, column: 1);
            root.AddChild(room);

            var bar = new Grid
            {
                BackgroundBrush = new SolidColorBrush(new Color(0, 0, 0, 150)),
                ColumnDefinitions =
                {
                    new ColumnDefinition(),
                    new ColumnDefinition { Width = new GridLength(180) }
                }
            };
            bar.AddChild(_status);
            var back = new TextButton("Back to demo")
            {
                Height = 36,
                Margin = new Thickness(0, 8, 8, 8),
                BackgroundBrush = new SolidColorBrush(new Color(255, 255, 255, 36)),
                TextColor = Color.White
            };
            back.Click += (sender, args) => ScreenEngine?.NavigateBack();
            bar.AddChild(back, column: 1);
            root.AddChild(bar, row: 1);
            return root;
        }

        private sealed class DosComputerScreen : Screen
        {
            public DosComputerScreen()
            {
                BackgroundBrush = new SolidColorBrush(new Color(0, 0, 168));
                Content = CreateDesktop();
            }

            private static Control CreateDesktop()
            {
                var root = new Grid
                {
                    Margin = 16,
                    RowDefinitions =
                    {
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition(),
                        new RowDefinition { Height = GridLength.Auto }
                    }
                };

                root.AddChild(new TextBlock
                {
                    Text = "NORTON PORTABLEUI COMMANDER",
                    TextColor = new Color(255, 255, 85),
                    TextSize = 14,
                    Margin = new Thickness(0, 0, 0, 8)
                });

                var list = new ListBox
                {
                    BackgroundBrush = new SolidColorBrush(new Color(0, 0, 168)),
                    ItemBackgroundBrush = new SolidColorBrush(new Color(0, 0, 168)),
                    SelectedItemBackgroundBrush = new SolidColorBrush(new Color(0, 168, 168)),
                    ItemTextColor = Color.White,
                    SelectedItemTextColor = Color.Black,
                    ItemHeight = 24
                };
                foreach (var item in new[] { "README.TXT", "THEMES", "DOS_SURF.EXE", "LCARS.PNL", "SAVEGAME.001", "AUTOEXEC.BAT" })
                    list.Items.Add(item);
                list.SelectedIndex = 2;
                root.AddChild(list, row: 1);

                root.AddChild(new TextBox
                {
                    Text = "C:\\PORTABLEUI>",
                    Height = 30,
                    BackgroundBrush = Color.Black,
                    TextColor = Color.White,
                    CursorColor = new SolidColorBrush(new Color(255, 255, 85))
                }, row: 2);
                return root;
            }
        }
    }
}
