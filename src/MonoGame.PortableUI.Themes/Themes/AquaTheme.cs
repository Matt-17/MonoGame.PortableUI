using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>macOS Aqua: blue gel buttons and the signature pinstriped chrome surfaces.</summary>
public static class AquaTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("aqua", "macOS Aqua", "atkinsonhyperlegible", ThemeEra.Desktop, ThemeBrightness.Light,
            background: "#F0F0F0", surface: "#FFFFFF", surfaceAlt: "#E8E8E8", text: "#1F2933",
            primary: "#3B88FD", secondary: "#88BFFC", selection: "#3B88FD", selectionText: "#FFFFFF",
            styleTheme: theme =>
            {
                var gel = new LinearGradientBrush(
                    new GradientStop(0, ThemeBuilder.Hex("#FDFFFF")),
                    new GradientStop(0.45f, ThemeBuilder.Hex("#CBE3F5")),
                    new GradientStop(0.5f, ThemeBuilder.Hex("#9CC5EB")),
                    new GradientStop(1, ThemeBuilder.Hex("#C7E0F5")));
                ThemeBuilder.Chrome(theme.Button, gel, ThemeBuilder.Solid(ThemeBuilder.Hex("#7A96B8")), 1, 12);
                theme.Button.Normal.TextColor = Color.Black;
                theme.ButtonTextColor = Color.Black;
                // Signature Aqua pinstripes on chrome surfaces.
                var pinstripes = PatternBrush.Pinstripes(ThemeBuilder.Hex("#F4F4F4"), ThemeBuilder.Hex("#E8E8E8"), 4);
                theme.TabHeaderBackgroundBrush = pinstripes;
                theme.Panel.Normal.Background = pinstripes;
                ThemeBuilder.Chrome(theme.TextBox, ThemeBuilder.Solid(Color.White), ThemeBuilder.Solid(ThemeBuilder.Hex("#9AB0C8")), 1, 4);
                ThemeBuilder.Chrome(theme.ListBox, ThemeBuilder.Solid(Color.White), ThemeBuilder.Solid(ThemeBuilder.Hex("#9AB0C8")), 1, 4);
                theme.ButtonShadow = new ShadowStyle { Color = new Color(0, 0, 0, 70), Offset = new Vector2(0, 2), Blur = 4 };
            });
    }
}