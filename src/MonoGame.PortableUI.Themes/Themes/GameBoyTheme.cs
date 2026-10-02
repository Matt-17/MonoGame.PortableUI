using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>
///     Game Boy DMG: strictly the four LCD shades (#9BBC0F, #8BAC0F, #306230, #0F380F), pixel font,
///     double-line dialog frames with notched pixel corners, solid ink selections. No gradients,
///     no blur, no motion.
/// </summary>
public static class GameBoyTheme
{
    public static ThemeDefinition Create()
    {
        var lightest = ThemeBuilder.Hex("#9BBC0F");
        var light = ThemeBuilder.Hex("#8BAC0F");
        var dark = ThemeBuilder.Hex("#306230");
        var ink = ThemeBuilder.Hex("#0F380F");
        var palette = new ThemePalette
        {
            Background = light,
            Surface = lightest,
            SurfaceAlt = light,
            Text = ink,
            HeadingText = ink,
            MutedText = dark,
            Primary = ink,
            Secondary = dark,
            Warning = dark,
            Danger = ink,
            Info = dark,
            Selection = ink,
            SelectionText = lightest,
            TabText = ink,
            SelectedTabText = lightest,
            FieldFrame = lightest,
            FieldBorder = ink,
            DisabledSurface = light,
            DisabledText = dark,
            SelectionBrush = ThemeBuilder.Solid(ink),
            // The dialog box: lightest fill inside a dark / light / dark double line.
            SurfaceBrush = new FrameBrush(lightest, new FrameRing(ink, 2), new FrameRing(lightest, 2), new FrameRing(dark, 1)) { NotchCorners = true }
        };

        return ThemeBuilder.CreateDefinition("gameboy", "Game Boy DMG", "silkscreen", ThemeEra.Retro, ThemeBrightness.Light, palette, light,
            styleTheme: theme =>
            {
                FrameBrush Box(Color face, Color frame, float width = 2) => new FrameBrush(face, new FrameRing(frame, width)) { NotchCorners = true };

                ThemeBuilder.Chrome(theme.Button, Box(lightest, ink), null, 0, 0);
                theme.Button.Hover.Background = Box(light, ink);
                theme.Button.Pressed.Background = Box(ink, ink);
                theme.ButtonBackgroundBrush = Box(lightest, ink);
                theme.ButtonTextColor = ink;
                theme.ButtonHoverTextColor = ink;
                theme.ButtonPressedTextColor = lightest;
                theme.ButtonHoverBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.Button.InvalidateResolvedCache();

                ThemeBuilder.Variants(theme, (style, color) =>
                {
                    ThemeBuilder.Chrome(style, Box(color, ink), null, 0, 0);
                    style.Normal.TextColor = lightest;
                    style.Hover.Background = Box(color == ink ? dark : ink, ink);
                    style.Pressed.Background = Box(lightest, ink);
                    style.Pressed.TextColor = ink;
                    style.Disabled.Background = Box(light, dark);
                    style.Disabled.TextColor = dark;
                });
                // Danger: the light dialog box with a heavy double frame instead of a fill.
                theme.DangerButton.Normal.Background = new FrameBrush(lightest, new FrameRing(ink, 2), new FrameRing(lightest, 1), new FrameRing(ink, 2)) { NotchCorners = true };
                theme.DangerButton.Normal.TextColor = ink;
                theme.DangerButton.Hover.Background = new FrameBrush(light, new FrameRing(ink, 2), new FrameRing(light, 1), new FrameRing(ink, 2)) { NotchCorners = true };
                theme.DangerButton.InvalidateResolvedCache();
                theme.VariantButtonHoverBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.VariantButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);

                var field = Box(lightest, ink, 1);
                foreach (var style in new[] { theme.TextBox, theme.ListBox, theme.ComboBox })
                    ThemeBuilder.Chrome(style, field, null, 0, 0);
                theme.TextBoxBackgroundBrush = field;
                theme.TextBoxTextColor = ink;
                theme.TextBoxHintTextColor = dark;
                theme.TextBoxCursorBrush = ThemeBuilder.Solid(ink);
                theme.TextBoxSelectionBrush = ThemeBuilder.Solid(dark);
                theme.ListBoxBackgroundBrush = field;
                theme.ListBoxItemBackgroundBrush = ThemeBuilder.Solid(lightest);
                theme.ListBoxSelectedItemBackgroundBrush = ThemeBuilder.Solid(ink);
                theme.ComboBoxDropDownBackgroundBrush = field;
                theme.ComboBoxGlyphColor = ink;

                theme.TabHeaderBackgroundBrush = ThemeBuilder.Solid(light);
                theme.TabSelectedHeaderBackgroundBrush = Box(ink, ink);
                theme.ScrollBarGutterBrush = ThemeBuilder.Solid(light);
                theme.ScrollBarBrush = ThemeBuilder.Solid(ink);
                theme.ScrollBarHoverBrush = ThemeBuilder.Solid(dark);
                theme.ScrollBarPressedBrush = ThemeBuilder.Solid(dark);
                theme.ProgressBarBackgroundBrush = Box(lightest, ink, 1);
                theme.ProgressBarFillBrush = ThemeBuilder.Solid(ink);
                theme.SliderTrackBrush = ThemeBuilder.Solid(dark);
                theme.SliderFillBrush = ThemeBuilder.Solid(ink);
                theme.SliderThumbBrush = ThemeBuilder.Solid(lightest);
                theme.SliderThumbBorderBrush = ThemeBuilder.Solid(ink);
                theme.CheckBoxBoxBackgroundBrush = ThemeBuilder.Solid(lightest);
                theme.CheckBoxBoxBorderBrush = ThemeBuilder.Solid(ink);
                theme.CheckBoxCheckMarkBrush = ThemeBuilder.Solid(ink);
                theme.RadioButtonDotBrush = ThemeBuilder.Solid(ink);
                theme.ToggleSwitchOffTrackBrush = ThemeBuilder.Solid(dark);
                theme.ToggleSwitchOnTrackBrush = ThemeBuilder.Solid(ink);
                theme.ToggleSwitchKnobBrush = ThemeBuilder.Solid(lightest);
                theme.BadgeBackgroundBrush = ThemeBuilder.Solid(ink);
                theme.BadgeTextColor = lightest;
                theme.ProgressIndicatorForeground = ink;
                theme.DataGridHeaderBackgroundBrush = ThemeBuilder.Solid(ink);
                theme.DataGridHeaderTextColor = lightest;
                theme.DataGridAlternateRowBackgroundBrush = ThemeBuilder.Solid(light);
                theme.DataGridGridLinesBrush = ThemeBuilder.Solid(dark);
                theme.ToolTipBackgroundBrush = Box(lightest, ink);
                theme.ToolTipBorderBrush = null;
                theme.ToolTipTextColor = ink;
                theme.FocusBorderBrush = ThemeBuilder.Solid(ink);
                theme.DisabledOverlayBrush = null;
                theme.ButtonShadow = null;
                theme.PanelShadow = null;
            },
            reducedMotion: true);
    }
}
