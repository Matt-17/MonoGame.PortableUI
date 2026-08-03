using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Themes;

/// <summary>Amiga Workbench: gray bevels, Workbench blue and the signature orange accent.</summary>
public static class AmigaTheme
{
    public static ThemeDefinition Create()
    {
        var gray = new Color(170, 170, 170);
        var panel = new Color(195, 195, 195);
        var blue = new Color(0, 86, 170);
        var ink = new Color(23, 23, 28);
        var palette = new ThemePalette
        {
            Background = gray,
            Surface = panel,
            SurfaceAlt = new Color(146, 146, 154),
            Text = ink,
            HeadingText = ink,
            MutedText = new Color(0, 45, 112),
            Primary = blue,
            Secondary = new Color(255, 136, 0),
            Warning = new Color(255, 136, 0),
            Danger = new Color(196, 48, 48),
            Info = new Color(39, 107, 172),
            Selection = blue,
            SelectionText = Color.White,
            TabText = ink,
            SelectedTabText = Color.White,
            FieldFrame = new Color(146, 146, 154),
            FieldBorder = blue,
            DisabledSurface = new Color(132, 132, 140),
            DisabledText = new Color(86, 86, 92)
        };

        return ThemeBuilder.CreateDefinition("amiga", "Amiga Workbench", "jersey10", ThemeEra.Desktop, ThemeBrightness.Light, palette, gray,
            styleTheme: theme => ThemeBuilder.Bevel(theme, panel, Color.White, panel, panel, Color.Black));
    }
}