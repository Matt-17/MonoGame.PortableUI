using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>
///     Neumorphism: everything is the same pale material, shaped only by a light top-left and a dark
///     bottom-right shadow; pressed buttons and input fields are pressed into the surface (inset
///     shadows). One soft violet accent, no borders.
/// </summary>
public static class NeumorphicTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("neumorphic", "Neumorphism", "atkinsonhyperlegible", ThemeEra.Modern, ThemeBrightness.Light,
            background: "#E0E5EC", surface: "#E0E5EC", surfaceAlt: "#E0E5EC", text: "#44476A",
            primary: "#6D5DFC", secondary: "#E0E5EC", selection: "#6D5DFC", selectionText: "#FFFFFF",
            danger: "#E0E5EC",
            styleTheme: theme =>
            {
                var baseColor = ThemeBuilder.Hex("#E0E5EC");
                var accent = ThemeBuilder.Hex("#6D5DFC");
                var face = ThemeBuilder.Solid(baseColor);

                ThemeBuilder.Chrome(theme.Button, face, null, 0, 14);
                theme.Button.Normal.Shadows = new[] { Raised(5) };
                theme.Button.Pressed.Shadows = new[] { Pressed(3) };
                theme.Button.Pressed.Background = face;
                theme.ButtonBackgroundBrush = face;
                theme.ButtonHoverBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 40));
                theme.ButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ButtonPressedTextColor = accent;
                theme.Button.InvalidateResolvedCache();

                ThemeBuilder.Variants(theme, (style, _) =>
                {
                    ThemeBuilder.Chrome(style, face, null, 0, 14);
                    style.Normal.Shadows = new[] { Raised(5) };
                    style.Pressed.Shadows = new[] { Pressed(3) };
                    style.Disabled.Background = face;
                    style.Disabled.Shadows = new[] { Raised(2) };
                    style.Disabled.TextColor = ThemeBuilder.Hex("#A3AEC2");
                });
                // Primary is the one accent-filled control; the others are base material with tinted labels.
                theme.PrimaryButton.Normal.Background = ThemeBuilder.Gloss((0, "#8577FF"), (1, "#5B4AF0"));
                theme.PrimaryButton.Normal.TextColor = Color.White;
                theme.SecondaryButton.Normal.TextColor = accent;
                theme.DangerButton.Normal.TextColor = ThemeBuilder.Hex("#E5484D");
                theme.PrimaryButton.InvalidateResolvedCache();
                theme.SecondaryButton.InvalidateResolvedCache();
                theme.DangerButton.InvalidateResolvedCache();
                theme.VariantButtonHoverBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 36));
                theme.VariantButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);

                // Wells: darker at the top edge, lighter at the bottom — read as pressed in.
                var well = ThemeBuilder.Gloss((0, "#C9D1DC"), (0.35f, "#D6DDE6"), (1, "#E6EBF1"));
                foreach (var field in new[] { theme.TextBox, theme.ListBox, theme.ComboBox })
                {
                    ThemeBuilder.Chrome(field, well, null, 0, 12);
                    field.Normal.Shadows = new[] { Pressed(3) };
                    field.InvalidateResolvedCache();
                }
                ThemeBuilder.Chrome(theme.ComboBox, face, null, 0, 12);
                theme.ComboBox.Normal.Shadows = new[] { Raised(4) };
                theme.TextBoxBackgroundBrush = well;
                theme.ListBoxBackgroundBrush = well;
                theme.ListBoxItemBackgroundBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.TabHeaderBackgroundBrush = face;
                theme.TabSelectedHeaderBackgroundBrush = ThemeBuilder.Solid(accent);
                theme.ProgressBarBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#CDD4DF"));
                theme.ProgressBarFillBrush = ThemeBuilder.Gloss((0, "#8577FF"), (1, "#5B4AF0"));
                theme.SliderTrackBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#CDD4DF"));
                theme.SliderFillBrush = ThemeBuilder.Solid(accent);
                theme.SliderThumbBrush = face;
                theme.SliderThumbBorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#C5CDD9"));
                theme.ScrollBarGutterBrush = face;
                theme.ScrollBarBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#B8C2D0"));
                theme.ScrollBarHoverBrush = ThemeBuilder.Solid(accent);
                theme.CheckBoxBoxBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#D5DCE6"));
                theme.CheckBoxBoxBorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#C5CDD9"));
                theme.CheckBoxCheckMarkBrush = ThemeBuilder.Solid(accent);
                theme.ToggleSwitchOffTrackBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#CDD4DF"));
                theme.ToggleSwitchOnTrackBrush = ThemeBuilder.Solid(accent);
                theme.ToggleSwitchKnobBrush = face;
                theme.ProgressIndicatorForeground = accent;
                theme.DataGridHeaderBackgroundBrush = face;
                theme.DataGridAlternateRowBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#D9DFE8"));
                theme.ButtonShadow = Raised(5);
                theme.PanelShadow = Raised(9);
            });
    }

    /// <summary>Extruded: dark shadow to the bottom right, light one to the top left.</summary>
    private static ShadowStyle Raised(float distance)
    {
        return new ShadowStyle
        {
            Color = new Color(163, 177, 198, 170),
            Offset = new Vector2(distance, distance),
            Blur = distance * 2,
            Also = new ShadowStyle { Color = new Color(255, 255, 255, 230), Offset = new Vector2(-distance, -distance), Blur = distance * 2 }
        };
    }

    /// <summary>Pressed into the surface: the same pair as inset shadows.</summary>
    private static ShadowStyle Pressed(float distance)
    {
        return new ShadowStyle
        {
            Inset = true,
            Color = new Color(163, 177, 198, 190),
            Offset = new Vector2(distance, distance),
            Blur = distance * 2,
            Also = new ShadowStyle { Inset = true, Color = new Color(255, 255, 255, 220), Offset = new Vector2(-distance, -distance), Blur = distance * 2 }
        };
    }
}
