using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Cyberpunk Neon: near-black surfaces, cyan/yellow neon frames, glow and grain.</summary>
public static class CyberpunkTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("cyberpunk", "Cyberpunk Neon", "orbitron", ThemeEra.Modern, ThemeBrightness.Dark,
            background: "#0A0A12", surface: "#161622", surfaceAlt: "#23142F", text: "#FCEE0A",
            primary: "#00F0FF", secondary: "#FF003C", selection: "#FCEE0A", selectionText: "#000000",
            styleTheme: theme =>
            {
                ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(theme.Palette.Primary), 1, 0);
                theme.ButtonShadow = new ShadowStyle { Color = new Color(0, 240, 255, 90), Offset = Vector2.Zero, Blur = 10, Spread = 1 };
                theme.PanelShadow = new ShadowStyle { Color = new Color(255, 0, 60, 70), Offset = Vector2.Zero, Blur = 12 };
                theme.PostEffects = new PostEffect[]
                {
                    new ScanlinePostEffect { Strength = 0.06f },
                    new BloomPostEffect { Strength = 0.34f },
                    new FilmGrainPostEffect { Strength = 0.025f }
                };
            });
    }
}