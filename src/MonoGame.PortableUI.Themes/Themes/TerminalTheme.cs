using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Themes;

/// <summary>Terminal Glass: graphite panels with bright mint/cyan frames.</summary>
public static class TerminalTheme
{
    public static ThemeDefinition Create()
    {
        var graphite = new Color(11, 16, 20);
        var green = new Color(42, 246, 183);
        var cyan = new Color(83, 232, 255);
        var text = new Color(217, 255, 244);
        var palette = new ThemePalette
        {
            Background = graphite,
            Surface = new Color(21, 30, 34),
            SurfaceAlt = new Color(39, 52, 58),
            Text = text,
            HeadingText = green,
            MutedText = new Color(138, 184, 174),
            Primary = green,
            Secondary = cyan,
            Warning = new Color(246, 206, 87),
            Danger = new Color(255, 96, 116),
            Info = cyan,
            Selection = green,
            SelectionText = graphite,
            TabText = text,
            SelectedTabText = graphite,
            FieldFrame = new Color(39, 52, 58),
            FieldBorder = green,
            DisabledSurface = new Color(46, 57, 62),
            DisabledText = new Color(105, 126, 125)
        };

        return ThemeBuilder.CreateDefinition("terminal", "Terminal Glass", "ibmplexmono", ThemeEra.Terminal, ThemeBrightness.Dark, palette, graphite,
            styleTheme: theme => ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(green), 1, 0));
    }
}