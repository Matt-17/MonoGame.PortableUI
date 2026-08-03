using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Mac System 1-bit: pure black on white, rounded outline buttons, dithered disabled state.</summary>
public static class Mac1BitTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("mac1bit", "Mac System 1-bit", "atkinsonhyperlegible", ThemeEra.Desktop, ThemeBrightness.Light,
            background: "#FFFFFF", surface: "#FFFFFF", surfaceAlt: "#CCCCCC", text: "#000000",
            primary: "#000000", secondary: "#808080", selection: "#000000", selectionText: "#FFFFFF",
            styleTheme: theme =>
            {
                ThemeBuilder.Chrome(theme.Button, ThemeBuilder.Solid(Color.White), ThemeBuilder.Solid(Color.Black), 1, 5);
                theme.Button.Pressed.Background = ThemeBuilder.Solid(Color.Black);
                theme.ButtonPressedTextColor = Color.White;
                theme.DisabledOverlayBrush = PatternBrush.Dither(new Color(255, 255, 255, 180), new Color(255, 255, 255, 0));
            },
            reducedMotion: true);
    }
}
