using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Soft Studio: warm paper tones, rounded buttons and soft paper-colored shadows.</summary>
public static class StudioTheme
{
    public static ThemeDefinition Create()
    {
        var offWhite = new Color(247, 244, 236);
        var paper = new Color(255, 252, 246);
        var charcoal = new Color(37, 37, 37);
        var blue = new Color(59, 130, 246);
        var coral = new Color(255, 107, 94);
        var palette = new ThemePalette
        {
            Background = offWhite,
            Surface = paper,
            SurfaceAlt = new Color(233, 228, 218),
            Text = charcoal,
            HeadingText = charcoal,
            MutedText = new Color(109, 106, 101),
            Primary = blue,
            Secondary = coral,
            Warning = new Color(210, 144, 39),
            Danger = new Color(194, 64, 76),
            Info = new Color(46, 118, 178),
            Selection = charcoal,
            SelectionText = offWhite,
            TabText = charcoal,
            SelectedTabText = offWhite,
            FieldFrame = new Color(233, 228, 218),
            FieldBorder = blue,
            DisabledSurface = new Color(218, 213, 203),
            DisabledText = new Color(139, 134, 126)
        };

        return ThemeBuilder.CreateDefinition("studio", "Soft Studio", "atkinsonhyperlegible", ThemeEra.Modern, ThemeBrightness.Light, palette, offWhite,
            styleTheme: theme =>
            {
                theme.ButtonShadow = new ShadowStyle { Color = new Color(217, 212, 203, 170), Offset = new Vector2(4, 5), Blur = 10 };
                theme.PanelShadow = new ShadowStyle { Color = new Color(217, 212, 203, 190), Offset = new Vector2(6, 8), Blur = 14 };
                ThemeBuilder.Chrome(theme.Button, null, null, 0, 8);
            });
    }
}
