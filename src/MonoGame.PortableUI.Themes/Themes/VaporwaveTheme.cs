using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Vaporwave Sunset: purple dusk, pink/blue neon accents and glow shadows.</summary>
public static class VaporwaveTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("vaporwave", "Vaporwave Sunset", "orbitron", ThemeEra.Modern, ThemeBrightness.Dark,
            background: "#1A1030", surface: "#2C164D", surfaceAlt: "#3E1E6C", text: "#FFFFFF",
            primary: "#FF71CE", secondary: "#01CDFE", selection: "#05FFA1", selectionText: "#1A1030",
            styleTheme: theme =>
            {
                ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(theme.Palette.Primary), 1, 8);
                theme.ButtonShadow = new ShadowStyle { Color = new Color(255, 113, 206, 110), Offset = new Vector2(0, 4), Blur = 12 };
                theme.PanelShadow = new ShadowStyle { Color = new Color(1, 205, 254, 90), Offset = new Vector2(0, 6), Blur = 16 };
            });
    }
}