using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Liquid Glass: refractive liquid-glass surfaces with squircle panels.</summary>
public static class LiquidGlassTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("liquid", "Liquid Glass", "atkinsonhyperlegible", ThemeEra.Glass, ThemeBrightness.Dark,
            background: "#111827", surface: "#FFFFFF", surfaceAlt: "#C7D2FE", text: "#FFFFFF",
            primary: "#7DD3FC", secondary: "#F0ABFC", selection: "#FFFFFF", selectionText: "#111827",
            glass: true, liquid: true,
            styleTheme: theme =>
            {
                ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(new Color(255, 255, 255, 96)), 1, 18);
                theme.PanelShadow = new ShadowStyle { Color = new Color(0, 0, 0, 110), Offset = new Vector2(0, 10), Blur = 18 };
            });
    }
}