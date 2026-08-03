namespace MonoGame.PortableUI.Themes;

/// <summary>RPG Parchment: aged paper tones with gold and deep-red accents.</summary>
public static class ParchmentTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("parchment", "RPG Parchment", "atkinsonhyperlegible", ThemeEra.Modern, ThemeBrightness.Light,
            background: "#E8D8B0", surface: "#F2E3BD", surfaceAlt: "#D6BC7F", text: "#3B2A18",
            primary: "#C9A227", secondary: "#8B0000", selection: "#8B0000", selectionText: "#FFFFFF",
            styleTheme: theme => ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(theme.Palette.FieldBorder), 1, 4));
    }
}