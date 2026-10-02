using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Common;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Windows Aero / 7: sky-blue acrylic glass with white frames and soft panel shadows.</summary>
public static class AeroTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("aero", "Windows Aero / 7", "selawik", ThemeEra.Glass, ThemeBrightness.Light,
            background: "#DCEFFF", surface: "#B8D6FB", surfaceAlt: "#FFFFFF", text: "#1E395B",
            primary: "#2A7FD4", secondary: "#4FA34F", selection: "#CDE6FC", selectionText: "#1E395B",
            danger: "#C74F35",
            glass: true,
            styleTheme: theme =>
            {
                // XP, Aqua and Aero already had smooth pointers.
                theme.Cursor = CursorStyle.ModernArrow(Color.White, Color.Black);
                // Windows 7: the hard 45 % split gloss, light blue hover/pressed faces, glossy colored
                // call-to-action buttons (close-button red, progress green).
                var face = ThemeBuilder.Gloss((0, "#F2F2F2"), (0.45f, "#EBEBEB"), (0.45f, "#DDDDDD"), (1, "#CFCFCF"));
                var hover = ThemeBuilder.Gloss((0, "#EAF6FD"), (0.45f, "#D9F0FC"), (0.45f, "#BEE6FD"), (1, "#A7D9F5"));
                var pressed = ThemeBuilder.Gloss((0, "#E5F4FC"), (0.3f, "#C4E5F6"), (0.5f, "#98D1EF"), (1, "#68B3DB"));
                ThemeBuilder.Chrome(theme.Button, face, ThemeBuilder.Solid(ThemeBuilder.Hex("#707070")), 1, 3);
                theme.Button.Normal.TextColor = Color.Black;
                theme.Button.Hover.Background = hover;
                theme.Button.Hover.BorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#3C7FB1"));
                theme.Button.Pressed.Background = pressed;
                theme.Button.Pressed.BorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#2C628B"));
                theme.ButtonBackgroundBrush = face;
                theme.ButtonTextColor = Color.Black;
                theme.ButtonHoverTextColor = Color.Black;
                theme.ButtonPressedTextColor = Color.Black;
                theme.ButtonHoverBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.Button.InvalidateResolvedCache();

                Glossy(theme.PrimaryButton, ThemeBuilder.Gloss((0, "#B6DCFA"), (0.45f, "#7DBDF0"), (0.45f, "#3D8FDB"), (1, "#5FB0EE")), "#1F5F9A");
                Glossy(theme.SecondaryButton, ThemeBuilder.Gloss((0, "#C5EBC0"), (0.45f, "#7CCB72"), (0.45f, "#3EA437"), (1, "#5FC456")), "#2A6E25");
                Glossy(theme.DangerButton, ThemeBuilder.Gloss((0, "#F2C2B5"), (0.45f, "#E8A493"), (0.45f, "#C74F35"), (1, "#D2350C")), "#7A2A18");

                // Selection: the pale blue rounded highlight of Explorer lists.
                var highlight = ThemeBuilder.Gloss((0, "#F2F8FF"), (1, "#D0E5FC"));
                theme.ListBoxSelectedItemBackgroundBrush = highlight;
                theme.ListBoxSelectedItemTextColor = ThemeBuilder.Hex("#1E395B");
                theme.TabSelectedHeaderBackgroundBrush = ThemeBuilder.Gloss((0, "#FFFFFF"), (1, "#E3F0FD"));
                theme.TabSelectedHeaderTextColor = Color.Black;
                theme.TabHeaderTextColor = ThemeBuilder.Hex("#1E395B");
                theme.ProgressBarFillBrush = ThemeBuilder.Gloss((0, "#9BEA9B"), (0.45f, "#3CCB3C"), (0.45f, "#06B025"), (1, "#4BD04B"));
                theme.SliderFillBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#3C7FB1"));
                theme.SliderThumbBrush = face;
                theme.SliderThumbBorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#707070"));
                theme.ScrollBarBrush = face;
                theme.ScrollBarHoverBrush = hover;
                theme.ScrollBarPressedBrush = pressed;
                theme.CheckBoxCheckMarkBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#1E395B"));
                theme.CheckBoxBoxBorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#8E8F8F"));
                theme.CheckBoxBoxBorderWidth = 1;
                theme.DataGridHeaderBackgroundBrush = ThemeBuilder.Gloss((0, "#FFFFFF"), (0.45f, "#F4F7FB"), (0.45f, "#E8EEF5"), (1, "#F1F5FA"));
                theme.PanelShadow = new ShadowStyle { Color = new Color(0, 0, 0, 110), Offset = new Vector2(0, 10), Blur = 18 };
            });
    }

    private static void Glossy(ControlStyle style, Brush face, string frame)
    {
        ThemeBuilder.Chrome(style, face, ThemeBuilder.Solid(ThemeBuilder.Hex(frame)), 1, 3);
        style.Normal.TextColor = Color.White;
        style.Disabled.Background = ThemeBuilder.Gloss((0, "#F4F4F4"), (0.45f, "#F4F4F4"), (0.45f, "#EAEAEA"), (1, "#EAEAEA"));
        style.Disabled.BorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#ADB2B5"));
        style.Disabled.TextColor = ThemeBuilder.Hex("#838383");
        style.InvalidateResolvedCache();
    }
}
