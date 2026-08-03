using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Dracula: dark purple editor palette with pink/purple accents.</summary>
public static class DraculaTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("dracula", "Dracula", "ibmplexmono", ThemeEra.Modern, ThemeBrightness.Dark,
            background: "#282A36", surface: "#343746", surfaceAlt: "#44475A", text: "#F8F8F2",
            primary: "#BD93F9", secondary: "#FF79C6", selection: "#44475A", selectionText: "#F8F8F2",
            styleTheme: theme =>
            {
                ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(theme.Palette.FieldBorder), 1, 6);
                theme.ButtonShadow = ShadowStyle.Level1();
            });
    }
}