using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Game Boy DMG: the four-shade green LCD look with hard ink shadows.</summary>
public static class GameBoyTheme
{
    public static ThemeDefinition Create()
    {
        var light = new Color(155, 188, 15);
        var glass = new Color(202, 220, 159);
        var mid = new Color(139, 172, 15);
        var dark = new Color(48, 98, 48);
        var ink = new Color(15, 56, 15);
        var palette = new ThemePalette
        {
            Background = light,
            Surface = mid,
            SurfaceAlt = dark,
            Text = ink,
            HeadingText = ink,
            MutedText = dark,
            Primary = ink,
            Secondary = new Color(88, 111, 26),
            Warning = new Color(102, 84, 15),
            Danger = new Color(112, 45, 40),
            Info = new Color(32, 78, 54),
            Selection = ink,
            SelectionText = glass,
            TabText = ink,
            SelectedTabText = glass,
            FieldFrame = dark,
            FieldBorder = ink,
            DisabledSurface = new Color(113, 143, 34),
            DisabledText = new Color(64, 91, 38)
        };

        return ThemeBuilder.CreateDefinition("gameboy", "Game Boy DMG", "silkscreen", ThemeEra.Retro, ThemeBrightness.Light, palette, light,
            styleTheme: theme =>
            {
                ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(ink), 1, 0);
                theme.ButtonShadow = new ShadowStyle { Color = new Color(15, 56, 15, 200), Offset = new Vector2(2, 2), Blur = 0 };
            },
            reducedMotion: true);
    }
}