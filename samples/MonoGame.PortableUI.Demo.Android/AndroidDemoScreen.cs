using Microsoft.Xna.Framework;
using MonoGame.PortableUI;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Demo.Android
{
    /// <summary>
    /// A single scrollable screen exercising the core touch-driven controls on Android:
    /// a tap counter button, a text box (soft keyboard), and a selectable list.
    /// </summary>
    public sealed class AndroidDemoScreen : Screen
    {
        private static readonly Color Heading = new Color(20, 30, 40);
        private static readonly Color Muted = new Color(105, 115, 125);
        private static readonly Color Surface = new Color(245, 245, 245);
        private static readonly Color Accent = new Color(20, 126, 133);

        private int _tapCount;

        public AndroidDemoScreen()
        {
            BackgroundBrush = Color.White;
            Content = BuildContent();
        }

        private Control BuildContent()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = 24,
                BackgroundBrush = Surface
            };

            panel.AddChild(new TextBlock
            {
                Text = "MonoGame.PortableUI",
                TextColor = Heading,
                TextSize = 24,
                Margin = new Thickness(12, 16, 12, 2)
            });
            panel.AddChild(new TextBlock
            {
                Text = "Running on Android",
                TextColor = Muted,
                TextSize = 14,
                Margin = new Thickness(12, 0, 12, 16)
            });

            var tapLabel = new TextBlock
            {
                Text = "Taps: 0",
                TextColor = Heading,
                TextSize = 16,
                Margin = new Thickness(12, 4, 12, 4)
            };

            var tapButton = new TextButton("Tap me")
            {
                BackgroundBrush = Accent,
                TextColor = Color.White,
                Height = 56,
                Margin = new Thickness(12, 4, 12, 16)
            };
            tapButton.Click += (_, _) =>
            {
                _tapCount++;
                tapLabel.Text = $"Taps: {_tapCount}";
            };
            panel.AddChild(tapLabel);
            panel.AddChild(tapButton);

            panel.AddChild(new TextBlock
            {
                Text = "Type (soft keyboard):",
                TextColor = Muted,
                TextSize = 14,
                Margin = new Thickness(12, 4, 12, 4)
            });
            panel.AddChild(new TextBox
            {
                HintText = "Enter some text…",
                Margin = new Thickness(12, 0, 12, 16)
            });

            panel.AddChild(new TextBlock
            {
                Text = "Pick a fruit (scroll + tap):",
                TextColor = Muted,
                TextSize = 14,
                Margin = new Thickness(12, 4, 12, 4)
            });

            var list = new ListBox
            {
                Height = 260,
                Margin = new Thickness(12, 0, 12, 16)
            };
            foreach (var item in new[]
            {
                "Apple", "Banana", "Cherry", "Date", "Elderberry",
                "Fig", "Grape", "Honeydew", "Kiwi", "Lemon", "Mango", "Nectarine"
            })
            {
                list.Items.Add(item);
            }
            panel.AddChild(list);

            return new ScrollViewer
            {
                ScrollOrientation = Orientation.Vertical,
                Content = panel
            };
        }
    }
}
