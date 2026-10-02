using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>
///     Cozy casual (mobile / life-sim / puzzle games): sky gradient, cream cards with chunky brown
///     outlines, big rounded candy buttons that sit on a solid darker "lip" and sink into it when
///     pressed, rounded Fredoka type.
/// </summary>
public static class CozyCasualTheme
{
    public static ThemeDefinition Create()
    {
        var brown = ThemeBuilder.Hex("#7A5230");
        var cream = ThemeBuilder.Hex("#FFF6E0");
        return ThemeBuilder.Catalog("cozy", "Cozy Casual", "fredoka", ThemeEra.Game, ThemeBrightness.Light,
            background: "#BDE8FF", surface: "#FFF6E0", surfaceAlt: "#FCEBC7", text: "#5A3E2B",
            primary: "#5AC83A", secondary: "#FFC93C", selection: "#3DA5F4", selectionText: "#FFFFFF",
            danger: "#FF6B6B",
            backgroundBrush: ThemeBuilder.Gloss((0, "#8FD3FF"), (0.6f, "#C8ECFF"), (1, "#EAF8FF")),
            styleTheme: theme =>
            {
                theme.Cursor = CursorStyle.ModernArrow(Color.White, ThemeBuilder.Hex("#7A5230"));
                const float radius = 16;
                Candy(theme.Button, ThemeBuilder.Gloss((0, "#FFFFFF"), (0.5f, "#FFFDF6"), (1, "#F6E7C8")), brown, ThemeBuilder.Hex("#C9A27A"), ThemeBuilder.Hex("#5A3E2B"), radius);
                theme.ButtonBackgroundBrush = theme.Button.Normal.Background;
                theme.ButtonTextColor = ThemeBuilder.Hex("#5A3E2B");
                theme.ButtonHoverTextColor = ThemeBuilder.Hex("#5A3E2B");
                theme.ButtonPressedTextColor = ThemeBuilder.Hex("#5A3E2B");
                theme.ButtonHoverBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 60));
                theme.ButtonPressedBrush = ThemeBuilder.Solid(new Color(122, 82, 48, 30));

                Candy(theme.PrimaryButton, ThemeBuilder.Gloss((0, "#9BEA6E"), (0.45f, "#6FD444"), (1, "#4FB52E")), ThemeBuilder.Hex("#2E7A18"), ThemeBuilder.Hex("#3A8F22"), Color.White, radius);
                Candy(theme.SecondaryButton, ThemeBuilder.Gloss((0, "#FFE88A"), (0.45f, "#FFD24D"), (1, "#FFB300")), ThemeBuilder.Hex("#A86E00"), ThemeBuilder.Hex("#D48F00"), ThemeBuilder.Hex("#5A3E2B"), radius);
                Candy(theme.DangerButton, ThemeBuilder.Gloss((0, "#FFA8A0"), (0.45f, "#FF7C72"), (1, "#F05045")), ThemeBuilder.Hex("#A83228"), ThemeBuilder.Hex("#C23A30"), Color.White, radius);
                theme.VariantButtonHoverBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 50));
                theme.VariantButtonPressedBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 30));

                var fieldBorder = ThemeBuilder.Solid(ThemeBuilder.Hex("#E0C9A6"));
                foreach (var style in new[] { theme.TextBox, theme.ListBox, theme.ComboBox })
                    ThemeBuilder.Chrome(style, ThemeBuilder.Solid(Color.White), fieldBorder, 2, 12);
                theme.TextBoxBackgroundBrush = ThemeBuilder.Solid(Color.White);
                theme.TextBoxHintTextColor = ThemeBuilder.Hex("#B39878");
                theme.ListBoxBackgroundBrush = ThemeBuilder.Solid(Color.White);
                theme.ListBoxItemBackgroundBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ListBoxSelectedItemBackgroundBrush = ThemeBuilder.Gloss((0, "#6CC0FF"), (1, "#3DA5F4"));
                theme.ComboBoxGlyphColor = brown;

                // Cards: cream, thick brown outline, round corners.
                ThemeBuilder.Chrome(theme.Panel, null, ThemeBuilder.Solid(brown), 3, 22);

                theme.TabHeaderBackgroundBrush = ThemeBuilder.Solid(new Color(255, 246, 224, 200));
                theme.TabSelectedHeaderBackgroundBrush = ThemeBuilder.Gloss((0, "#7CC8FF"), (1, "#3DA5F4"));
                theme.TabHeaderTextColor = ThemeBuilder.Hex("#5A3E2B");
                theme.TabSelectedHeaderTextColor = Color.White;

                theme.ProgressBarBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#F1E3C6"));
                theme.ProgressBarFillBrush = ThemeBuilder.Gloss((0, "#9BEA6E"), (0.5f, "#6FD444"), (1, "#4FB52E"));
                theme.SliderTrackBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#F1E3C6"));
                theme.SliderFillBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#3DA5F4"));
                theme.SliderThumbBrush = ThemeBuilder.Solid(Color.White);
                theme.SliderThumbBorderBrush = ThemeBuilder.Solid(brown);
                theme.SliderThumbSize = 22;
                theme.SliderTrackHeight = 8;
                theme.ScrollBarBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#D9B98F"));
                theme.ScrollBarHoverBrush = ThemeBuilder.Solid(brown);
                theme.ScrollBarGutterBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.CheckBoxBoxBackgroundBrush = ThemeBuilder.Solid(Color.White);
                theme.CheckBoxBoxBorderBrush = ThemeBuilder.Solid(brown);
                theme.CheckBoxCheckMarkBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#4FB52E"));
                theme.ToggleSwitchOffTrackBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#E6D3B0"));
                theme.ToggleSwitchOnTrackBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#5AC83A"));
                theme.ToggleSwitchKnobBrush = ThemeBuilder.Solid(Color.White);
                theme.BadgeBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#FF6B6B"));
                theme.DataGridHeaderBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#FCEBC7"));
                theme.DataGridAlternateRowBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#FFFBF0"));
                theme.ToolTipBackgroundBrush = ThemeBuilder.Solid(cream);
                theme.ToolTipBorderBrush = ThemeBuilder.Solid(brown);
                theme.ToolTipTextColor = ThemeBuilder.Hex("#5A3E2B");
                theme.ProgressIndicatorForeground = ThemeBuilder.Hex("#3DA5F4");
                theme.FocusBorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#3DA5F4"));
                theme.FocusVisualKind = MonoGame.PortableUI.Controls.FocusVisualKind.Thick;
                theme.PanelShadow = new ShadowStyle { Color = new Color(122, 82, 48, 90), Offset = new Vector2(0, 6), Blur = 0 };
            });
    }

    /// <summary>A rounded face with outline, sitting on a solid lip that shrinks when pressed.</summary>
    private static void Candy(ControlStyle style, Brush face, Color outline, Color lip, Color text, float radius)
    {
        ThemeBuilder.Chrome(style, face, ThemeBuilder.Solid(outline), 2, radius);
        style.Normal.TextColor = text;
        style.Normal.Shadows = new[] { new ShadowStyle { Color = lip, Offset = new Vector2(0, 5), Blur = 0 } };
        style.Pressed.Shadows = new[] { new ShadowStyle { Color = lip, Offset = new Vector2(0, 1), Blur = 0 } };
        style.Disabled.Background = ThemeBuilder.Solid(ThemeBuilder.Hex("#EDE3D0"));
        style.Disabled.BorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#CDBFA6"));
        style.Disabled.TextColor = ThemeBuilder.Hex("#A8987E");
        style.Disabled.Shadows = new[] { new ShadowStyle { Color = ThemeBuilder.Hex("#CDBFA6"), Offset = new Vector2(0, 3), Blur = 0 } };
        style.InvalidateResolvedCache();
    }
}
