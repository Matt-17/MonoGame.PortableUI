using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Green Phosphor CRT: monochrome green terminal with scanlines and bloom (screen curvature belongs to the display: ScreenEngineOptions.PostEffects).</summary>
public static class PhosphorTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("phosphor", "Green Phosphor CRT", "vt323", ThemeEra.Terminal, ThemeBrightness.Dark,
            background: "#001100", surface: "#001B00", surfaceAlt: "#003300", text: "#33FF33",
            primary: "#33FF33", secondary: "#1A801A", selection: "#33FF33", selectionText: "#001100",
            styleTheme: theme =>
            {
                theme.Cursor = CursorStyle.Arrow(ThemeBuilder.Hex("#33FF66"), ThemeBuilder.Hex("#001A08"));
                ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(theme.Palette.Primary), 1, 0);
                theme.PostEffects = new PostEffect[]
                {
                    new ScanlinePostEffect { Strength = 0.12f },
                    new BloomPostEffect { Strength = 0.18f }
                };
            });
    }
}