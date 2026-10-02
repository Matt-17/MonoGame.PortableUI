using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>
///     Dark fantasy RPG (action-RPG inventory / dungeon menus): soot-dark stone with a vignette,
///     gold-rimmed frames, leather buttons, a gold call-to-action, arcane blue and blood red,
///     parchment-colored text in Cinzel capitals, health-bar red progress.
/// </summary>
public static class DarkFantasyTheme
{
    public static ThemeDefinition Create()
    {
        var gold = ThemeBuilder.Hex("#C9A045");
        var paleGold = ThemeBuilder.Hex("#F5D78E");
        var parchment = ThemeBuilder.Hex("#E8D9B0");
        return ThemeBuilder.Catalog("fantasy", "Dark Fantasy RPG", "cinzel", ThemeEra.Game, ThemeBrightness.Dark,
            background: "#120F0C", surface: "#221B15", surfaceAlt: "#2B231B", text: "#E8D9B0",
            primary: "#C9A045", secondary: "#3E6FA8", selection: "#C9A045", selectionText: "#1A120A",
            danger: "#8B1A1A",
            backgroundBrush: new RadialGradientBrush(ThemeBuilder.Hex("#2E251C"), ThemeBuilder.Hex("#0B0907")),
            // Panels: dark stone inside black / gold / dark-bronze rims.
            surfaceBrush: new FrameBrush(ThemeBuilder.Hex("#211A14"),
                new FrameRing(ThemeBuilder.Hex("#050403"), 1), new FrameRing(gold, 2), new FrameRing(ThemeBuilder.Hex("#5A4318"), 1), new FrameRing(ThemeBuilder.Hex("#171210"), 2)),
            styleTheme: theme =>
            {
                theme.Cursor = CursorStyle.Arrow(ThemeBuilder.Hex("#E0BA5C"), ThemeBuilder.Hex("#1A120A"));
                var leather = ThemeBuilder.Gloss((0, "#4A3A2B"), (0.5f, "#33281E"), (1, "#211A13"));
                var rim = ThemeBuilder.Solid(ThemeBuilder.Hex("#A8843A"));
                ThemeBuilder.Chrome(theme.Button, leather, rim, 2, 3);
                theme.Button.Hover.BorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#FF9A3C"));
                theme.Button.Pressed.Background = ThemeBuilder.Gloss((0, "#1A140F"), (1, "#3A2E22"));
                theme.ButtonBackgroundBrush = leather;
                theme.ButtonTextColor = parchment;
                theme.ButtonHoverTextColor = paleGold;
                theme.ButtonPressedTextColor = paleGold;
                theme.ButtonHoverBrush = ThemeBuilder.Solid(new Color(255, 154, 60, 24));
                theme.ButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.Button.InvalidateResolvedCache();

                Variant(theme.PrimaryButton, ThemeBuilder.Gloss((0, "#FBE7A8"), (0.35f, "#E0BA5C"), (0.7f, "#B88A30"), (1, "#7A5A1E")), "#3A2A10", ThemeBuilder.Hex("#1A120A"));
                Variant(theme.SecondaryButton, ThemeBuilder.Gloss((0, "#6E9BD6"), (0.5f, "#3E6FA8"), (1, "#22416B")), "#C9A045", ThemeBuilder.Hex("#F0F5FF"));
                Variant(theme.DangerButton, ThemeBuilder.Gloss((0, "#C23A3A"), (0.5f, "#8B1A1A"), (1, "#4E0B0B")), "#C9A045", ThemeBuilder.Hex("#F7E3C8"));
                theme.VariantButtonHoverBrush = ThemeBuilder.Solid(new Color(255, 200, 120, 36));
                theme.VariantButtonPressedBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 80));

                var well = ThemeBuilder.Gloss((0, "#0D0A08"), (1, "#1C1611"));
                var wellRim = ThemeBuilder.Solid(ThemeBuilder.Hex("#6B5528"));
                foreach (var style in new[] { theme.TextBox, theme.ListBox, theme.ComboBox })
                    ThemeBuilder.Chrome(style, well, wellRim, 1, 2);
                theme.TextBoxBackgroundBrush = well;
                theme.TextBoxTextColor = parchment;
                theme.TextBoxHintTextColor = ThemeBuilder.Hex("#8A7A5E");
                theme.TextBoxCursorBrush = ThemeBuilder.Solid(paleGold);
                theme.ListBoxBackgroundBrush = well;
                theme.ListBoxItemBackgroundBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ListBoxSelectedItemBackgroundBrush = ThemeBuilder.Gloss((0, "#5A4318"), (1, "#3A2A10"));
                theme.ListBoxSelectedItemTextColor = paleGold;
                theme.ComboBoxDropDownBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#1A1511"));
                theme.ComboBoxGlyphColor = gold;

                theme.TabHeaderBackgroundBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.TabSelectedHeaderBackgroundBrush = ThemeBuilder.Gloss((0, "#FBE7A8"), (0.4f, "#D4AE52"), (1, "#8A6422"));
                theme.TabHeaderTextColor = ThemeBuilder.Hex("#B8A37A");
                theme.TabSelectedHeaderTextColor = ThemeBuilder.Hex("#1A120A");

                // Health-bar red, framed in bronze.
                theme.ProgressBarFillBrush = ThemeBuilder.Gloss((0, "#E25050"), (0.5f, "#B02020"), (1, "#6E0E0E"));
                theme.ProgressBarBackgroundBrush = new FrameBrush(ThemeBuilder.Hex("#120D0A"), new FrameRing(ThemeBuilder.Hex("#6B5528"), 1));
                theme.SliderTrackBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#3A2E22"));
                theme.SliderFillBrush = ThemeBuilder.Solid(gold);
                theme.SliderThumbBrush = ThemeBuilder.Gloss((0, "#FBE7A8"), (1, "#8A6422"));
                theme.SliderThumbBorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#3A2A10"));
                theme.ScrollBarGutterBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#120D0A"));
                theme.ScrollBarBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#8A6A2E"));
                theme.ScrollBarHoverBrush = ThemeBuilder.Solid(gold);
                theme.CheckBoxBoxBackgroundBrush = well;
                theme.CheckBoxBoxBorderBrush = ThemeBuilder.Solid(gold);
                theme.CheckBoxCheckMarkBrush = ThemeBuilder.Solid(paleGold);
                theme.ToggleSwitchOffTrackBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#3A2E22"));
                theme.ToggleSwitchOnTrackBrush = ThemeBuilder.Solid(gold);
                theme.ToggleSwitchKnobBrush = ThemeBuilder.Solid(parchment);
                theme.DataGridHeaderBackgroundBrush = ThemeBuilder.Gloss((0, "#3A2E22"), (1, "#241C15"));
                theme.DataGridHeaderTextColor = gold;
                theme.DataGridAlternateRowBackgroundBrush = ThemeBuilder.Solid(new Color(255, 220, 160, 10));
                theme.DataGridGridLinesBrush = ThemeBuilder.Solid(new Color(201, 160, 69, 40));
                theme.ToolTipBackgroundBrush = new FrameBrush(ThemeBuilder.Hex("#1A1511"), new FrameRing(gold, 1));
                theme.ToolTipBorderBrush = null;
                theme.ToolTipTextColor = parchment;
                theme.BadgeBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#8B1A1A"));
                theme.ProgressIndicatorForeground = gold;
                theme.FocusBorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#FF9A3C"));
                theme.ButtonShadow = new ShadowStyle { Color = new Color(0, 0, 0, 160), Offset = new Vector2(0, 3), Blur = 5 };
                theme.PanelShadow = new ShadowStyle { Color = new Color(0, 0, 0, 200), Offset = new Vector2(0, 8), Blur = 18 };
                theme.PostEffects = new PostEffect[] { new FilmGrainPostEffect { Strength = 0.03f } };
            });
    }

    private static void Variant(ControlStyle style, Brush face, string rim, Color text)
    {
        ThemeBuilder.Chrome(style, face, ThemeBuilder.Solid(ThemeBuilder.Hex(rim)), 2, 3);
        style.Normal.TextColor = text;
        style.Disabled.Background = ThemeBuilder.Gloss((0, "#2E261E"), (1, "#1E1813"));
        style.Disabled.BorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#4A3D28"));
        style.Disabled.TextColor = ThemeBuilder.Hex("#6E6250");
        style.InvalidateResolvedCache();
    }
}
