using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Brutalist: stark white, thick black borders, hard offset shadows, no motion.</summary>
public static class BrutalistTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("brutalist", "Brutalist", "atkinsonhyperlegible", ThemeEra.Modern, ThemeBrightness.Light,
            background: "#FFFFFF", surface: "#FFFFFF", surfaceAlt: "#F0F0F0", text: "#000000",
            primary: "#FF3300", secondary: "#000000", selection: "#000000", selectionText: "#FFFFFF",
            styleTheme: theme =>
            {
                ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(Color.Black), 3, 0);
                theme.ButtonShadow = new ShadowStyle { Color = Color.Black, Offset = new Vector2(6, 6), Blur = 0 };
                theme.PanelShadow = new ShadowStyle { Color = Color.Black, Offset = new Vector2(8, 8), Blur = 0 };
            },
            reducedMotion: true);
    }
}