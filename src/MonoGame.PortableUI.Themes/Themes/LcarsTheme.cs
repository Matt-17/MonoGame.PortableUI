namespace MonoGame.PortableUI.Themes;

/// <summary>Sci-Fi HUD / LCARS: black space, orange/lilac pill-shaped controls.</summary>
public static class LcarsTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("lcars", "Sci-Fi HUD / LCARS", "orbitron", ThemeEra.Modern, ThemeBrightness.Dark,
            background: "#000000", surface: "#111111", surfaceAlt: "#222222", text: "#FFCC99",
            primary: "#FF9C00", secondary: "#CC99CC", selection: "#9999FF", selectionText: "#000000",
            styleTheme: theme => ThemeBuilder.Chrome(theme.Button, null, null, 0, 19));
    }
}