using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>
///     Sci-fi HUD (space shooter / starship UI): translucent dark panels with 45° cut corners and
///     hairline frames, ice-cyan accents with a soft glow, amber secondary readouts, segmented
///     bars, wide Orbitron type and a faint scanline/bloom pass.
/// </summary>
public static class SciFiHudTheme
{
    public static ThemeDefinition Create()
    {
        var cyan = ThemeBuilder.Hex("#3FD0FF");
        var line = ThemeBuilder.Hex("#1F4D66");
        return ThemeBuilder.Catalog("scifi", "Sci-Fi HUD", "orbitron", ThemeEra.Game, ThemeBrightness.Dark,
            background: "#060A0F", surface: "#0C1822", surfaceAlt: "#0A141D", text: "#D8F3FF",
            primary: "#3FD0FF", secondary: "#F2A93B", selection: "#3FD0FF", selectionText: "#03121A",
            danger: "#FF4D5E",
            backgroundBrush: new RadialGradientBrush(ThemeBuilder.Hex("#0E2232"), ThemeBuilder.Hex("#04070B")),
            surfaceBrush: new ChamferBrush(new Color(14, 30, 44, 235), new Color(8, 18, 27, 235), line, 12, 1),
            styleTheme: theme =>
            {
                ChamferBrush Plate(string top, string bottom, string border, float chamfer = 7) =>
                    new(ThemeBuilder.Hex(top), ThemeBuilder.Hex(bottom), ThemeBuilder.Hex(border), chamfer, 1);

                var plate = Plate("#10283A", "#0A1824", "#2B6A88");
                ThemeBuilder.Chrome(theme.Button, plate, null, 0, 0);
                theme.Button.Hover.Background = Plate("#143349", "#0C1E2C", "#3FD0FF");
                theme.Button.Pressed.Background = Plate("#1E6A8C", "#124A63", "#A6EEFF");
                theme.ButtonBackgroundBrush = plate;
                theme.ButtonTextColor = ThemeBuilder.Hex("#BDEBFF");
                theme.ButtonHoverTextColor = ThemeBuilder.Hex("#EFFBFF");
                theme.ButtonPressedTextColor = Color.White;
                theme.ButtonHoverBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.Button.InvalidateResolvedCache();

                Variant(theme.PrimaryButton, Plate("#5BE0FF", "#1E9CCB", "#C8F6FF", 9), ThemeBuilder.Hex("#03121A"), new Color(63, 208, 255, 130));
                Variant(theme.SecondaryButton, Plate("#F7C266", "#C9821F", "#FFE2A8", 9), ThemeBuilder.Hex("#1A0F02"), new Color(242, 169, 59, 110));
                Variant(theme.DangerButton, Plate("#3A0E16", "#24070C", "#FF4D5E", 9), ThemeBuilder.Hex("#FF9AA4"), new Color(255, 77, 94, 110));
                theme.VariantButtonHoverBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 34));
                theme.VariantButtonPressedBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 70));

                var field = new ChamferBrush(ThemeBuilder.Hex("#07121A"), ThemeBuilder.Hex("#050D13"), line, 6, 1, ChamferCorners.BottomRight) { Accent = cyan, AccentWidth = 2 };
                foreach (var style in new[] { theme.TextBox, theme.ListBox, theme.ComboBox })
                    ThemeBuilder.Chrome(style, field, null, 0, 0);
                theme.TextBoxBackgroundBrush = field;
                theme.TextBoxHintTextColor = ThemeBuilder.Hex("#5F8296");
                theme.TextBoxCursorBrush = ThemeBuilder.Solid(cyan);
                theme.ListBoxBackgroundBrush = field;
                theme.ListBoxItemBackgroundBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ListBoxSelectedItemBackgroundBrush = new ChamferBrush(new Color(63, 208, 255, 60), cyan, 5, 1);
                theme.ListBoxSelectedItemTextColor = ThemeBuilder.Hex("#EFFBFF");
                theme.ComboBoxDropDownBackgroundBrush = ThemeBuilder.Solid(new Color(6, 14, 21, 245));
                theme.ComboBoxGlyphColor = cyan;

                theme.TabHeaderBackgroundBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.TabSelectedHeaderBackgroundBrush = new ChamferBrush(new Color(63, 208, 255, 50), cyan, 7, 1);
                theme.TabHeaderTextColor = ThemeBuilder.Hex("#6F98AD");
                theme.TabSelectedHeaderTextColor = ThemeBuilder.Hex("#EFFBFF");

                // Segmented energy bars.
                var off = new Color(0, 0, 0, 0);
                theme.ProgressBarFillBrush = new PatternBrush(8, 1, new[] { cyan, cyan, cyan, cyan, cyan, cyan, off, off });
                theme.ProgressBarBackgroundBrush = new FrameBrush(ThemeBuilder.Hex("#081620"), new FrameRing(line, 1));
                theme.SliderTrackBrush = ThemeBuilder.Solid(line);
                theme.SliderFillBrush = ThemeBuilder.Solid(cyan);
                theme.SliderThumbBrush = new ChamferBrush(ThemeBuilder.Hex("#0A1824"), cyan, 4, 1, ChamferCorners.All);
                theme.SliderThumbBorderBrush = ThemeBuilder.Solid(cyan);
                theme.ScrollBarThickness = 4;
                theme.ScrollBarGutterBrush = ThemeBuilder.Solid(new Color(31, 77, 102, 80));
                theme.ScrollBarBrush = ThemeBuilder.Solid(cyan);
                theme.ScrollBarHoverBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#A6EEFF"));
                theme.CheckBoxBoxBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#07121A"));
                theme.CheckBoxBoxBorderBrush = ThemeBuilder.Solid(cyan);
                theme.CheckBoxBoxBorderWidth = 1;
                theme.CheckBoxCheckMarkBrush = ThemeBuilder.Solid(cyan);
                theme.ToggleSwitchOffTrackBrush = ThemeBuilder.Solid(line);
                theme.ToggleSwitchOnTrackBrush = ThemeBuilder.Solid(cyan);
                theme.ToggleSwitchKnobBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#EFFBFF"));
                theme.DataGridHeaderBackgroundBrush = ThemeBuilder.Solid(new Color(63, 208, 255, 36));
                theme.DataGridHeaderTextColor = cyan;
                theme.DataGridAlternateRowBackgroundBrush = ThemeBuilder.Solid(new Color(63, 208, 255, 12));
                theme.DataGridGridLinesBrush = ThemeBuilder.Solid(new Color(63, 208, 255, 40));
                theme.ToolTipBackgroundBrush = new ChamferBrush(ThemeBuilder.Hex("#0A1824"), cyan, 5, 1);
                theme.ToolTipBorderBrush = null;
                theme.ToolTipTextColor = ThemeBuilder.Hex("#D8F3FF");
                theme.ProgressIndicatorForeground = cyan;
                theme.FocusBorderBrush = ThemeBuilder.Solid(cyan);
                theme.FocusVisualKind = MonoGame.PortableUI.Controls.FocusVisualKind.Glow;
                theme.ButtonShadow = null;
                theme.PanelShadow = new ShadowStyle { Color = new Color(63, 208, 255, 40), Offset = Vector2.Zero, Blur = 14 };
                theme.PostEffects = new PostEffect[]
                {
                    new ScanlinePostEffect { Strength = 0.035f },
                    new BloomPostEffect { Strength = 0.22f }
                };
            });
    }

    private static void Variant(ControlStyle style, Brush face, Color text, Color glow)
    {
        ThemeBuilder.Chrome(style, face, null, 0, 0);
        style.Normal.TextColor = text;
        style.Normal.Shadows = new[] { new ShadowStyle { Color = glow, Offset = Vector2.Zero, Blur = 10 } };
        style.Disabled.Background = new ChamferBrush(ThemeBuilder.Hex("#0B1620"), ThemeBuilder.Hex("#1A3343"), 9, 1);
        style.Disabled.TextColor = ThemeBuilder.Hex("#3E5A6A");
        style.Disabled.Shadows = System.Array.Empty<ShadowStyle>();
        style.InvalidateResolvedCache();
    }
}
