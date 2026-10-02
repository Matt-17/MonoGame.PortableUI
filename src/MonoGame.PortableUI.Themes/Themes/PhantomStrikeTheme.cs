using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>
///     Phantom Strike (stylish anime-RPG menus): red, black and white only, halftone dots, jagged
///     cut-corner plates with hard offset shadows, inverted white input slips, heavy Anton type.
/// </summary>
public static class PhantomStrikeTheme
{
    public static ThemeDefinition Create()
    {
        var red = ThemeBuilder.Hex("#E5191C");
        var black = ThemeBuilder.Hex("#0A0A0A");
        var white = Color.White;
        return ThemeBuilder.Catalog("phantom", "Phantom Strike", "anton", ThemeEra.Game, ThemeBrightness.Dark,
            background: "#0A0A0A", surface: "#111111", surfaceAlt: "#1A1A1A", text: "#FFFFFF",
            primary: "#E5191C", secondary: "#FFFFFF", selection: "#E5191C", selectionText: "#FFFFFF",
            danger: "#FFE600",
            backgroundBrush: new PatternBrush(4, 4, Halftone(ThemeBuilder.Hex("#0A0A0A"), ThemeBuilder.Hex("#3A0A0C"))),
            surfaceBrush: new ChamferBrush(ThemeBuilder.Hex("#0D0D0D"), ThemeBuilder.Hex("#0D0D0D"), white, 16, 3, ChamferCorners.TopLeft | ChamferCorners.BottomRight),
            styleTheme: theme =>
            {
                theme.Cursor = CursorStyle.ModernArrow(ThemeBuilder.Hex("#E5191C"), Color.Black);
                ChamferBrush Slash(Color fill, Color border, float thickness = 2) =>
                    new(fill, border, 10, thickness, ChamferCorners.TopLeft | ChamferCorners.BottomRight);
                ShadowStyle Offset(Color color) => new() { Color = color, Offset = new Vector2(5, 5), Blur = 0 };

                ThemeBuilder.Chrome(theme.Button, Slash(black, white), null, 0, 0);
                theme.Button.Hover.Background = Slash(red, white);
                theme.Button.Pressed.Background = Slash(white, black);
                theme.Button.Pressed.TextColor = black;
                theme.ButtonBackgroundBrush = Slash(black, white);
                theme.ButtonTextColor = white;
                theme.ButtonHoverTextColor = white;
                theme.ButtonPressedTextColor = black;
                theme.ButtonHoverBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.Button.InvalidateResolvedCache();

                Variant(theme.PrimaryButton, Slash(red, black), white, Offset(white));
                Variant(theme.SecondaryButton, Slash(white, black), black, Offset(red));
                Variant(theme.DangerButton, Slash(ThemeBuilder.Hex("#FFE600"), black), black, Offset(red));
                theme.VariantButtonHoverBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 50));
                theme.VariantButtonPressedBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 90));

                // Inputs are inverted white slips.
                var slip = new ChamferBrush(white, black, 6, 2, ChamferCorners.BottomRight);
                foreach (var style in new[] { theme.TextBox, theme.ListBox, theme.ComboBox })
                    ThemeBuilder.Chrome(style, slip, null, 0, 0);
                theme.TextBoxBackgroundBrush = slip;
                theme.TextBoxTextColor = black;
                theme.TextBoxHintTextColor = ThemeBuilder.Hex("#8A8A8A");
                theme.TextBoxCursorBrush = ThemeBuilder.Solid(red);
                theme.TextBoxSelectionBrush = ThemeBuilder.Solid(new Color(229, 25, 28, 120));
                theme.ComboBox.Normal.TextColor = black;
                theme.ListBoxBackgroundBrush = slip;
                theme.ListBoxItemBackgroundBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ListBoxItemTextColor = black;
                theme.ListBoxSelectedItemBackgroundBrush = new ChamferBrush(red, black, 8, 0, ChamferCorners.TopLeft | ChamferCorners.BottomRight);
                theme.ListBoxSelectedItemTextColor = white;
                theme.ComboBoxDropDownBackgroundBrush = ThemeBuilder.Solid(white);
                theme.ComboBoxGlyphColor = red;

                theme.TabHeaderBackgroundBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.TabSelectedHeaderBackgroundBrush = new ChamferBrush(red, white, 9, 2, ChamferCorners.TopLeft | ChamferCorners.BottomRight);
                theme.TabHeaderTextColor = white;
                theme.TabSelectedHeaderTextColor = white;

                theme.ProgressBarBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#2A2A2A"));
                theme.ProgressBarFillBrush = ThemeBuilder.Solid(red);
                theme.SliderTrackBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#3A3A3A"));
                theme.SliderFillBrush = ThemeBuilder.Solid(red);
                theme.SliderThumbBrush = ThemeBuilder.Solid(white);
                theme.SliderThumbBorderBrush = ThemeBuilder.Solid(black);
                theme.ScrollBarGutterBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ScrollBarBrush = ThemeBuilder.Solid(red);
                theme.ScrollBarHoverBrush = ThemeBuilder.Solid(white);
                theme.CheckBoxBoxBackgroundBrush = ThemeBuilder.Solid(white);
                theme.CheckBoxBoxBorderBrush = ThemeBuilder.Solid(black);
                theme.CheckBoxCheckMarkBrush = ThemeBuilder.Solid(red);
                theme.ToggleSwitchOffTrackBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#3A3A3A"));
                theme.ToggleSwitchOnTrackBrush = ThemeBuilder.Solid(red);
                theme.ToggleSwitchKnobBrush = ThemeBuilder.Solid(white);
                theme.BadgeBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#FFE600"));
                theme.DataGridHeaderBackgroundBrush = ThemeBuilder.Solid(red);
                theme.DataGridHeaderTextColor = white;
                theme.DataGridAlternateRowBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#161616"));
                theme.DataGridGridLinesBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 30));
                theme.ToolTipBackgroundBrush = Slash(white, black);
                theme.ToolTipBorderBrush = null;
                theme.ToolTipTextColor = black;
                theme.ProgressIndicatorForeground = red;
                theme.FocusBorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#FFE600"));
                theme.ButtonShadow = null;
                theme.PanelShadow = new ShadowStyle { Color = red, Offset = new Vector2(8, 8), Blur = 0 };
            });
    }

    private static void Variant(ControlStyle style, Brush face, Color text, ShadowStyle shadow)
    {
        ThemeBuilder.Chrome(style, face, null, 0, 0);
        style.Normal.TextColor = text;
        style.Normal.Shadows = new[] { shadow };
        style.Pressed.Shadows = System.Array.Empty<ShadowStyle>();
        style.Disabled.Background = new ChamferBrush(ThemeBuilder.Hex("#1E1E1E"), ThemeBuilder.Hex("#444444"), 10, 2, ChamferCorners.TopLeft | ChamferCorners.BottomRight);
        style.Disabled.TextColor = ThemeBuilder.Hex("#6A6A6A");
        style.Disabled.Shadows = System.Array.Empty<ShadowStyle>();
        style.InvalidateResolvedCache();
    }

    /// <summary>A 4×4 tile with one dark red dot: the halftone backdrop.</summary>
    private static Color[] Halftone(Color background, Color dot)
    {
        var pixels = new Color[16];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = background;
        pixels[5] = dot;
        return pixels;
    }
}
