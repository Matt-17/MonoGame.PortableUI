using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Windows XP Luna: glossy button faces, dark blue frames and the orange hover ring.</summary>
public static class LunaTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("luna", "Windows XP Luna", "selawik", ThemeEra.Desktop, ThemeBrightness.Light,
            background: "#ECE9D8", surface: "#F4F3EE", surfaceAlt: "#DDE8FF", text: "#000000",
            primary: "#3C81F3", secondary: "#F9B233", selection: "#3C81F3", selectionText: "#FFFFFF",
            styleTheme: theme =>
            {
                // Windows XP: glossy face, dark blue frame, radius 3, orange hover ring.
                var face = new LinearGradientBrush(new GradientStop(0, ThemeBuilder.Hex("#FFFFFF")), new GradientStop(1, ThemeBuilder.Hex("#ECE9D8")));
                var pressed = new LinearGradientBrush(new GradientStop(0, ThemeBuilder.Hex("#D8D4C8")), new GradientStop(1, ThemeBuilder.Hex("#E8E4D8")));
                ThemeBuilder.Chrome(theme.Button, face, ThemeBuilder.Solid(ThemeBuilder.Hex("#003C74")), 1, 3);
                theme.Button.Normal.TextColor = Color.Black;
                theme.Button.Hover.BorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#F9B233"));
                theme.Button.Hover.BorderThickness = new MonoGame.PortableUI.Common.Thickness(2);
                theme.Button.Pressed.Background = pressed;
                theme.ButtonTextColor = Color.Black;
                theme.ButtonHoverBrush = ThemeBuilder.Solid(new Color(249, 178, 51, 34));
                theme.ButtonPressedBrush = ThemeBuilder.Solid(new Color(0, 60, 116, 40));
                ThemeBuilder.Chrome(theme.TextBox, ThemeBuilder.Solid(Color.White), ThemeBuilder.Solid(ThemeBuilder.Hex("#7F9DB9")), 1, 0);
                ThemeBuilder.Chrome(theme.ListBox, ThemeBuilder.Solid(Color.White), ThemeBuilder.Solid(ThemeBuilder.Hex("#7F9DB9")), 1, 0);
                ThemeBuilder.Chrome(theme.ComboBox, ThemeBuilder.Solid(Color.White), ThemeBuilder.Solid(ThemeBuilder.Hex("#7F9DB9")), 1, 0);
                theme.TextBoxBackgroundBrush = ThemeBuilder.Solid(Color.White);
                theme.TextBoxTextColor = Color.Black;
                theme.ListBoxBackgroundBrush = ThemeBuilder.Solid(Color.White);
                theme.ListBoxItemTextColor = Color.Black;
                theme.ListBoxItemBackgroundBrush = ThemeBuilder.Solid(Color.White);
                theme.ComboBoxGlyphColor = ThemeBuilder.Hex("#003C74");
                theme.Button.InvalidateResolvedCache();
                theme.ButtonShadow = new ShadowStyle { Color = new Color(0, 0, 0, 70), Offset = new Vector2(0, 2), Blur = 4 };
            });
    }
}