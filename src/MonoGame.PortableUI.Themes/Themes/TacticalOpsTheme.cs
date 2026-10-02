using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>
///     Tactical ops (military shooter menus): near-black desaturated panels over a faint diagonal
///     hatch, one hot amber accent, angled cut corners, a thick accent bar marking the selected
///     row, olive secondary, condensed Barlow type. Square, hard, no gloss.
/// </summary>
public static class TacticalOpsTheme
{
    public static ThemeDefinition Create()
    {
        var amber = ThemeBuilder.Hex("#FFB000");
        var steel = ThemeBuilder.Hex("#3A4046");
        var hatchBase = ThemeBuilder.Hex("#0E1012");
        var hatchLine = ThemeBuilder.Hex("#14171A");
        return ThemeBuilder.Catalog("tactical", "Tactical Ops", "barlowcondensed", ThemeEra.Game, ThemeBrightness.Dark,
            background: "#0E1012", surface: "#171A1D", surfaceAlt: "#1E2226", text: "#E8E8E8",
            primary: "#FFB000", secondary: "#6B7A4B", selection: "#FFB000", selectionText: "#111111",
            danger: "#E5484D",
            backgroundBrush: new PatternBrush(6, 6, Hatch(hatchBase, hatchLine)),
            surfaceBrush: new ChamferBrush(new Color(23, 26, 29, 240), new Color(23, 26, 29, 240), ThemeBuilder.Hex("#2A2F34"), 14, 1, ChamferCorners.TopRight),
            styleTheme: theme =>
            {
                theme.Cursor = CursorStyle.ModernArrow(ThemeBuilder.Hex("#FFB000"), ThemeBuilder.Hex("#111111"));
                ChamferBrush Cut(string fill, string border, ChamferCorners corners = ChamferCorners.TopRight | ChamferCorners.BottomLeft) =>
                    new(ThemeBuilder.Hex(fill), ThemeBuilder.Hex(border), 8, 1, corners);

                ThemeBuilder.Chrome(theme.Button, Cut("#1E2226", "#3A4046"), null, 0, 0);
                theme.Button.Hover.Background = Cut("#262B30", "#FFB000");
                theme.Button.Pressed.Background = Cut("#FFB000", "#FFD266");
                theme.Button.Pressed.TextColor = ThemeBuilder.Hex("#111111");
                theme.ButtonBackgroundBrush = Cut("#1E2226", "#3A4046");
                theme.ButtonTextColor = ThemeBuilder.Hex("#E8E8E8");
                theme.ButtonHoverTextColor = amber;
                theme.ButtonPressedTextColor = ThemeBuilder.Hex("#111111");
                theme.ButtonHoverBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.Button.InvalidateResolvedCache();

                Variant(theme.PrimaryButton, Cut("#FFB000", "#FFD266"), ThemeBuilder.Hex("#111111"));
                Variant(theme.SecondaryButton, Cut("#4E5A35", "#8A9A62"), ThemeBuilder.Hex("#EEF2E2"));
                Variant(theme.DangerButton, Cut("#3A1214", "#E5484D"), ThemeBuilder.Hex("#FF8A8E"));
                theme.VariantButtonHoverBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 30));
                theme.VariantButtonPressedBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 80));

                var field = ThemeBuilder.Solid(ThemeBuilder.Hex("#101315"));
                foreach (var style in new[] { theme.TextBox, theme.ListBox, theme.ComboBox })
                    ThemeBuilder.Chrome(style, field, ThemeBuilder.Solid(steel), 1, 0);
                theme.TextBoxBackgroundBrush = field;
                theme.TextBoxHintTextColor = ThemeBuilder.Hex("#6E757C");
                theme.TextBoxCursorBrush = ThemeBuilder.Solid(amber);
                theme.ListBoxBackgroundBrush = field;
                theme.ListBoxItemBackgroundBrush = ThemeBuilder.Solid(Color.Transparent);
                // Selected row: lifted plate with the thick amber bar on the left.
                theme.ListBoxSelectedItemBackgroundBrush = new ChamferBrush(ThemeBuilder.Hex("#2A2F34"), Color.Transparent, 0, 0) { Accent = amber, AccentWidth = 4 };
                theme.ListBoxSelectedItemTextColor = ThemeBuilder.Hex("#FFFFFF");
                theme.ComboBoxDropDownBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#15181B"));
                theme.ComboBoxGlyphColor = amber;

                theme.TabHeaderBackgroundBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.TabSelectedHeaderBackgroundBrush = Cut("#FFB000", "#FFB000", ChamferCorners.TopRight);
                theme.TabHeaderTextColor = ThemeBuilder.Hex("#9AA0A6");
                theme.TabSelectedHeaderTextColor = ThemeBuilder.Hex("#111111");

                var off = new Color(0, 0, 0, 0);
                theme.ProgressBarFillBrush = new PatternBrush(5, 1, new[] { amber, amber, amber, amber, off });
                theme.ProgressBarBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#24292E"));
                theme.SliderTrackBrush = ThemeBuilder.Solid(steel);
                theme.SliderFillBrush = ThemeBuilder.Solid(amber);
                theme.SliderThumbBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#E8E8E8"));
                theme.SliderThumbBorderBrush = ThemeBuilder.Solid(amber);
                theme.ScrollBarThickness = 4;
                theme.ScrollBarGutterBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#1A1D20"));
                theme.ScrollBarBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#5A6168"));
                theme.ScrollBarHoverBrush = ThemeBuilder.Solid(amber);
                theme.CheckBoxBoxBackgroundBrush = field;
                theme.CheckBoxBoxBorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#5A6168"));
                theme.CheckBoxBoxBorderWidth = 1;
                theme.CheckBoxCheckMarkBrush = ThemeBuilder.Solid(amber);
                theme.CheckBoxGlyphKind = MonoGame.PortableUI.Controls.CheckBoxGlyphKind.Cross;
                theme.ToggleSwitchOffTrackBrush = ThemeBuilder.Solid(steel);
                theme.ToggleSwitchOnTrackBrush = ThemeBuilder.Solid(amber);
                theme.ToggleSwitchKnobBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#E8E8E8"));
                theme.DataGridHeaderBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#24292E"));
                theme.DataGridHeaderTextColor = amber;
                theme.DataGridAlternateRowBackgroundBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 6));
                theme.DataGridGridLinesBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 20));
                theme.ToolTipBackgroundBrush = new ChamferBrush(ThemeBuilder.Hex("#171A1D"), Color.Transparent, 0, 0) { Accent = amber, AccentWidth = 3 };
                theme.ToolTipBorderBrush = null;
                theme.ToolTipTextColor = ThemeBuilder.Hex("#E8E8E8");
                theme.BadgeBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#E5484D"));
                theme.ProgressIndicatorForeground = amber;
                theme.FocusBorderBrush = ThemeBuilder.Solid(amber);
                theme.ButtonShadow = null;
                theme.PanelShadow = null;
                theme.PostEffects = new PostEffect[] { new FilmGrainPostEffect { Strength = 0.02f } };
            });
    }

    private static void Variant(ControlStyle style, Brush face, Color text)
    {
        ThemeBuilder.Chrome(style, face, null, 0, 0);
        style.Normal.TextColor = text;
        style.Disabled.Background = new ChamferBrush(ThemeBuilder.Hex("#16191C"), ThemeBuilder.Hex("#2A2F34"), 8, 1, ChamferCorners.TopRight | ChamferCorners.BottomLeft);
        style.Disabled.TextColor = ThemeBuilder.Hex("#5A6168");
        style.InvalidateResolvedCache();
    }

    /// <summary>A 6×6 tile with one 45° line: the faint diagonal hatch behind everything.</summary>
    private static Color[] Hatch(Color background, Color line)
    {
        var pixels = new Color[36];
        for (var y = 0; y < 6; y++)
        {
            for (var x = 0; x < 6; x++)
                pixels[y * 6 + x] = (x + y) % 6 == 0 ? line : background;
        }
        return pixels;
    }
}
