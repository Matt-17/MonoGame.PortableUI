using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>
///     Mac System 1–6, 1-bit: strictly black and white — grays only as dither patterns. Rounded
///     outline buttons, the thick ring of the default button, inverted selection and pressed states,
///     hard 1-bit window shadows.
/// </summary>
public static class Mac1BitTheme
{
    public static ThemeDefinition Create()
    {
        var black = Color.Black;
        var white = Color.White;
        var palette = new ThemePalette
        {
            Background = white,
            Surface = white,
            SurfaceAlt = white,
            Text = black,
            HeadingText = black,
            MutedText = black,
            Primary = black,
            Secondary = black,
            Warning = black,
            Danger = black,
            Info = black,
            Selection = black,
            SelectionText = white,
            TabText = black,
            SelectedTabText = white,
            FieldFrame = white,
            FieldBorder = black,
            DisabledSurface = white,
            DisabledText = black,
            SelectionBrush = ThemeBuilder.Solid(black),
            // The classic desktop: a 50 % checker behind everything.
            BackgroundBrush = PatternBrush.Dither(white, ThemeBuilder.Hex("#D8D8D8"))
        };

        return ThemeBuilder.CreateDefinition("mac1bit", "Mac System 1-bit", "atkinsonhyperlegible", ThemeEra.Desktop, ThemeBrightness.Light, palette, white,
            styleTheme: theme =>
            {
                theme.Cursor = CursorStyle.Arrow(Color.Black, Color.White);
                var dither = PatternBrush.Dither(white, black);
                ThemeBuilder.Chrome(theme.Button, ThemeBuilder.Solid(white), ThemeBuilder.Solid(black), 1, 8);
                theme.Button.Pressed.Background = ThemeBuilder.Solid(black);
                theme.ButtonPressedTextColor = white;
                theme.ButtonHoverBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.Button.InvalidateResolvedCache();

                ThemeBuilder.Variants(theme, (style, _) =>
                {
                    ThemeBuilder.Chrome(style, ThemeBuilder.Solid(white), ThemeBuilder.Solid(black), 1, 8);
                    style.Normal.TextColor = black;
                    style.Pressed.Background = ThemeBuilder.Solid(black);
                    style.Pressed.TextColor = white;
                    style.Disabled.Background = ThemeBuilder.Solid(white);
                });
                // The default button's thick ring; danger is drawn inverted.
                theme.PrimaryButton.Normal.BorderThickness = new MonoGame.PortableUI.Common.Thickness(3);
                theme.DangerButton.Normal.Background = ThemeBuilder.Solid(black);
                theme.DangerButton.Normal.TextColor = white;
                theme.PrimaryButton.InvalidateResolvedCache();
                theme.DangerButton.InvalidateResolvedCache();
                theme.VariantButtonHoverBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.VariantButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.DisabledOverlayBrush = PatternBrush.Dither(new Color(255, 255, 255, 255), new Color(0, 0, 0, 0));
                theme.DisabledTextColor = black;

                foreach (var field in new[] { theme.TextBox, theme.ListBox, theme.ComboBox })
                    ThemeBuilder.Chrome(field, ThemeBuilder.Solid(white), ThemeBuilder.Solid(black), 1, 0);
                theme.TextBoxBackgroundBrush = ThemeBuilder.Solid(white);
                theme.TextBoxHintTextColor = black;
                theme.ListBoxBackgroundBrush = ThemeBuilder.Solid(white);
                theme.ListBoxItemBackgroundBrush = ThemeBuilder.Solid(white);
                theme.TabHeaderBackgroundBrush = ThemeBuilder.Solid(white);
                theme.TabSelectedHeaderBackgroundBrush = ThemeBuilder.Solid(black);
                theme.ScrollBarGutterBrush = dither;
                theme.ScrollBarBrush = new FrameBrush(white, new FrameRing(black, 1));
                theme.ScrollBarHoverBrush = new FrameBrush(white, new FrameRing(black, 2));
                theme.ScrollBarPressedBrush = ThemeBuilder.Solid(black);
                theme.ProgressBarBackgroundBrush = new FrameBrush(white, new FrameRing(black, 1));
                theme.ProgressBarFillBrush = ThemeBuilder.Solid(black);
                theme.SliderTrackBrush = dither;
                theme.SliderFillBrush = ThemeBuilder.Solid(black);
                theme.SliderThumbBrush = ThemeBuilder.Solid(white);
                theme.SliderThumbBorderBrush = ThemeBuilder.Solid(black);
                theme.CheckBoxBoxBackgroundBrush = ThemeBuilder.Solid(white);
                theme.CheckBoxBoxBorderBrush = ThemeBuilder.Solid(black);
                theme.CheckBoxBoxBorderWidth = 1;
                theme.CheckBoxCheckMarkBrush = ThemeBuilder.Solid(black);
                theme.CheckBoxGlyphKind = MonoGame.PortableUI.Controls.CheckBoxGlyphKind.Cross;
                theme.ToggleSwitchOffTrackBrush = dither;
                theme.ToggleSwitchOnTrackBrush = ThemeBuilder.Solid(black);
                theme.ToggleSwitchKnobBrush = ThemeBuilder.Solid(white);
                theme.ProgressIndicatorForeground = black;
                theme.DataGridHeaderBackgroundBrush = ThemeBuilder.Solid(white);
                theme.DataGridAlternateRowBackgroundBrush = ThemeBuilder.Solid(white);
                theme.DataGridGridLinesBrush = ThemeBuilder.Solid(black);
                theme.ToolTipBackgroundBrush = ThemeBuilder.Solid(white);
                theme.ToolTipBorderBrush = ThemeBuilder.Solid(black);
                theme.ToolTipTextColor = black;
                theme.BadgeBackgroundBrush = ThemeBuilder.Solid(black);
                theme.BadgeTextColor = white;
                theme.FocusBorderBrush = ThemeBuilder.Solid(black);
                theme.ButtonShadow = null;
                // Windows cast a hard 1-bit shadow.
                theme.PanelShadow = new ShadowStyle { Color = black, Offset = new Vector2(3, 3), Blur = 0 };
            },
            reducedMotion: true);
    }
}
