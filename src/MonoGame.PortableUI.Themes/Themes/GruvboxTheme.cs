namespace MonoGame.PortableUI.Themes;

/// <summary>Gruvbox: retro warm dark palette with orange/green accents.</summary>
public static class GruvboxTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("gruvbox", "Gruvbox", "ibmplexmono", ThemeEra.Modern, ThemeBrightness.Dark,
            background: "#282828", surface: "#3C3836", surfaceAlt: "#504945", text: "#EBDBB2",
            primary: "#D65D0E", secondary: "#689D6A", selection: "#D79921", selectionText: "#282828",
            styleTheme: theme => ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(theme.Palette.FieldBorder), 1, 6));
    }
}
