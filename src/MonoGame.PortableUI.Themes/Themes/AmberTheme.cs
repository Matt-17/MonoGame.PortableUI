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
                // Monochrome monitor: inverse video is a block in the phosphor colour.
                theme.Cursor = CursorStyle.TextBlock(ThemeBuilder.Hex("#FFB000") * 0.85f);
                ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(theme.Palette.Primary), 1, 0);
                theme.PostEffects = new PostEffect[]
                {
                    new ScanlinePostEffect { Strength = 0.12f },
                    new BloomPostEffect { Strength = 0.18f }
                };
            });
    }
}