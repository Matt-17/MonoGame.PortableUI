using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Nord: arctic blue-gray palette with frost accents.</summary>
public static class NordTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("nord", "Nord", "atkinsonhyperlegible", ThemeEra.Modern, ThemeBrightness.Dark,
            background: "#2E3440", surface: "#3B4252", surfaceAlt: "#434C5E", text: "#D8DEE9",
            primary: "#88C0D0", secondary: "#BF616A", selection: "#88C0D0", selectionText: "#2E3440",
            styleTheme: theme =>
            {
                ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(theme.Palette.FieldBorder), 1, 6);
                theme.ButtonShadow = ShadowStyle.Level1();
            });
    }
}