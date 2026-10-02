using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>macOS Aqua: blue gel buttons and the signature pinstriped chrome surfaces.</summary>
public static class AquaTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("aqua", "macOS Aqua", "atkinsonhyperlegible", ThemeEra.Desktop, ThemeBrightness.Light,
            background: "#ECECEC", surface: "#FFFFFF", surfaceAlt: "#F4F4F4", text: "#1F1F1F",
            primary: "#3A8EE6", secondary: "#8C99A6", selection: "#3875D7", selectionText: "#FFFFFF",
            danger: "#D63A2F",
            backgroundBrush: PatternBrush.Pinstripes(ThemeBuilder.Hex("#F2F2F2"), ThemeBuilder.Hex("#E4E4E4"), 2),
            styleTheme: theme =>
            {
                // XP, Aqua and Aero already had smooth pointers.
                theme.Cursor = CursorStyle.ModernArrow(Color.White, Color.Black);
                // Aqua gel: a bright upper half with a hard highlight edge, darker core, glowing bottom.
                var white = ThemeBuilder.Gloss((0, "#FFFFFF"), (0.5f, "#F3F3F3"), (0.5f, "#E2E2E2"), (1, "#FDFDFD"));
                var blue = ThemeBuilder.Gloss((0, "#D6EBFF"), (0.12f, "#94CBFF"), (0.5f, "#4A9BF0"), (0.5f, "#2479E0"), (0.85f, "#5BB0FA"), (1, "#A8DBFF"));
                var graphite = ThemeBuilder.Gloss((0, "#F4F6F8"), (0.5f, "#BAC4CE"), (0.5f, "#8C99A6"), (1, "#D3DBE3"));
                var red = ThemeBuilder.Gloss((0, "#FFE0DC"), (0.12f, "#FF9B91"), (0.5f, "#EE5A4F"), (0.5f, "#CC2F24"), (0.85f, "#F06A5E"), (1, "#FFB0A7"));
                const float pill = 19;

                ThemeBuilder.Chrome(theme.Button, white, ThemeBuilder.Solid(ThemeBuilder.Hex("#8E8E8E")), 1, pill);
                theme.Button.Normal.TextColor = Color.Black;
                theme.Button.Pressed.Background = blue;
                theme.Button.Pressed.TextColor = Color.White;
                theme.ButtonBackgroundBrush = white;
                theme.ButtonTextColor = Color.Black;
                theme.ButtonHoverTextColor = Color.Black;
                theme.ButtonPressedTextColor = Color.White;
                theme.ButtonHoverBrush = ThemeBuilder.Solid(new Color(56, 117, 215, 28));
                theme.ButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.Button.InvalidateResolvedCache();

                Gel(theme.PrimaryButton, blue, "#1B57A6", Color.White, pill);
                Gel(theme.SecondaryButton, graphite, "#5E6873", Color.Black, pill);
                Gel(theme.DangerButton, red, "#8C1E17", Color.White, pill);

                var fieldBorder = ThemeBuilder.Solid(ThemeBuilder.Hex("#9A9A9A"));
                foreach (var field in new[] { theme.TextBox, theme.ListBox })
                    ThemeBuilder.Chrome(field, ThemeBuilder.Solid(Color.White), fieldBorder, 1, 0);
                ThemeBuilder.Chrome(theme.ComboBox, white, fieldBorder, 1, 6);
                theme.TextBoxBackgroundBrush = ThemeBuilder.Solid(Color.White);
                theme.ListBoxBackgroundBrush = ThemeBuilder.Solid(Color.White);
                theme.ListBoxItemBackgroundBrush = ThemeBuilder.Solid(Color.White);
                theme.ListBoxSelectedItemBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#3875D7"));

                // Segmented tab control: white gel, the selected segment blue gel.
                theme.TabHeaderBackgroundBrush = white;
                theme.TabSelectedHeaderBackgroundBrush = blue;
                theme.TabHeaderTextColor = Color.Black;
                theme.TabSelectedHeaderTextColor = Color.White;

                theme.ScrollBarThickness = 11;
                theme.ScrollBarGutterBrush = ThemeBuilder.Gloss((0, "#DADADA"), (0.5f, "#F4F4F4"), (1, "#FFFFFF"));
                theme.ScrollBarBrush = blue;
                theme.ScrollBarHoverBrush = blue;
                theme.ScrollBarPressedBrush = graphite;
                theme.ProgressBarFillBrush = blue;
                theme.ProgressBarBackgroundBrush = ThemeBuilder.Gloss((0, "#D4D4D4"), (0.5f, "#EFEFEF"), (1, "#FFFFFF"));
                theme.SliderFillBrush = blue;
                theme.SliderTrackBrush = ThemeBuilder.Gloss((0, "#BDBDBD"), (1, "#EDEDED"));
                theme.SliderThumbBrush = white;
                theme.SliderThumbBorderBrush = fieldBorder;
                theme.CheckBoxBoxBackgroundBrush = white;
                theme.CheckBoxBoxBorderBrush = fieldBorder;
                theme.CheckBoxBoxBorderWidth = 1;
                theme.CheckBoxCheckMarkBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#1B57A6"));
                theme.ToggleSwitchOnTrackBrush = blue;
                theme.DataGridHeaderBackgroundBrush = white;
                theme.ButtonShadow = new ShadowStyle { Color = new Color(0, 0, 0, 60), Offset = new Vector2(0, 1), Blur = 2 };
                theme.PanelShadow = new ShadowStyle { Color = new Color(0, 0, 0, 70), Offset = new Vector2(0, 8), Blur = 18 };
            });
    }

    private static void Gel(ControlStyle style, Brush face, string frame, Color text, float radius)
    {
        ThemeBuilder.Chrome(style, face, ThemeBuilder.Solid(ThemeBuilder.Hex(frame)), 1, radius);
        style.Normal.TextColor = text;
        style.Disabled.Background = ThemeBuilder.Gloss((0, "#FFFFFF"), (0.5f, "#F6F6F6"), (0.5f, "#ECECEC"), (1, "#FAFAFA"));
        style.Disabled.BorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#C4C4C4"));
        style.Disabled.TextColor = ThemeBuilder.Hex("#A0A0A0");
        style.InvalidateResolvedCache();
    }
}
