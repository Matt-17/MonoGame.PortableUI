using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Mac OS 9 Platinum: brushed gray gradients with lavender highlights.</summary>
public static class MacOs9Theme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("macos9", "Mac OS 9 Platinum", "atkinsonhyperlegible", ThemeEra.Desktop, ThemeBrightness.Light,
            background: "#CCCCCC", surface: "#DDDDDD", surfaceAlt: "#BBBBBB", text: "#000000",
            primary: "#6666CC", secondary: "#9999CC", selection: "#6666CC", selectionText: "#FFFFFF",
            styleTheme: theme =>
            {
                var face = new LinearGradientBrush(new GradientStop(0, ThemeBuilder.Hex("#F8F8F8")), new GradientStop(1, ThemeBuilder.Hex("#D0D0D0")));
                ThemeBuilder.Chrome(theme.Button, face, ThemeBuilder.Solid(ThemeBuilder.Hex("#888888")), 1, 6);
                theme.ButtonShadow = new ShadowStyle { Color = new Color(0, 0, 0, 70), Offset = new Vector2(0, 2), Blur = 4 };
            });
    }
}