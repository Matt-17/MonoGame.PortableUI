using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>DOS / Turbo Vision: navy desktop, gray dialog buttons with hard shadows, cyan accents.</summary>
public static class DosTheme
{
    public static ThemeDefinition Create()
    {
        var navy = new Color(0, 0, 84);
        var blue = new Color(0, 0, 170);
        var dialog = new Color(170, 170, 170);
        var cyan = new Color(85, 255, 255);
        var yellow = new Color(255, 255, 85);
        var black = new Color(0, 0, 0);
        var palette = new ThemePalette
        {
            Background = navy,
            Surface = blue,
            SurfaceAlt = dialog,
            Text = new Color(192, 192, 192),
            HeadingText = yellow,
            MutedText = cyan,
            Primary = new Color(0, 170, 170),
            Secondary = yellow,
            Warning = yellow,
            Danger = new Color(170, 0, 0),
            Info = Color.White,
            Selection = dialog,
            SelectionText = black,
            TabText = Color.White,
            SelectedTabText = black,
            FieldFrame = dialog,
            FieldBorder = cyan,
            DisabledSurface = new Color(85, 85, 85),
            DisabledText = new Color(120, 120, 120)
        };

        return ThemeBuilder.CreateDefinition("dos", "DOS / Turbo Vision", "px437ibmvga8x16", ThemeEra.Terminal, ThemeBrightness.Dark, palette, navy,
            styleTheme: theme => ApplyTurboVisionChrome(theme, palette, buttonFace: dialog));
    }

    /// <summary>Turbo Vision / Norton Commander chrome: light dialog buttons with black text and hard shadows; blue input fields.</summary>
    internal static void ApplyTurboVisionChrome(PortableTheme theme, ThemePalette palette, Color buttonFace)
    {
        ThemeBuilder.Chrome(theme.Button, ThemeBuilder.Solid(buttonFace), ThemeBuilder.Solid(Color.Black), 1, 0);
        theme.Button.Normal.TextColor = Color.Black;
        theme.ButtonBackgroundBrush = ThemeBuilder.Solid(buttonFace);
        theme.ButtonTextColor = Color.Black;
        theme.ButtonHoverBrush = ThemeBuilder.Solid(new Color(0, 170, 170, 90));
        theme.ButtonPressedBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 60));
        theme.ButtonShadow = new ShadowStyle { Color = new Color(0, 0, 0, 190), Offset = new Vector2(2, 2), Blur = 0 };
        ThemeBuilder.Chrome(theme.ListBox, null, ThemeBuilder.Solid(palette.FieldBorder), 1, 0);
        // Turbo Vision input fields: blue face with light text (not gray-on-gray).
        ThemeBuilder.Chrome(theme.TextBox, ThemeBuilder.Solid(palette.Surface), ThemeBuilder.Solid(palette.FieldBorder), 1, 0);
        theme.TextBoxBackgroundBrush = ThemeBuilder.Solid(palette.Surface);
        theme.TextBoxTextColor = palette.HeadingText;
        theme.TextBoxHintTextColor = palette.Text;
        // ComboBox: blue face with yellow text like classic TV pick lists.
        ThemeBuilder.Chrome(theme.ComboBox, ThemeBuilder.Solid(palette.Surface), ThemeBuilder.Solid(palette.FieldBorder), 1, 0);
        theme.ComboBox.Normal.TextColor = palette.Text;
        theme.ComboBox.Pressed.TextColor = palette.Text;
        theme.ComboBox.InvalidateResolvedCache();
        theme.ComboBoxGlyphColor = palette.Text;
    }
}