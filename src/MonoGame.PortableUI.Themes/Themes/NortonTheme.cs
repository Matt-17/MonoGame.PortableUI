using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Norton Blue TUI: Norton Commander blue with cyan dialog buttons and hard shadows.</summary>
public static class NortonTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("norton", "Norton Blue TUI", "px437ibmvga8x16", ThemeEra.Terminal, ThemeBrightness.Dark,
            background: "#0000A8", surface: "#0000A8", surfaceAlt: "#000000", text: "#FFFFFF",
            primary: "#00A8A8", secondary: "#FCFC54", selection: "#00A8A8", selectionText: "#000000",
            styleTheme: theme =>
            {
                var palette = theme.Palette;
                // Turbo Vision / Norton Commander chrome: cyan dialog buttons with black text and hard shadows.
                var buttonFace = ThemeBuilder.Hex("#00A8A8");
                ThemeBuilder.Chrome(theme.Button, ThemeBuilder.Solid(buttonFace), ThemeBuilder.Solid(Color.Black), 1, 0);
                theme.Button.Normal.TextColor = Color.Black;
                theme.ButtonBackgroundBrush = ThemeBuilder.Solid(buttonFace);
                theme.ButtonTextColor = Color.Black;
                theme.ButtonHoverBrush = ThemeBuilder.Solid(new Color(0, 170, 170, 90));
                theme.ButtonPressedBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 60));
                theme.ButtonShadow = new ShadowStyle { Color = new Color(0, 0, 0, 190), Offset = new Vector2(2, 2), Blur = 0 };
                ThemeBuilder.Chrome(theme.ListBox, null, ThemeBuilder.Solid(palette.FieldBorder), 1, 0);
                // Input fields: blue face with light text (not gray-on-gray).
                ThemeBuilder.Chrome(theme.TextBox, ThemeBuilder.Solid(palette.Surface), ThemeBuilder.Solid(palette.FieldBorder), 1, 0);
                theme.TextBoxBackgroundBrush = ThemeBuilder.Solid(palette.Surface);
                theme.TextBoxTextColor = palette.HeadingText;
                theme.TextBoxHintTextColor = palette.Text;
                ThemeBuilder.Chrome(theme.ComboBox, ThemeBuilder.Solid(palette.Surface), ThemeBuilder.Solid(palette.FieldBorder), 1, 0);
                theme.ComboBox.Normal.TextColor = palette.Text;
                theme.ComboBox.Pressed.TextColor = palette.Text;
                theme.ComboBox.InvalidateResolvedCache();
                theme.ComboBoxGlyphColor = palette.Text;
            });
    }
}