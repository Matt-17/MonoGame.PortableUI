namespace MonoGame.PortableUI.Themes;

/// <summary>E-Ink Paper: paper-white, near-black ink, no motion.</summary>
public static class EInkTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("eink", "E-Ink Paper", "atkinsonhyperlegible", ThemeEra.Modern, ThemeBrightness.Light,
            background: "#F5F2EA", surface: "#FFFFFF", surfaceAlt: "#DDD9D0", text: "#333333",
            primary: "#333333", secondary: "#8A8680", selection: "#333333", selectionText: "#FFFFFF",
            styleTheme: theme => ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(theme.Palette.Text), 1, 3),
            reducedMotion: true);
    }
}