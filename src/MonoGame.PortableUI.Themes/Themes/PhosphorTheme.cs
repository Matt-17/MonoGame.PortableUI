namespace MonoGame.PortableUI.Themes;

/// <summary>Green Phosphor CRT: monochrome green terminal with scanlines, barrel distortion and bloom.</summary>
public static class PhosphorTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("phosphor", "Green Phosphor CRT", "vt323", ThemeEra.Terminal, ThemeBrightness.Dark,
            background: "#001100", surface: "#001B00", surfaceAlt: "#003300", text: "#33FF33",
            primary: "#33FF33", secondary: "#1A801A", selection: "#33FF33", selectionText: "#001100",
            styleTheme: theme =>
            {
                ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(theme.Palette.Primary), 1, 0);
                theme.PostEffects = new PostEffect[]
                {
                    new ScanlinePostEffect { Strength = 0.12f },
                    new CrtBarrelPostEffect { Distortion = 0.06f },
                    new BloomPostEffect { Strength = 0.18f }
                };
            });
    }
}