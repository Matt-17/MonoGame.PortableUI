using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Demo.Android
{
    /// <summary>
    ///     A page opened with a slide: title, shadowed gradient cards and a chart. Every page uses text sizes, shadow
    ///     blurs and corner radii no page before it used, so its first draw builds glyphs, masks and gradients like an
    ///     app page opened for the first time - what <see cref="ScreenEngine.WarmUpScreens"/> moves ahead of the slide.
    /// </summary>
    public sealed class DetailPage : Screen
    {
        public DetailPage(int number)
        {
            BackgroundBrush = new Color(245, 245, 245);
            var panel = new StackPanel { Margin = new Thickness(16) };
            var back = new TextButton("Back") { Height = 44, Width = 96, HorizontalAlignment = HorizontalAlignment.Left };
            back.Click += (_, _) => ScreenEngine?.NavigateBack();
            panel.AddChild(back);
            panel.AddChild(new TextBlock
            {
                Text = "Page " + number,
                TextSize = 34 + number % 10 * 1.3f,
                TextColor = new Color(20, 30, 40),
                Margin = new Thickness(0, 16, 0, 4)
            });
            panel.AddChild(new TextBlock
            {
                Text = "Opened for the first time: its text sizes, shadows and corners are new.",
                TextSize = 15 + number % 10 * 0.4f,
                TextColor = new Color(105, 115, 125),
                TextWrapping = TextWrapping.Wrap
            });
            for (var i = 0; i < 3; i++)
            {
                var card = new Border
                {
                    Height = 120,
                    Margin = new Thickness(0, 16, 0, 0),
                    CornerRadius = new CornerRadius(8 + number % 10 + i * 2),
                    BackgroundBrush = new LinearGradientBrush(new Color(20 + i * 40, 126, 133), new Color(10, 60 + i * 30, 90)),
                    Shadow = new ShadowStyle { Blur = 6 + number % 10 * 1.5f + i * 2, Offset = new Vector2(0, 3 + i) }
                };
                card.Content = new TextBlock
                {
                    Text = "Card " + (i + 1),
                    TextSize = 20 + number % 10 * 0.7f + i * 1.9f,
                    TextColor = Color.White,
                    Margin = new Thickness(16)
                };
                panel.AddChild(card);
            }
            var chart = new DrawingCanvas { Height = 160, Margin = new Thickness(0, 16, 0, 0) };
            chart.Render += (canvas, context) =>
            {
                // Canvas coordinates start at its top-left corner.
                var width = canvas.ClippingRect.Width;
                for (var i = 0; i < 10; i++)
                    context.FillCircle(new Vector2(16 + i * (width - 32) / 9, 80 + MathF.Sin(i + number) * 50), 6, new Color(20, 126, 133));
                context.DrawRectangle(new Rect(0, 0, width, canvas.ClippingRect.Height), new Color(185, 190, 195), 1);
            };
            panel.AddChild(chart);
            Content = new SafeAreaPanel(new ScrollViewer { Content = panel });
            BackRequested += (_, args) =>
            {
                args.Handled = true;
                ScreenEngine?.NavigateBack();
            };
        }
    }
}
