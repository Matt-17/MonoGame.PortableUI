using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Amber Terminal: warm monochrome amber CRT with scanlines and bloom (screen curvature belongs to the display: ScreenEngineOptions.PostEffects).</summary>
public static class AmberTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("amber", "Amber Terminal", "vt323", ThemeEra.Terminal, ThemeBrightness.Dark,
            background: "#1A0F00", surface: "#221400", surfaceAlt: "#332000", text: "#FFB000",
            primary: "#FFB000", secondary: "#805800", selection: "#FFB000", selectionText: "#1A0F00",
            styleTheme: theme =>
            {
                theme.Cursor = CursorStyle.Arrow(ThemeBuilder.Hex("#FFB000"), ThemeBuilder.Hex("#1A0F00"));
                ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(theme.Palette.Primary), 1, 0);
                theme.PostEffects = new PostEffect[]
                {
                    new ScanlinePostEffect { Strength = 0.12f },
                    new BloomPostEffect { Strength = 0.18f }
                };
            });
    }
}