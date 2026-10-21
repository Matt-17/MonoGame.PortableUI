using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Demo
{
    /// <summary>
    ///     --benchmark-navigation out.csv: frame times while a never-seen page slides or fades in. Every page uses text
    ///     sizes, shadow blurs, corner radii and gradient sizes no page before it used, so glyphs, masks and gradient
    ///     textures are built on first use as in an app opening a new page. Reports the first frame (navigation) and the
    ///     worst frame while the transition moves - a hitch there makes the page jump. GPU time is included.
    /// </summary>
    internal static class NavigationBenchmark
    {
        private const int Width = 1080;
        private const int Height = 1920;
        private const int Pages = 12;

        private sealed class ColdPage : Screen
        {
            public ColdPage(int seed)
            {
                BackgroundBrush = new SolidColorBrush(new Color(245, 245, 248));
                var stack = new StackPanel { Margin = new Thickness(24) };
                stack.AddChild(new TextBlock { Text = "Page " + seed + " title", TextSize = 41 + seed * 1.37f });
                stack.AddChild(new TextBlock { Text = "A subtitle that wraps over a line or two to show body text", TextSize = 17 + seed * 0.61f });
                for (var i = 0; i < 4; i++)
                {
                    var card = new Border
                    {
                        Height = 150 + seed * 3 + i * 11,
                        Margin = new Thickness(0, 16, 0, 0),
                        CornerRadius = new CornerRadius(9 + seed * 2 + i),
                        BackgroundBrush = new LinearGradientBrush(new Color(80 + i * 30, 120, 200), new Color(30, 60 + i * 20, 120)),
                        Shadow = new ShadowStyle { Blur = 7 + seed * 1.9f + i * 3, Offset = new Vector2(0, 4 + i) }
                    };
                    card.Content = new TextBlock
                    {
                        Text = "Card " + i,
                        TextSize = 23 + seed * 0.83f + i * 2.1f,
                        TextColor = Color.White,
                        Margin = new Thickness(16)
                    };
                    stack.AddChild(card);
                }
                var chart = new DrawingCanvas { Height = 220, Margin = new Thickness(0, 16, 0, 0) };
                chart.Render += (canvas, context) =>
                {
                    var rect = canvas.ClippingRect;
                    for (var i = 0; i < 12; i++)
                        context.FillCircle(new Vector2(rect.Left + 30 + i * 70, rect.Top + 110 + MathF.Sin(i + seed) * 60), 9 + seed % 5, Color.OrangeRed);
                    context.DrawRectangle(rect, Color.Gray, 2);
                };
                stack.AddChild(chart);
                Content = new ScrollViewer { Content = stack };
            }
        }

        private sealed class HomePage : Screen
        {
            public HomePage()
            {
                BackgroundBrush = new SolidColorBrush(Color.White);
                Content = new TextBlock { Text = "Home", Margin = new Thickness(24) };
            }
        }

        public static void Run(Game game, string file)
        {
            var lines = new List<string> { "transition,page,first_frame_ms,worst_moving_frame_ms,frames_over_17ms,total_ms" };
            var seed = 0;
            foreach (var transition in new[] { ScreenTransition.SlideFromRight, ScreenTransition.Fade, ScreenTransition.SlideFromRight, ScreenTransition.Fade })
            {
                var home = new HomePage();
                using var surface = new UISurface(game, home, Width, Height, DemoThemeRegistry.Default.CreateTheme())
                {
                    ShowSoftwareCursor = false,
                    InputSource = PortableUI.Input.NullInputSource.Instance
                };
                surface.Engine.TransitionDuration = TimeSpan.FromMilliseconds(300);
                // --navigation-no-warmup: the engine draws no page ahead of its transition, for comparison.
                surface.Engine.WarmUpScreens = Array.IndexOf(Environment.GetCommandLineArgs(), "--navigation-no-warmup") < 0;
                var probe = new Color[1];
                var time = TimeSpan.FromSeconds(1);
                var step = TimeSpan.FromMilliseconds(16);
                var watch = new Stopwatch();
                double Frame()
                {
                    watch.Restart();
                    time += step;
                    surface.Update(new GameTime(time, step));
                    var target = surface.Draw(new GameTime(time, step));
                    // Wait for the GPU so the time covers the actual rendering (MonoGame reads the whole target here,
                    // so every frame allocates a screen of pixels: a GC every few frames, alike before and after).
                    target.GetData(0, new Rectangle(0, 0, 1, 1), probe, 0, 1);
                    return watch.Elapsed.TotalMilliseconds;
                }
                for (var i = 0; i < 10; i++)
                    Frame();
                for (var page = 0; page < Pages; page++)
                {
                    surface.Engine.NavigateToScreen(new ColdPage(seed++), transition);
                    var first = Frame();
                    var worst = 0.0;
                    var over = first > 17 ? 1 : 0;
                    var total = first;
                    var trace = new System.Text.StringBuilder().Append(first.ToString("0.0", CultureInfo.InvariantCulture));
                    while (surface.Engine.IsTransitioning)
                    {
                        var ms = Frame();
                        worst = Math.Max(worst, ms);
                        total += ms;
                        trace.Append(' ').Append(ms.ToString("0.0", CultureInfo.InvariantCulture));
                        if (ms > 17)
                            over++;
                    }
                    surface.Engine.NavigateBack(ScreenTransition.None);
                    for (var i = 0; i < 3; i++)
                        Frame();
                    var line = string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:0.0},{3:0.0},{4},{5:0.0}", transition, page, first, worst, over, total);
                    lines.Add(line);
                    // The frames one by one: where a hitch lands.
                    Console.WriteLine(line + "  [" + trace + "]");
                }
            }
            game.GraphicsDevice.SetRenderTarget(null);
            File.WriteAllLines(file, lines);
        }
    }
}
