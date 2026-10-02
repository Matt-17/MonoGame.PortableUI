using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Themes;

/// <summary>
///     E-Ink Paper (Kindle-like): warm paper, near-black ink and a few gray levels only — no color,
///     no gradients, no shadows, no motion. Emphasis comes from inversion and line weight: primary
///     buttons are solid ink, danger gets a heavy frame.
/// </summary>
public static class EInkTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("eink", "E-Ink Paper", "atkinsonhyperlegible", ThemeEra.Modern, ThemeBrightness.Light,
            background: "#F4F1EA", surface: "#FBFAF6", surfaceAlt: "#E2DFD8", text: "#1A1A1A",
            primary: "#1A1A1A", secondary: "#555555", selection: "#1A1A1A", selectionText: "#FBFAF6",
            danger: "#1A1A1A",
            styleTheme: theme =>
            {
                var ink = ThemeBuilder.Hex("#1A1A1A");
                var paper = ThemeBuilder.Hex("#FBFAF6");
                var gray = ThemeBuilder.Hex("#888888");
                ThemeBuilder.Chrome(theme.Button, ThemeBuilder.Solid(paper), ThemeBuilder.Solid(ink), 1, 2);
                theme.Button.Pressed.Background = ThemeBuilder.Solid(ink);
                theme.ButtonPressedTextColor = paper;
                theme.ButtonHoverBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 18));
                theme.ButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.Button.InvalidateResolvedCache();

                ThemeBuilder.Variants(theme, (style, _) =>
                {
                    ThemeBuilder.Chrome(style, ThemeBuilder.Solid(paper), ThemeBuilder.Solid(ink), 2, 2);
                    style.Normal.TextColor = ink;
                    style.Disabled.Background = ThemeBuilder.Solid(paper);
                    style.Disabled.BorderBrush = ThemeBuilder.Solid(gray);
                    style.Disabled.TextColor = gray;
                });
                // Primary = inverted ink block; danger = heavy 4 px frame.
                theme.PrimaryButton.Normal.Background = ThemeBuilder.Solid(ink);
                theme.PrimaryButton.Normal.TextColor = paper;
                theme.DangerButton.Normal.BorderThickness = new Thickness(4);
                theme.PrimaryButton.InvalidateResolvedCache();
                theme.DangerButton.InvalidateResolvedCache();
                theme.VariantButtonHoverBrush = ThemeBuilder.Solid(new Color(128, 128, 128, 40));
                theme.VariantButtonPressedBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 90));

                foreach (var field in new[] { theme.TextBox, theme.ListBox, theme.ComboBox })
                    ThemeBuilder.Chrome(field, ThemeBuilder.Solid(paper), ThemeBuilder.Solid(ink), 1, 0);
                theme.TextBoxBackgroundBrush = ThemeBuilder.Solid(paper);
                theme.TextBoxHintTextColor = ThemeBuilder.Hex("#6E6E6E");
                theme.ListBoxBackgroundBrush = ThemeBuilder.Solid(paper);
                theme.ListBoxItemBackgroundBrush = ThemeBuilder.Solid(paper);
                theme.TabHeaderBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#F4F1EA"));
                theme.TabSelectedHeaderBackgroundBrush = ThemeBuilder.Solid(ink);
                theme.ProgressBarBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#DDDDDD"));
                theme.ProgressBarFillBrush = ThemeBuilder.Solid(ink);
                theme.SliderTrackBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#BBBBBB"));
                theme.SliderFillBrush = ThemeBuilder.Solid(ink);
                theme.SliderThumbBrush = ThemeBuilder.Solid(paper);
                theme.SliderThumbBorderBrush = ThemeBuilder.Solid(ink);
                theme.ScrollBarBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#555555"));
                theme.ScrollBarHoverBrush = ThemeBuilder.Solid(ink);
                theme.CheckBoxCheckMarkBrush = ThemeBuilder.Solid(ink);
                theme.CheckBoxBoxBorderBrush = ThemeBuilder.Solid(ink);
                theme.ToggleSwitchOnTrackBrush = ThemeBuilder.Solid(ink);
                theme.ToggleSwitchKnobBrush = ThemeBuilder.Solid(paper);
                theme.BadgeBackgroundBrush = ThemeBuilder.Solid(ink);
                theme.BadgeTextColor = paper;
                theme.ProgressIndicatorForeground = ink;
                theme.ToolTipBackgroundBrush = ThemeBuilder.Solid(paper);
                theme.ToolTipBorderBrush = ThemeBuilder.Solid(ink);
                theme.ButtonShadow = null;
                theme.PanelShadow = null;
            },
            reducedMotion: true);
    }
}
