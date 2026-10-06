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
                // XP, Aqua and Aero already had smooth pointers.
                theme.Cursor = CursorStyle.ModernArrow(Color.White, Color.Black);
                var frame = ThemeBuilder.Hex("#003C74");
                var face = ThemeBuilder.Gloss((0, "#FFFFFF"), (0.8f, "#ECEBE6"), (1, "#D6D0C5"));
                var pressed = ThemeBuilder.Gloss((0, "#CDCAC3"), (0.2f, "#E3E2DA"), (1, "#F2F1EA"));
                ThemeBuilder.Chrome(theme.Button, face, ThemeBuilder.Solid(frame), 1, 3);
                theme.Button.Normal.TextColor = Color.Black;
                theme.Button.Hover.BorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#F8B330"));
                theme.Button.Hover.BorderThickness = new Thickness(2);
                theme.Button.Pressed.Background = pressed;
                theme.Button.Disabled.Background = ThemeBuilder.Solid(ThemeBuilder.Hex("#F4F3EE"));
                theme.Button.Disabled.BorderBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#C9C7BA"));
                theme.Button.Disabled.TextColor = ThemeBuilder.Hex("#ACA899");
                // XP greys controls out with its own colours, not a dark veil over them.
                theme.DisabledOverlayBrush = null;
                theme.ButtonBackgroundBrush = face;
                theme.ButtonTextColor = Color.Black;
                theme.ButtonHoverTextColor = Color.Black;
                theme.ButtonPressedTextColor = Color.Black;
                theme.ButtonHoverBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ButtonPressedBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.Button.InvalidateResolvedCache();

                // Call-to-action buttons wear the XP chrome colors: title-bar blue, Start green, close red.
                // Candy: a light upper half breaking hard into the saturated lower half, as on the
                // XP title bar and Start button.
                Candy(theme.PrimaryButton, ThemeBuilder.Gloss((0, "#7DB6FF"), (0.42f, "#3A86FA"), (0.5f, "#0B5DEC"), (0.9f, "#0A55E4"), (1, "#0841C4")), "#0831D9");
                Candy(theme.SecondaryButton, ThemeBuilder.Gloss((0, "#A4DE96"), (0.42f, "#5DB84F"), (0.5f, "#3A9A2E"), (0.9f, "#348C29"), (1, "#256B1D")), "#1E5B1E");
                Candy(theme.DangerButton, ThemeBuilder.Gloss((0, "#F7BFA8"), (0.42f, "#E8825F"), (0.5f, "#D6542A"), (0.9f, "#CA4A1F"), (1, "#A8360E")), "#7A2508");
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
