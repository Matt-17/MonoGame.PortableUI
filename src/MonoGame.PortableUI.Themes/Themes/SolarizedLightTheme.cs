namespace MonoGame.PortableUI.Themes;

/// <summary>Solarized Light: warm cream background with the classic Solarized accent set.</summary>
public static class SolarizedLightTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("solarized-light", "Solarized Light", "ibmplexmono", ThemeEra.Modern, ThemeBrightness.Light,
            background: "#FDF6E3", surface: "#EEE8D5", surfaceAlt: "#E7DFC5", text: "#657B83",
            primary: "#268BD2", secondary: "#2AA198", selection: "#268BD2", selectionText: "#FDF6E3",
            styleTheme: theme => ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(theme.Palette.FieldBorder), 1, 4));
    }
}