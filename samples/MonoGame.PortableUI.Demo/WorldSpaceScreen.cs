using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Demo
{
    /// <summary>
    ///     World-space demo: a live, interactive UISurface rendered on a perspective 3D quad.
    ///     The mouse is raycast onto the quad (WorldSurfaceMapper) and fed into the surface's
    ///     VirtualInputSource, so the DOS screen on the monitor is fully clickable; keyboard
    ///     text is routed via SurfaceFocusManager.
    /// </summary>
    public sealed class WorldSpaceScreen : Screen
    {
        private const int SurfaceWidth = 640;
        private const int SurfaceHeight = 400;
        private static readonly Vector2 QuadSize = new Vector2(3.2f, 2.0f);

        private readonly Game _game;
        private readonly UISurface _computerSurface;
        private readonly VirtualInputSource _virtualInput;
        private readonly TextBlock _status;
        private BasicEffect? _effect;
        private RenderTarget2D? _surfaceTarget;

        public WorldSpaceScreen(Game game, DemoThemePreset themePreset)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            var preset = themePreset ?? DemoThemeRegistry.Default;
            // Background is drawn manually in OnBeforeDraw so the 3D quad sits on top of it
            // but underneath the 2D chrome.
            BackgroundBrush = null;
            _virtualInput = new VirtualInputSource();
            // The monitor shows the currently selected theme: same theme instance for the
            // surface engine, theme-default styling for the inner controls.
            var computerScreen = new MonitorScreen(preset, message => _status!.Text = $"Monitor: {message}");
            _computerSurface = new UISurface(_game, computerScreen, SurfaceWidth, SurfaceHeight, preset.CreateTheme())
            {
                InputSource = _virtualInput,
                ShowSoftwareCursor = true,
                SoftwareCursorColor = preset.Palette.Secondary
            };
            _status = new TextBlock
            {
                Text = "World space demo - click the monitor; type into the DOS prompt",
                TextColor = Color.White,
                TextSize = 14,
                Margin = new Thickness(12)
            };
            Content = CreateChrome();
        }

        protected override void OnBeforeDraw(SpriteBatch spriteBatch)
        {
            var device = spriteBatch.GraphicsDevice;
            var gameTime = new GameTime(ScreenSystem.TotalTime, TimeSpan.FromMilliseconds(16));
            var time = (float)ScreenSystem.TotalTime.TotalSeconds;

            // Slight sway so the perspective is obvious.
            var world = Matrix.CreateRotationY((float)Math.Sin(time * 0.45) * 0.32f)
                        * Matrix.CreateRotationX(-0.06f);
            var view = Matrix.CreateLookAt(new Vector3(0, 0.15f, 3.1f), Vector3.Zero, Vector3.Up);
            var viewport = device.Viewport;
            var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), viewport.AspectRatio, 0.1f, 20f);

            RouteMouseIntoSurface(viewport, view, projection, world);

            _computerSurface.Update(gameTime);
            _surfaceTarget = _computerSurface.Draw(gameTime);

            DrawRoomBackground(spriteBatch);
            DrawMonitorQuad(device, world, view, projection);
        }

        private void RouteMouseIntoSurface(Viewport viewport, Matrix view, Matrix projection, Matrix world)
        {
            var mouse = Mouse.GetState();
            var ray = WorldSurfaceMapper.GetMouseRay(viewport, view, projection, new PointF(mouse.X, mouse.Y));
            if (WorldSurfaceMapper.TryMapRayToSurface(ray, world, QuadSize, SurfaceWidth, SurfaceHeight, out var uiPoint))
            {
                _virtualInput.SetPointer(uiPoint, mouse.LeftButton == ButtonState.Pressed, mouse.RightButton == ButtonState.Pressed, false);
                _computerSurface.SoftwareCursorPosition = uiPoint;
                _computerSurface.ShowSoftwareCursor = true;
            }
            else
            {
                _virtualInput.SetPointer(new PointF(-100, -100), false, false, false);
                _computerSurface.ShowSoftwareCursor = false;
            }
        }

        private void DrawRoomBackground(SpriteBatch spriteBatch)
        {
            var gradient = new GradientBrush(new Color(23, 20, 18), new Color(64, 48, 38), GradientDirection.Vertical);
            spriteBatch.Begin();
            gradient.Draw(spriteBatch, ScreenRect);
            spriteBatch.End();
        }

        private void DrawMonitorQuad(GraphicsDevice device, Matrix world, Matrix view, Matrix projection)
        {
            _effect ??= new BasicEffect(device)
            {
                TextureEnabled = true,
                VertexColorEnabled = true,
                LightingEnabled = false
            };
            _effect.World = world;
            _effect.View = view;
            _effect.Projection = projection;

            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            device.SamplerStates[0] = SamplerState.LinearClamp;

            // Bezel: slightly larger dark quad behind the screen.
            _effect.TextureEnabled = false;
            DrawQuad(device, QuadSize + new Vector2(0.34f, 0.34f), new Color(52, 47, 41), -0.02f);
            _effect.TextureEnabled = true;
            _effect.Texture = _surfaceTarget;
            DrawQuad(device, QuadSize, Color.White, 0);
        }

        private void DrawQuad(GraphicsDevice device, Vector2 size, Color color, float z)
        {
            var halfX = size.X / 2;
            var halfY = size.Y / 2;
            var vertices = new[]
            {
                new VertexPositionColorTexture(new Vector3(-halfX, halfY, z), color, new Vector2(0, 0)),
                new VertexPositionColorTexture(new Vector3(halfX, halfY, z), color, new Vector2(1, 0)),
                new VertexPositionColorTexture(new Vector3(-halfX, -halfY, z), color, new Vector2(0, 1)),
                new VertexPositionColorTexture(new Vector3(halfX, -halfY, z), color, new Vector2(1, 1))
            };
            var indices = new short[] { 0, 1, 2, 1, 3, 2 };

            foreach (var pass in _effect!.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2);
            }
        }

        private Control CreateChrome()
        {
            var root = new Grid
            {
                Margin = 18,
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition(),
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            root.AddChild(new TextBlock
            {
                Text = "WORLD SPACE DEMO",
                TextColor = new Color(255, 214, 130),
                TextSize = 18,
                Margin = new Thickness(0, 0, 0, 4)
            });

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
            root.AddChild(bar, row: 2);
            return root;
        }

        private sealed class MonitorScreen : Screen
        {
            private readonly Action<string> _report;
            private readonly TextBlock _innerStatus;
            private readonly DemoThemePreset _preset;

            public MonitorScreen(DemoThemePreset preset, Action<string> report)
            {
                _preset = preset;
                _report = report;
                BackgroundBrush = preset.Palette.BackgroundBrush ?? new SolidColorBrush(preset.Palette.Background);
                _innerStatus = new TextBlock
                {
                    Text = "READY.",
                    TextColor = preset.Palette.MutedText,
                    TextSize = 14,
                    Margin = new Thickness(0, 6, 0, 0)
                };
                Content = CreateDesktop();
            }

            private Control CreateDesktop()
            {
                var palette = _preset.Palette;
                var root = new Grid
                {
                    Margin = 16,
                    RowDefinitions =
                    {
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition(),
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = GridLength.Auto }
                    }
                };

                root.AddChild(new TextBlock
                {
                    Text = $"PORTABLEUI TERMINAL - {_preset.DisplayName.ToUpperInvariant()}",
                    TextColor = palette.HeadingText,
                    TextSize = 14,
                    Margin = new Thickness(0, 0, 0, 8)
                });

                // Theme-default styling on purpose: the monitor mirrors the selected theme.
                var list = new ListBox { ItemHeight = 24 };
                foreach (var item in new[] { "README.TXT", "THEMES", "DOS_SURF.EXE", "LCARS.PNL", "SAVEGAME.001", "AUTOEXEC.BAT" })
                    list.Items.Add(item);
                list.SelectedIndex = 2;
                list.SelectionChanged += (sender, args) => Report($"selected {list.SelectedItem}");
                root.AddChild(list, row: 1);

                var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
                var run = new TextButton("RUN") { Width = 120, Height = 30, Margin = new Thickness(0, 0, 8, 0) };
                run.Click += (sender, args) => Report($"running {list.SelectedItem}...");
                var reboot = new TextButton("REBOOT") { Width = 120, Height = 30 };
                reboot.Click += (sender, args) => Report("system rebooted");
                buttons.AddChild(run);
                buttons.AddChild(reboot);
                root.AddChild(buttons, row: 2);

                // Theme-driven field (glass themes stay glassy, DOS stays blue); the frame keeps
                // the prompt visible in styleless themes like the default one.
                var prompt = new TextBox
                {
                    Text = "C:\\PORTABLEUI>",
                    Height = 30
                };
                var promptFrame = new Border
                {
                    Margin = new Thickness(0, 8, 0, 0),
                    BorderColor = palette.FieldBorder,
                    BorderWidth = 1,
                    Padding = new Thickness(2),
                    Content = prompt
                };
                root.AddChild(promptFrame, row: 3);
                root.AddChild(_innerStatus, row: 4);
                return root;
            }

            private void Report(string message)
            {
                _innerStatus.Text = message.ToUpperInvariant();
                _report(message);
            }
        }
    }
}
