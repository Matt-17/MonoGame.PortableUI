namespace MonoGame.PortableUI.Themes;

/// <summary>Solarized Dark: deep teal background with the classic Solarized accent set.</summary>
public static class SolarizedDarkTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("solarized-dark", "Solarized Dark", "ibmplexmono", ThemeEra.Modern, ThemeBrightness.Dark,
            background: "#002B36", surface: "#073642", surfaceAlt: "#094352", text: "#839496",
            primary: "#268BD2", secondary: "#2AA198", selection: "#268BD2", selectionText: "#FDF6E3",
            styleTheme: theme => ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(theme.Palette.FieldBorder), 1, 4));
    }
}
