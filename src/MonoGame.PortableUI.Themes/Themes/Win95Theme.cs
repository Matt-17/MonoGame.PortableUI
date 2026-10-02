using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>
///     Windows 95: teal desktop, gray 4-color raised bevels on every button (call-to-action buttons
///     included — 95 had no colored buttons, the default one just gets a black ring), sunken white
///     fields, navy selection, dithered scroll track.
/// </summary>
public static class Win95Theme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("win95", "Windows 95", "selawik", ThemeEra.Desktop, ThemeBrightness.Light,
            background: "#008080", surface: "#C0C0C0", surfaceAlt: "#C0C0C0", text: "#000000",
            primary: "#000080", secondary: "#808080", selection: "#000080", selectionText: "#FFFFFF",
            danger: "#800000",
            styleTheme: theme =>
            {
                var face = ThemeBuilder.Hex("#C0C0C0");
                var light = ThemeBuilder.Hex("#DFDFDF");
                var shadow = ThemeBuilder.Hex("#808080");
                var raised = new BevelBrush(face, Color.White, light, shadow, Color.Black);
                var sunkenWhite = new BevelBrush(Color.White, Color.White, light, shadow, Color.Black).AsSunken();
                ThemeBuilder.Bevel(theme, face, Color.White, light, shadow, Color.Black);
                theme.ButtonHoverBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ButtonTextColor = Color.Black;

                // Every role is a gray bevel; the default (primary) button gets the black outer ring,
                // danger only differs by its dark red label.
                ThemeBuilder.Variants(theme, (style, color) =>
                {
                    ThemeBuilder.Chrome(style, raised, null, 0, 0);
                    style.Normal.TextColor = color == theme.Palette.Danger ? theme.Palette.Danger : Color.Black;
                    style.Pressed.Background = raised.AsSunken();
                    style.Disabled.Background = raised;
                    style.Disabled.TextColor = shadow;
                });
                theme.PrimaryButton.Normal.Background = new FrameBrush(face, new FrameRing(Color.Black, 1), new FrameRing(Color.White, 1), new FrameRing(light, 1));
                theme.VariantButtonHoverBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.VariantButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);

                foreach (var field in new[] { theme.TextBox, theme.ComboBox, theme.ListBox })
                    ThemeBuilder.Chrome(field, sunkenWhite, null, 0, 0);
                theme.TextBoxBackgroundBrush = sunkenWhite;
                theme.TextBoxTextColor = Color.Black;
                theme.ListBoxBackgroundBrush = sunkenWhite;
                theme.ListBoxItemBackgroundBrush = ThemeBuilder.Solid(Color.White);
                theme.ListBoxItemTextColor = Color.Black;
                theme.ComboBoxDropDownBackgroundBrush = ThemeBuilder.Solid(Color.White);
                theme.CheckBoxBoxBackgroundBrush = sunkenWhite;
                theme.CheckBoxCheckMarkBrush = ThemeBuilder.Solid(Color.Black);

                theme.TabHeaderBackgroundBrush = raised;
                theme.TabSelectedHeaderBackgroundBrush = new BevelBrush(light, Color.White, Color.White, shadow, Color.Black);
                theme.TabHeaderTextColor = Color.Black;
                theme.TabSelectedHeaderTextColor = Color.Black;

                theme.ScrollBarGutterBrush = PatternBrush.Dither(face, Color.White);
                theme.ScrollBarBrush = raised;
                theme.ScrollBarHoverBrush = raised;
                theme.ScrollBarPressedBrush = raised;
                theme.ProgressBarBackgroundBrush = sunkenWhite;
                theme.ProgressBarFillBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#000080"));
                theme.SliderThumbBrush = raised;
                theme.SliderTrackBrush = new BevelBrush(face, shadow, Color.White).AsSunken();
                theme.DataGridHeaderBackgroundBrush = raised;
                theme.ContextMenuBackgroundBrush = raised;
                theme.ToolTipBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#FFFFE1"));
                theme.ToolTipBorderBrush = ThemeBuilder.Solid(Color.Black);
                theme.ToolTipTextColor = Color.Black;
                theme.FocusVisualKind = MonoGame.PortableUI.Controls.FocusVisualKind.Dotted;
                theme.FocusBorderBrush = ThemeBuilder.Solid(Color.Black);
                theme.FocusBorderWidth = 1;
                theme.Button.TransitionDuration = System.TimeSpan.Zero;
            },
            reducedMotion: true);
    }
}
