using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Demo
{
    /// <summary>
    ///     --benchmark-scale out.csv: frame times while a shadowed, rounded card with a solid, gradient or glass background
    ///     grows and shrinks through an implicit Scale transition - every frame draws it at a size it was never drawn at.
    ///     GPU time is included.
    /// </summary>
    internal static class ScaleBenchmark
    {
        private const int Width = 1080;
        private const int Height = 1920;
        private const int Pulses = 6;

        private sealed class CardPage : Screen
        {
            public CardPage(Brush brush)
            {
                BackgroundBrush = new LinearGradientBrush(new Color(255, 140, 60), new Color(40, 90, 200)) { AngleDegrees = 60 };
                Card = new Border
                {
                    Width = 720,
                    Height = 420,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    CornerRadius = new CornerRadius(28),
                    BackgroundBrush = brush,
                    Shadow = new ShadowStyle { Blur = 24, Offset = new Vector2(0, 8), Color = new Color(0, 0, 0, 90) },
                    Content = new TextBlock { Text = "Scaling card", TextSize = 40, Margin = new Thickness(32) }
                };
                Card.Transitions.Add(TransitionProperty.Scale, TimeSpan.FromMilliseconds(400));
                var stack = new Grid();
                for (var i = 0; i < 12; i++)
                    stack.AddChild(new TextBlock { Text = "Backdrop line " + i, TextSize = 30, Margin = new Thickness(40, 80 + i * 140, 0, 0) });
                stack.AddChild(Card);
                Content = stack;
            }

            public Border Card { get; }
        }

        public static void Run(Game game, string file)
        {
            var lines = new List<string> { "brush,frames,mean_ms,worst_ms,frames_over_17ms" };
            var brushes = new (string Name, Func<Brush> Create)[]
            {
                ("solid", () => new SolidColorBrush(new Color(250, 250, 252))),
                ("gradient", () => new LinearGradientBrush(new Color(255, 255, 255), new Color(210, 220, 240))),
                ("acrylic", () => new AcrylicBrush(new Color(255, 255, 255, 120))),
                ("liquid-glass", () => new LiquidGlassBrush(new Color(255, 255, 255, 60)))
            };
            foreach (var (name, create) in brushes)
            {
                var page = new CardPage(create());
                using var surface = new UISurface(game, page, Width, Height, DemoThemeRegistry.Default.CreateTheme())
                {
                    ShowSoftwareCursor = false,
                    InputSource = PortableUI.Input.NullInputSource.Instance
                };
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
                    // Wait for the GPU so the time covers the actual rendering.
                    target.GetData(0, new Rectangle(0, 0, 1, 1), probe, 0, 1);
                    return watch.Elapsed.TotalMilliseconds;
                }
                for (var i = 0; i < 20; i++)
                    Frame();
                var frames = 0;
                var total = 0.0;
                var worst = 0.0;
                var over = 0;
                var trace = new System.Text.StringBuilder();
                for (var pulse = 0; pulse < Pulses; pulse++)
                {
                    // A different end scale each pulse, so no size repeats.
                    page.Card.Scale = pulse % 2 == 0 ? new Vector2(1.13f + pulse * 0.011f) : Vector2.One;
                    for (var i = 0; i < 30; i++)
                    {
                        var ms = Frame();
                        frames++;
                        total += ms;
                        worst = Math.Max(worst, ms);
                        if (ms > 17)
                            over++;
                        if (pulse == 0)
                            trace.Append(ms.ToString("0.0", CultureInfo.InvariantCulture)).Append(' ');
                    }
                }
                var line = string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:0.00},{3:0.0},{4}", name, frames, total / frames, worst, over);
                lines.Add(line);
                Console.WriteLine(line + "  [" + trace.ToString().TrimEnd() + "]");
            }
            game.GraphicsDevice.SetRenderTarget(null);
            File.WriteAllLines(file, lines);
        }
    }
}
