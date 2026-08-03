using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Fluent Acrylic: dark acrylic surfaces, subtle borders, Windows accent blue.</summary>
public static class FluentTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("fluent", "Fluent Acrylic", "selawik", ThemeEra.Glass, ThemeBrightness.Dark,
            background: "#202020", surface: "#2C2C2C", surfaceAlt: "#3A3A3A", text: "#FFFFFF",
            primary: "#0078D4", secondary: "#60CDFF", selection: "#0078D4", selectionText: "#FFFFFF",
            glass: true,
            styleTheme: theme =>
            {
                ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(ThemeBuilder.Hex("#454545")), 1, 4);
                theme.PanelShadow = new ShadowStyle { Color = new Color(0, 0, 0, 110), Offset = new Vector2(0, 10), Blur = 18 };
            });
    }
}