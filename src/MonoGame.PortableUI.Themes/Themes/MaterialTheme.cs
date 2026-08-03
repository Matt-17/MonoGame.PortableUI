using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Material Design 3: tonal purple palette with elevation shadows.</summary>
public static class MaterialTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("material", "Material Design 3", "roboto", ThemeEra.Modern, ThemeBrightness.Light,
            background: "#FFFBFE", surface: "#FFFBFE", surfaceAlt: "#E7E0EC", text: "#1C1B1F",
            primary: "#6750A4", secondary: "#B3261E", selection: "#6750A4", selectionText: "#FFFFFF",
            styleTheme: theme =>
            {
                ThemeBuilder.Chrome(theme.Button, null, null, 0, 4);
                theme.ButtonShadow = ShadowStyle.Level1();
                theme.PanelShadow = ShadowStyle.Level2();
            });
    }
}