using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>BeOS R5: light gray desktop with the signature yellow tab accent and soft bevels.</summary>
public static class BeOsTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("beos", "BeOS R5", "selawik", ThemeEra.Desktop, ThemeBrightness.Light,
            background: "#D8D8D8", surface: "#D8D8D8", surfaceAlt: "#FFCB00", text: "#000000",
            primary: "#336698", secondary: "#FFCB00", selection: "#FFCB00", selectionText: "#000000",
            styleTheme: theme =>
            {
                var raised = new BevelBrush(theme.Palette.Surface, Color.White, ThemeBuilder.Hex("#9A9A9A"));
                ThemeBuilder.Chrome(theme.Button, raised, null, 0, 0);
                theme.Button.Pressed.Background = raised.AsSunken();
                theme.ButtonBackgroundBrush = raised;
            });
    }
}