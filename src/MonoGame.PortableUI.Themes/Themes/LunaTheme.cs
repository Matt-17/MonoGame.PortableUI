using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>
///     Windows XP Luna ("candy"): beige #ECE9D8 face, glossy white buttons with a dark blue frame and
///     orange hover ring, the saturated blue title-bar gloss for primary actions, the green Start
///     pill for secondary ones, the red close-button gloss for danger, orange-topped tabs and the
///     segmented green progress bar.
/// </summary>
public static class LunaTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("luna", "Windows XP Luna", "selawik", ThemeEra.Desktop, ThemeBrightness.Light,
            background: "#ECE9D8", surface: "#F4F3EE", surfaceAlt: "#FFFFFF", text: "#000000",
            primary: "#0053EE", secondary: "#3C9A3C", selection: "#316AC5", selectionText: "#FFFFFF",
            danger: "#C03B0F",
            styleTheme: theme =>
            {
                var frame = ThemeBuilder.Hex("#003C74");
                var face = ThemeBuilder.Gloss((0, "#FFFFFF"), (0.8f, "#ECEBE6"), (1, "#D6D0C5"));
                var pressed = ThemeBuilder.Gloss((0, "#CDCAC3"), (0.2f, "#E3E2DA"), (1, "#F2F1EA"));
                ThemeBuilder.Chrome(theme.Button, face, ThemeBuilder.Solid(frame), 1, 3);
                theme.Button.Normal.TextColor = Color.Black;
                theme.Button.Hover.BorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#F8B330"));
                theme.Button.Hover.BorderThickness = new Thickness(2);
                theme.Button.Pressed.Background = pressed;
                theme.ButtonBackgroundBrush = face;
                theme.ButtonTextColor = Color.Black;
                theme.ButtonHoverTextColor = Color.Black;
                theme.ButtonPressedTextColor = Color.Black;
                theme.ButtonHoverBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.Button.InvalidateResolvedCache();

                // Call-to-action buttons wear the XP chrome colors: title-bar blue, Start green, close red.
                Candy(theme.PrimaryButton, ThemeBuilder.Gloss((0, "#3D95FF"), (0.1f, "#0058EE"), (0.45f, "#0050EE"), (0.88f, "#0066FF"), (1, "#003DD7")), "#0831D9");
                Candy(theme.SecondaryButton, ThemeBuilder.Gloss((0, "#7CC97C"), (0.12f, "#3C9A3C"), (0.6f, "#2E8B2E"), (1, "#1F6B1F")), "#1E5B1E");
                Candy(theme.DangerButton, ThemeBuilder.Gloss((0, "#F0A98F"), (0.15f, "#E06A45"), (0.6f, "#D24E22"), (1, "#B0350C")), "#7A2508");
                theme.VariantButtonHoverBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 46));
                theme.VariantButtonPressedBrush = ThemeBuilder.Solid(new Color(0, 0, 40, 60));

                var fieldBorder = ThemeBuilder.Solid(ThemeBuilder.Hex("#7F9DB9"));
                foreach (var field in new[] { theme.TextBox, theme.ListBox, theme.ComboBox })
                    ThemeBuilder.Chrome(field, ThemeBuilder.Solid(Color.White), fieldBorder, 1, 0);
                theme.TextBoxBackgroundBrush = ThemeBuilder.Solid(Color.White);
                theme.TextBoxTextColor = Color.Black;
                theme.ListBoxBackgroundBrush = ThemeBuilder.Solid(Color.White);
                theme.ListBoxItemBackgroundBrush = ThemeBuilder.Solid(Color.White);
                theme.ListBoxItemTextColor = Color.Black;
                theme.ListBoxSelectedItemBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#316AC5"));
                theme.ComboBoxGlyphColor = frame;
                theme.ComboBoxDropDownBackgroundBrush = ThemeBuilder.Solid(Color.White);

                // Tabs: white-to-beige faces; the selected one carries XP's orange top line.
                theme.TabHeaderBackgroundBrush = ThemeBuilder.Gloss((0, "#FFFFFF"), (1, "#ECEBE6"));
                theme.TabSelectedHeaderBackgroundBrush = ThemeBuilder.Gloss((0, "#E69A00"), (0.08f, "#FFC83C"), (0.1f, "#FFFFFF"), (1, "#FCFCFE"));
                theme.TabHeaderTextColor = Color.Black;
                theme.TabSelectedHeaderTextColor = Color.Black;

                theme.CheckBoxBoxBackgroundBrush = ThemeBuilder.Gloss((0, "#DCDCD7"), (1, "#FFFFFF"));
                theme.CheckBoxBoxBorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#1C5180"));
                theme.CheckBoxBoxBorderWidth = 1;
                theme.CheckBoxCheckMarkBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#21A121"));
                theme.RadioButtonDotBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#21A121"));

                // Segmented green progress: 8 px blocks with 2 px gaps.
                var green = ThemeBuilder.Hex("#2FD62F");
                var gap = Color.White;
                theme.ProgressBarFillBrush = new PatternBrush(10, 1, new[] { gap, green, green, green, green, green, green, green, green, gap });
                theme.ProgressBarBackgroundBrush = ThemeBuilder.Solid(Color.White);
                theme.SliderFillBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#316AC5"));
                theme.SliderTrackBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#D6D0C5"));
                theme.SliderThumbBrush = face;
                theme.SliderThumbBorderBrush = ThemeBuilder.Solid(frame);

                theme.ScrollBarGutterBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#F4F4F0"));
                theme.ScrollBarBrush = ThemeBuilder.Gloss((0, "#D6E2FD"), (1, "#B5CAF3"));
                theme.ScrollBarHoverBrush = ThemeBuilder.Gloss((0, "#E3ECFE"), (1, "#C5D6F8"));
                theme.DataGridHeaderBackgroundBrush = ThemeBuilder.Gloss((0, "#FFFFFF"), (0.85f, "#EBEADB"), (1, "#D6D2C2"));
                theme.ToolTipBackgroundBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#FFFFE1"));
                theme.ToolTipBorderBrush = ThemeBuilder.Solid(Color.Black);
                theme.ToolTipTextColor = Color.Black;
                theme.ButtonShadow = null;
            });
    }

    private static void Candy(ControlStyle style, Brush face, string frame)
    {
        ThemeBuilder.Chrome(style, face, ThemeBuilder.Solid(ThemeBuilder.Hex(frame)), 1, 3);
        style.Normal.TextColor = Color.White;
        style.Disabled.Background = ThemeBuilder.Solid(ThemeBuilder.Hex("#F4F3EE"));
        style.Disabled.BorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#C9C7BA"));
        style.Disabled.TextColor = ThemeBuilder.Hex("#ACA899");
        style.InvalidateResolvedCache();
    }
}
