namespace MonoGame.PortableUI.Themes;

/// <summary>Metro / Windows 8: flat dark tiles, hard edges, cyan accent.</summary>
public static class MetroTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("metro", "Metro / Windows 8", "selawik", ThemeEra.Modern, ThemeBrightness.Dark,
            background: "#1D1D1D", surface: "#252525", surfaceAlt: "#333333", text: "#FFFFFF",
            primary: "#1BA1E2", secondary: "#E51400", selection: "#1BA1E2", selectionText: "#FFFFFF",
            styleTheme: theme => ThemeBuilder.Chrome(theme.Button, null, null, 0, 0));
    }
}