using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Neumorphism: soft extruded surfaces on a single pale background, blue accent.</summary>
public static class NeumorphicTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("neumorphic", "Neumorphism", "atkinsonhyperlegible", ThemeEra.Modern, ThemeBrightness.Light,
            background: "#E0E5EC", surface: "#E0E5EC", surfaceAlt: "#D3DAE4", text: "#4A5568",
            primary: "#5B7FFF", secondary: "#A3B1C6", selection: "#5B7FFF", selectionText: "#FFFFFF",
            styleTheme: theme =>
            {
                ThemeBuilder.Chrome(theme.Button, null, null, 0, 14);
                theme.ButtonShadow = new ShadowStyle { Color = new Color(163, 177, 198, 150), Offset = new Vector2(6, 6), Blur = 12 };
                theme.PanelShadow = new ShadowStyle { Color = new Color(163, 177, 198, 130), Offset = new Vector2(8, 8), Blur = 16 };
            });
    }
}